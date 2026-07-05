using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// LED-lit crop production (shielded/buried growth modules — light is all electric,
    /// which is exactly what makes food-vs-power one of the sim's core trades).
    ///
    /// L1: staggered crop batches with growth cycles — plantings mature after cycle sols,
    ///     harvests are discrete events (food + inedible biomass), lights follow photoperiod.
    /// L0: continuous distilled production — kcal/m²/sol and kWh/m²/sol constants.
    /// Photosynthesis gas exchange runs against the cabin: CO2 in, O2 out — a real coupling
    /// with ECLSS sizing. Transpired water returns to the potable loop via condensate.
    /// </summary>
    public sealed class Greenhouse : SimModule
    {
        public override string DisplayName => "Greenhouse";
        public override FidelityLevel MaxFidelity => FidelityLevel.L2_Physics; // L2 == L1 batch model with per-step light control

        public double GrowingAreaM2 { get; set; }

        private sealed class Batch
        {
            public double AreaM2;
            public double PlantedSol;
            public double CycleSols;
            public double Progress; // 0..1
        }

        private readonly List<Batch> _batches = new();
        private double _lastPlantSol = -999;

        private Param _kcalPerM2Sol, _kwhPerM2Sol, _photoperiodH, _cycleSols, _edibleFraction,
                      _waterPerM2Sol, _transpirationRecovery, _laborMinPerM2Sol, _kcalPerKgFood,
                      _co2PerKgBiomass, _o2PerKgBiomass, _systemMassPerM2, _failureRework;

        private Store _food, _water, _biomass, _o2Reserve;
        private Habitat _hab;
        private double _lightsKw;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _kcalPerM2Sol = p.GetOrRegister("food.kcal_per_m2_sol", "Edible output per growing area", 80, "kcal/m2/sol",
                "BVAD crop tables: potato/wheat class at high PPF");
            _kwhPerM2Sol = p.GetOrRegister("food.led_kwh_per_m2_sol", "LED electrical energy per growing area", 18, "kWh/m2/sol",
                "PPF ~800 umol/m2/s, 20 h photoperiod, LED efficacy ~2.8 umol/J");
            _photoperiodH = p.GetOrRegister("food.photoperiod_hours", "Photoperiod", 20, "h/sol",
                "BVAD crop tables (species-averaged)");
            _cycleSols = p.GetOrRegister("food.crop_cycle_sols", "Mean crop cycle", 85, "sols",
                "BVAD: potato ~132 d, wheat ~62 d, lettuce ~28 d — area-weighted mix");
            _edibleFraction = p.GetOrRegister("food.harvest_index", "Edible fraction of biomass (harvest index)", 0.45, "",
                "BVAD crop tables");
            _waterPerM2Sol = p.GetOrRegister("food.water_per_m2_sol", "Crop water throughput", 2.5, "kg/m2/sol",
                "Transpiration-dominated (BVAD: 1.5-4 L/m2-day)");
            _transpirationRecovery = p.GetOrRegister("food.transpiration_recovery", "Transpired water recovered as condensate", 0.95, "",
                "Closed-loop condensing HX assumption");
            _laborMinPerM2Sol = p.GetOrRegister("food.labor_min_per_m2_sol", "Tending labor", 0.015, "crew-eq h/m2/sol",
                "CELSS studies: ~1.5 h/day per 100 m2");
            // Grown food is banked at the pantry's shared energy density so the food store
            // has ONE kcal/kg basis for both packaged and grown mass.
            _kcalPerKgFood = p.GetOrRegister("food.kcal_per_kg_packaged", "Packaged food energy density (as-shipped incl. packaging)", 1650, "kcal/kg",
                "BVAD: ~1.83 kg/CM-day for 3040 kcal incl. packaging");
            p.GetOrRegister("food.l0_utilization", "Distilled batch-model utilization (L0)", 0.9, "",
                "Derived: staggering gaps + batch losses measured by the greenhouse distillation");
            _co2PerKgBiomass = p.GetOrRegister("food.co2_per_kg_biomass", "CO2 fixed per kg dry biomass", 1.6, "kg/kg",
                "Photosynthesis stoichiometry (CH2O basis)");
            _o2PerKgBiomass = p.GetOrRegister("food.o2_per_kg_biomass", "O2 released per kg dry biomass", 1.2, "kg/kg",
                "Photosynthesis stoichiometry");
            _systemMassPerM2 = p.GetOrRegister("food.system_mass_kg_m2", "Growth system mass per area", 90, "kg/m2",
                "BVAD biomass production chamber estimates");
            _failureRework = p.GetOrRegister("food.crop_loss_probability", "Probability a batch is lost (disease/failure)", 0.05, "probability",
                "CELSS risk studies (tunable)");

            _food = ctx.Stores.GetOrCreate("food", Resource.Food, 0);
            _water = ctx.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            _biomass = ctx.Stores.GetOrCreate("biomass", Resource.Biomass, 100000);
            _o2Reserve = ctx.Stores.GetOrCreate("o2_reserve", Resource.O2, 0);
            _hab = Engine.Find<Habitat>();
        }

        public double SystemMassKg => GrowingAreaM2 * _systemMassPerM2.Value;

        public override void PreTick(SimContext ctx)
        {
            if (GrowingAreaM2 <= 0) return;

            bool lightsOn = EffectiveFidelity == FidelityLevel.L0_Distilled
                || ctx.Clock.LocalSolarHours < _photoperiodH.Value; // photoperiod window each sol

            double avgKw = GrowingAreaM2 * _kwhPerM2Sol.Value / Units.SolHours;
            _lightsKw = EffectiveFidelity == FidelityLevel.L0_Distilled
                ? avgKw
                : (lightsOn ? avgKw * (Units.SolHours / _photoperiodH.Value) : 0);

            ctx.Power.Request(this, _lightsKw, LoadPriority.Low);
            ctx.Labor.Request(this, TaskType.Agriculture,
                GrowingAreaM2 * _laborMinPerM2Sol.Value * ctx.DtSols, LaborPriority.Normal);
        }

        public override void Tick(SimContext ctx)
        {
            if (GrowingAreaM2 <= 0) return;
            double powerGrant = ctx.Power.GrantedFraction(this);
            double laborGrant = ctx.Labor.GrantedFraction(this, TaskType.Agriculture);
            double effectiveness = Math.Min(powerGrant, 0.5 + 0.5 * laborGrant) * CapacityFactor;

            // kcalGrowth drives gas exchange continuously (plants photosynthesize while
            // growing); food only lands in the pantry at harvest in the batch model.
            double kcalGrowth;
            if (EffectiveFidelity == FidelityLevel.L0_Distilled)
            {
                double util = ctx.Params.V("food.l0_utilization");
                kcalGrowth = GrowingAreaM2 * _kcalPerM2Sol.Value * util * ctx.DtSols * effectiveness;
                _food.Deposit(kcalGrowth / _kcalPerKgFood.Value);
            }
            else
            {
                kcalGrowth = TickBatches(ctx, effectiveness);
            }
            double kcalProduced = kcalGrowth;

            // Gas exchange: crops preferentially scrub cabin CO2 (helping ECLSS); the rest is
            // enriched from the 95%-CO2 Mars atmosphere (cheap compressor, folded into lights
            // power). Photosynthetic O2 tops up the cabin only to setpoint; surplus goes to
            // the O2 reserve — greenhouses double as oxygen plants.
            double dryBiomass = kcalProduced / 4000.0 / Math.Max(0.05, _edibleFraction.Value); // kcal->dry kg via ~4 kcal/g
            if (_hab != null && dryBiomass > 0)
            {
                double co2Wanted = dryBiomass * _co2PerKgBiomass.Value;
                double fromCabin = _hab.DrawCO2(Math.Min(co2Wanted, _hab.CabinCO2Kg * 0.5));
                // Shortfall drawn from the Mars atmosphere (unlimited CO2 source).
                double o2Made = dryBiomass * _o2PerKgBiomass.Value;
                double deficitKg = _hab.KgToRaisePpO2(ctx.Params.V("eclss.ppo2_setpoint") - _hab.PpO2Kpa);
                double toCabin = Math.Min(o2Made, deficitKg);
                _hab.InjectO2(toCabin);
                _o2Reserve.Deposit(o2Made - toCabin);
            }

            // Water loop: transpiration mostly recovered; the rest is embodied/lost.
            double waterUsed = GrowingAreaM2 * _waterPerM2Sol.Value * ctx.DtSols * effectiveness;
            double got = _water.Withdraw(waterUsed);
            _water.Deposit(got * _transpirationRecovery.Value);

            Record(ctx, "greenhouse.kcal_per_sol", "Food production", "kcal/sol", kcalProduced / ctx.DtSols);
            Record(ctx, "greenhouse.power", "Greenhouse lighting", "kW", _lightsKw * powerGrant);
            Record(ctx, "greenhouse.area", "Growing area", "m²", GrowingAreaM2);
        }

        /// <summary>Returns kcal of biomass GROWN this step (photosynthesis basis); harvests deposit food.</summary>
        private double TickBatches(SimContext ctx, double effectiveness)
        {
            // Keep the whole area planted in staggered batches (1/8 of area per planting).
            double planted = 0;
            foreach (var b in _batches) planted += b.AreaM2;
            if (planted < GrowingAreaM2 - 1 && ctx.Clock.Sol - _lastPlantSol > _cycleSols.Value / 8.0)
            {
                _batches.Add(new Batch
                {
                    AreaM2 = Math.Min(GrowingAreaM2 / 8.0, GrowingAreaM2 - planted),
                    PlantedSol = ctx.Clock.Sol,
                    CycleSols = _cycleSols.Value * Rng.Range(0.9, 1.1),
                });
                _lastPlantSol = ctx.Clock.Sol;
            }

            double kcalGrown = 0;
            for (int i = _batches.Count - 1; i >= 0; i--)
            {
                var b = _batches[i];
                double progressStep = ctx.DtSols / b.CycleSols * effectiveness;
                b.Progress += progressStep;
                kcalGrown += b.AreaM2 * _kcalPerM2Sol.Value * b.CycleSols * progressStep;
                if (b.Progress >= 1)
                {
                    _batches.RemoveAt(i);
                    if (Rng.Chance(_failureRework.Value))
                    {
                        Log(ctx, EventSeverity.Warning, $"Crop batch lost ({b.AreaM2:F0} m²) — disease/system failure");
                        continue;
                    }
                    // Full-cycle yield delivered at harvest. Food is banked at the shared
                    // pantry energy density so eating and growing use one kcal/kg basis.
                    double batchKcal = b.AreaM2 * _kcalPerM2Sol.Value * b.CycleSols;
                    double foodKg = batchKcal / _kcalPerKgFood.Value;
                    _food.Deposit(foodKg);
                    _biomass.Deposit(foodKg * (1 - _edibleFraction.Value) / Math.Max(0.05, _edibleFraction.Value));
                    Log(ctx, EventSeverity.Info, $"Harvest: {foodKg:F0} kg food ({batchKcal / 1000.0:F0} Mcal) from {b.AreaM2:F0} m²");
                }
            }
            return kcalGrown;
        }

        public override string StatusLine => GrowingAreaM2 <= 0 ? "Not installed"
            : $"{GrowingAreaM2:F0} m² growing, {_batches.Count} batches";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Growing area", GrowingAreaM2, "m²");
                yield return ("Lighting draw", _lightsKw, "kW");
                yield return ("System mass", SystemMassKg / 1000.0, "t");
            }
        }
    }
}
