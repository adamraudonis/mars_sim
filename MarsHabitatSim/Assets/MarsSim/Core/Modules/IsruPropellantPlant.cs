using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Methalox propellant production. Two classic architectures, switchable live:
    ///
    /// A) WATER-BASED (architecture=0, needs ice mining):
    ///    Electrolyze water for H2; Sabatier CO2+4H2 -> CH4+2H2O (water recycled);
    ///    the same electrolysis stream yields all O2.
    ///    Per kg CH4: fresh water 2.25 kg, O2 co-produced 4.0 kg (O:F 3.6 leaves margin).
    ///
    /// B) H2-IMPORT + SOXE (architecture=1, the Mars-Direct/MOXIE path, zero water mining):
    ///    Earth H2 feeds Sabatier; product water is electrolyzed (recycling half the H2,
    ///    producing 2.0 kg O2/kg CH4); the remaining O2 comes from solid-oxide CO2
    ///    electrolysis (2CO2 -> 2CO + O2, MOXIE-style) at a higher specific energy.
    ///    Per kg CH4: net H2 import 0.25 kg, zero net water, more kWh.
    ///
    /// The trade between them (water mining mass+labor vs H2 import mass+boiloff vs power)
    /// is one of the sim's headline studies.
    /// </summary>
    public sealed class IsruPropellantPlant : SimModule
    {
        public override string DisplayName => "ISRU propellant plant";

        /// <summary>Installed production capacity, kg propellant (CH4+O2 mix) per sol.</summary>
        public double CapacityKgPerSol { get; set; }

        /// <summary>Commissioning progress [0..1] — construction labor drives this.</summary>
        public double Commissioned { get; private set; }

        public double ProductionKgPerSol { get; private set; }

        private Param _ofRatio, _co2KwhPerKg, _elecKwhPerKgH2O, _sabKwhPerKgCh4, _liqKwhPerKgCh4,
                      _liqKwhPerKgO2, _architecture, _soxeKwhPerKgO2, _plantMassPerKgSol,
                      _opsHoursPerTonne, _commissionHours, _commissionSols, _l0KwhPerKg;

        private Store _water, _h2Import, _ch4, _lox;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _ofRatio = p.GetOrRegister("starship.of_ratio", "Raptor O:F mixture ratio", 3.6, "kg O2/kg CH4",
                "Raptor methalox, SpaceX statements");
            _co2KwhPerKg = p.GetOrRegister("isru_atmosphere.co2_acquisition_kwh_per_kg", "CO2 acquisition energy", 0.9, "kWh/kg CO2",
                "Cryogenic CO2 freezer studies (Muscatello et al.)");
            _elecKwhPerKgH2O = p.GetOrRegister("isru_atmosphere.electrolysis_kwh_per_kg_h2o", "Water electrolysis energy", 6.0, "kWh/kg H2O",
                "PEM ~50-55 kWh/kg H2 => ~6 kWh/kg H2O incl. BoP");
            _sabKwhPerKgCh4 = p.GetOrRegister("isru_atmosphere.sabatier_kwh_per_kg_ch4", "Sabatier system energy", 0.8, "kWh/kg CH4",
                "Exothermic reaction; energy is compressors/thermal management");
            _liqKwhPerKgCh4 = p.GetOrRegister("isru_atmosphere.liquefaction_kwh_per_kg_ch4", "CH4 liquefaction energy", 0.9, "kWh/kg",
                "Mars-surface cryocooler studies (~0.5-1.2 kWh/kg)");
            _liqKwhPerKgO2 = p.GetOrRegister("isru_atmosphere.liquefaction_kwh_per_kg_o2", "O2 liquefaction energy", 0.5, "kWh/kg",
                "Mars-surface cryocooler studies");
            _architecture = p.GetOrRegister("isru_atmosphere.architecture", "Plant architecture (0=water-based, 1=H2-import+SOXE)", 0, "mode",
                "Trade switch: ice mining vs Earth H2 + CO2-only O2");
            _soxeKwhPerKgO2 = p.GetOrRegister("isru_atmosphere.soxe_kwh_per_kg_o2", "SOXE O2 specific energy (full scale)", 11.0, "kWh/kg O2",
                "MOXIE flight ~37 kWh/kg at 6-10 g/hr; full-scale projections ~10-12 (Hecht et al.)");
            _plantMassPerKgSol = p.GetOrRegister("isru_atmosphere.plant_mass_kg_per_kg_sol", "Plant specific mass", 8, "kg per (kg/sol)",
                "Scaled from NASA ISRU system studies (Kleinhenz & Paz)");
            _opsHoursPerTonne = p.GetOrRegister("isru_atmosphere.ops_hours_per_tonne", "Operations labor", 1.5, "crew-eq h/t",
                "Estimate: monitoring + maintenance rounds");
            _commissionHours = p.GetOrRegister("isru_atmosphere.commission_hours_per_kg_sol", "Commissioning labor per capacity", 0.5, "crew-eq h per kg/sol",
                "Estimate: deployment, hookup, checkout");
            _commissionSols = p.GetOrRegister("isru_atmosphere.commission_target_sols", "Commissioning campaign length target", 50, "sols",
                "Ops assumption");
            _l0KwhPerKg = p.GetOrRegister("isru_atmosphere.l0_kwh_per_kg_propellant", "Distilled plant energy (L0)", 7.5, "kWh/kg propellant",
                "Kleinhenz & Paz-class full-chain estimates (7-10 kWh/kg)");

            _water = ctx.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            _h2Import = ctx.Stores.GetOrCreate("h2_import", Resource.H2, 0);
            _ch4 = ctx.Stores.GetOrCreate("depot_ch4", Resource.CH4, 0);
            _lox = ctx.Stores.GetOrCreate("depot_lox", Resource.LOX, 0);
        }

        public double PlantMassKg => CapacityKgPerSol * _plantMassPerKgSol.Value;

        /// <summary>Wear tracks actual throughput (a power-starved plant isn't grinding its compressors).</summary>
        public override double FailureDutyCycle => CapacityKgPerSol <= 0 ? 0
            : Commissioned < 1 ? 0.05
            : Math.Clamp(ProductionKgPerSol / Math.Max(1, CapacityKgPerSol), 0.05, 1.0);

        private bool UseH2Import => _architecture.Value > 0.5;

        /// <summary>
        /// Per-kg-propellant-mix chain coefficients for the current architecture.
        /// All mass flows per kg of (CH4+O2) mix at the current O:F.
        /// </summary>
        public (double kwhPerKg, double waterPerKg, double h2PerKg) ChainCoefficients(bool h2Import)
        {
            double of = _ofRatio.Value;
            double mCh4 = 1.0 / (1.0 + of);   // kg CH4 per kg mix
            double mO2 = of / (1.0 + of);     // kg O2 per kg mix

            double sabatierCo2 = 2.75 * mCh4;            // CO2 into Sabatier
            double sabatierWaterOut = 2.25 * mCh4;       // water product

            if (!h2Import)
            {
                // Electrolysis supplies all H2 (0.5 kg/kg CH4 -> 4.5 kg H2O/kg CH4) and with it
                // co-produces 4.0 kg O2/kg CH4 >= the 3.6 needed. Sabatier water is recycled.
                double waterElectrolyzed = 4.5 * mCh4;
                double freshWater = waterElectrolyzed - sabatierWaterOut;   // 2.25 * mCh4
                double kwh = sabatierCo2 * _co2KwhPerKg.Value
                             + mCh4 * _sabKwhPerKgCh4.Value
                             + waterElectrolyzed * _elecKwhPerKgH2O.Value
                             + mCh4 * _liqKwhPerKgCh4.Value + mO2 * _liqKwhPerKgO2.Value;
                return (kwh, freshWater, 0);
            }
            else
            {
                // Sabatier product water is electrolyzed: recycles 0.25 kg H2/kg CH4 (halving
                // the import) and yields 2.0 kg O2/kg CH4; SOXE covers the rest of the O2.
                double waterElectrolyzed = sabatierWaterOut;                 // 2.25 * mCh4
                double o2FromWater = waterElectrolyzed * 8.0 / 9.0;          // 2.0 * mCh4
                double h2Recycled = waterElectrolyzed / 9.0;                 // 0.25 * mCh4
                double h2Imported = 0.5 * mCh4 - h2Recycled;                 // 0.25 * mCh4
                double o2FromSoxe = Math.Max(0, mO2 - o2FromWater);
                double soxeCo2 = o2FromSoxe * 2.75;                          // 2CO2 -> 2CO + O2

                double kwh = (sabatierCo2 + soxeCo2) * _co2KwhPerKg.Value
                             + mCh4 * _sabKwhPerKgCh4.Value
                             + waterElectrolyzed * _elecKwhPerKgH2O.Value
                             + o2FromSoxe * _soxeKwhPerKgO2.Value
                             + mCh4 * _liqKwhPerKgCh4.Value + mO2 * _liqKwhPerKgO2.Value;
                return (kwh, 0, h2Imported);
            }
        }

        public override void PreTick(SimContext ctx)
        {
            if (CapacityKgPerSol <= 0) return;

            if (Commissioned < 1)
            {
                double totalHours = CapacityKgPerSol * _commissionHours.Value;
                ctx.Labor.Request(this, TaskType.Construction,
                    totalHours / Math.Max(1, _commissionSols.Value) * ctx.DtSols, LaborPriority.Normal);
                return;
            }

            var (kwhPerKg, _, _) = EffectiveFidelity == FidelityLevel.L0_Distilled
                ? (_l0KwhPerKg.Value, 0, 0)
                : ChainCoefficients(UseH2Import);
            ctx.Power.Request(this, CapacityKgPerSol * kwhPerKg / Units.SolHours, LoadPriority.Normal);
            ctx.Labor.Request(this, TaskType.IsruOps,
                CapacityKgPerSol / 1000.0 * _opsHoursPerTonne.Value * ctx.DtSols, LaborPriority.Normal);
        }

        public override void Tick(SimContext ctx)
        {
            if (CapacityKgPerSol <= 0) { ProductionKgPerSol = 0; return; }

            if (Commissioned < 1)
            {
                double grantC = ctx.Labor.GrantedFraction(this, TaskType.Construction);
                Commissioned = Math.Min(1, Commissioned + grantC * ctx.DtSols / Math.Max(1, _commissionSols.Value));
                if (Commissioned >= 1)
                    Log(ctx, EventSeverity.Milestone, $"Propellant plant commissioned ({CapacityKgPerSol / 1000.0:F1} t/sol capacity)");
                ProductionKgPerSol = 0;
                return;
            }

            bool h2Mode = UseH2Import;
            var (kwhPerKg, waterPerKg, h2PerKg) = ChainCoefficients(h2Mode);
            if (EffectiveFidelity == FidelityLevel.L0_Distilled) kwhPerKg = _l0KwhPerKg.Value;

            double powerGrant = ctx.Power.GrantedFraction(this);
            double laborGrant = ctx.Labor.GrantedFraction(this, TaskType.IsruOps);
            double throughput = CapacityKgPerSol * ctx.DtSols * powerGrant
                                * Math.Min(1.0, 0.7 + 0.3 * laborGrant) * CapacityFactor;

            // Feedstock limits.
            double maxByWater = waterPerKg > 1e-9 ? _water.AmountKg / waterPerKg : double.MaxValue;
            double maxByH2 = h2PerKg > 1e-9 ? _h2Import.AmountKg / h2PerKg : double.MaxValue;
            double made = Math.Min(throughput, Math.Min(maxByWater, maxByH2));

            if (made > 1e-9)
            {
                double of = _ofRatio.Value;
                if (waterPerKg > 0) _water.Withdraw(made * waterPerKg);
                if (h2PerKg > 0) _h2Import.Withdraw(made * h2PerKg);
                _ch4.Deposit(made / (1 + of));
                _lox.Deposit(made * of / (1 + of));
            }
            ProductionKgPerSol = made / ctx.DtSols;

            Record(ctx, "isru.production", "Propellant production", "kg/sol", ProductionKgPerSol);
            Record(ctx, "isru.kwh_per_kg", "Plant specific energy", "kWh/kg", kwhPerKg);
            Record(ctx, "isru.water_demand", "Plant water demand", "kg/sol", ProductionKgPerSol * waterPerKg);
            Record(ctx, "isru.h2_demand", "Plant H2 import demand", "kg/sol", ProductionKgPerSol * h2PerKg);
        }

        public override string StatusLine => CapacityKgPerSol <= 0 ? "Not installed"
            : Commissioned < 1 ? $"Commissioning {Commissioned:P0}"
            : $"{ProductionKgPerSol / 1000.0:F2} t/sol of {CapacityKgPerSol / 1000.0:F2} t/sol ({(UseH2Import ? "H2-import+SOXE" : "water-based")})";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Capacity", CapacityKgPerSol, "kg/sol");
                yield return ("Production", ProductionKgPerSol, "kg/sol");
                yield return ("Plant mass", PlantMassKg / 1000.0, "t");
                yield return ("CH₄ in depot", (Engine.Stores.Get("depot_ch4")?.AmountKg ?? 0) / 1000.0, "t");
                yield return ("LOX in depot", (Engine.Stores.Get("depot_lox")?.AmountKg ?? 0) / 1000.0, "t");
            }
        }
    }
}
