using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Photovoltaic farm.
    /// L2: per-step irradiance geometry x cell efficiency x temperature derate x dust factor,
    ///     with continuous dust deposition (faster during storms) and cleaning (labor or
    ///     electrostatic) — the "expensive" model.
    /// L1: same but with dust held at its long-run equilibrium (no cleaning dynamics).
    /// L0: distilled scalar — installed kW x (kWh/sol per kW) spread uniformly over the sol.
    ///     Use the Distiller to fit the L0 coefficient from an L2 run.
    /// </summary>
    public sealed class SolarFarm : SimModule
    {
        public override string DisplayName => "Solar farm";
        public override FidelityLevel MaxFidelity => FidelityLevel.L2_Physics;

        /// <summary>Deployed array area, m² (grows as cargo ships land).</summary>
        public double ArrayAreaM2 { get; set; }

        /// <summary>Dust obscuration fraction [0..1): 0 = clean.</summary>
        public double DustFraction { get; private set; }

        public double OutputKw { get; private set; }

        private Param _cellEff, _tempCoeff, _tRef, _dustRatePerSol, _dustStormRatePerSol,
                      _cleaningThreshold, _cleaningHoursPer100m2, _electrostaticCleaning,
                      _specificMassKgM2, _l0KwhPerSolPerKw, _degradationPerYear;

        private double _ageYears;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _cellEff = p.GetOrRegister("power_solar.cell_efficiency", "PV cell efficiency (BOL)", 0.30, "",
                "Flight multi-junction (XTJ/IMM) class");
            _tempCoeff = p.GetOrRegister("power_solar.temp_coeff", "Efficiency temperature coefficient", -0.0025, "1/degC",
                "Triple-junction typical -0.2..-0.3 %/degC");
            _tRef = p.GetOrRegister("power_solar.t_ref", "Cell reference temperature", 25, "degC",
                "Standard rating condition");
            _dustRatePerSol = p.GetOrRegister("power_solar.dust_rate_per_sol", "Dust obscuration accumulation", 0.0025, "fraction/sol",
                "MER flight data ~0.28%/sol early mission (Kinch et al.)");
            _dustStormRatePerSol = p.GetOrRegister("power_solar.dust_storm_rate_per_sol", "Dust accumulation during storms", 0.01, "fraction/sol",
                "InSight 2018 storm experience");
            _cleaningThreshold = p.GetOrRegister("power_solar.cleaning_threshold", "Dust fraction triggering cleaning", 0.15, "",
                "Ops policy (tunable)");
            _cleaningHoursPer100m2 = p.GetOrRegister("power_solar.cleaning_hours_per_100m2", "Cleaning labor per 100 m²", 1.0, "crew-eq h",
                "Estimate: brushing/wiping rate analog");
            _electrostaticCleaning = p.GetOrRegister("power_solar.electrostatic_cleaning", "Electrostatic dust removal installed (0/1)", 0, "bool",
                "EDS technology (Calle et al., KSC) — optional upgrade");
            _specificMassKgM2 = p.GetOrRegister("power_solar.specific_mass_kg_m2", "Array system specific mass", 2.5, "kg/m2",
                "NASA Mars surface array studies (ROSA-derived, incl. structure)");
            _l0KwhPerSolPerKw = p.GetOrRegister("power_solar.l0_kwh_per_sol_per_kw", "Distilled yield per installed kW (L0)", 4.5, "kWh/sol/kW",
                "Derived: distilled from L2 run at 40N (rating basis 500 W/m2 clear noon); cf. 7.4 equivalent peak hours at equator (research)");
            _degradationPerYear = p.GetOrRegister("power_solar.degradation_per_earth_year", "Cell degradation", 0.01, "fraction/year",
                "GaAs on-orbit experience ~0.5-1.5%/yr");
        }

        /// <summary>Installed kW rating at reference conditions (Mars noon, clear, 25°C).</summary>
        public double InstalledKwRating
        {
            get
            {
                const double refGhi = 500.0; // W/m², approx clear-sky noon GHI at mid-latitude
                return ArrayAreaM2 * _cellEff.Value * refGhi / 1000.0;
            }
        }

        public double ArrayMassKg => ArrayAreaM2 * _specificMassKgM2.Value;

        public override void PreTick(SimContext ctx)
        {
            OutputKw = ComputeOutputKw(ctx);
            ctx.Power.Offer(OutputKw);

            // Cleaning labor must be requested here (before LaborPool.Resolve) so the grant
            // read in Tick reflects a real allocation, not the never-requested default.
            _cleaningHoursWanted = 0;
            if (EffectiveFidelity == FidelityLevel.L2_Physics && ArrayAreaM2 > 0
                && DustFraction > _cleaningThreshold.Value && _electrostaticCleaning.Value < 0.5)
            {
                _cleaningHoursWanted = (ArrayAreaM2 / 100.0) * _cleaningHoursPer100m2.Value * 0.2 * ctx.DtSols;
                // Badly dusted arrays threaten the whole power supply — cleaning outranks
                // ordinary construction/ops until obscuration is back under control.
                var prio = DustFraction > 2 * _cleaningThreshold.Value ? LaborPriority.High : LaborPriority.Normal;
                ctx.Labor.Request(this, TaskType.Logistics, _cleaningHoursWanted, prio);
            }
        }

        private double _cleaningHoursWanted;

        private double ComputeOutputKw(SimContext ctx)
        {
            if (ArrayAreaM2 <= 0) return 0;
            double derate = (1 - DustFraction) * CapacityFactor * Math.Pow(1 - _degradationPerYear.Value, _ageYears);

            if (EffectiveFidelity == FidelityLevel.L0_Distilled)
            {
                // Uniform-power abstraction: sol-averaged energy spread across all steps.
                return InstalledKwRating * _l0KwhPerSolPerKw.Value / Units.SolHours * derate;
            }

            double ghi = ctx.Env.GlobalHorizontalWm2;
            if (ghi <= 0) return 0;
            double cellTemp = ctx.Env.AirTemperatureC + ghi * 0.06; // ~+30C at 500 W/m2 (MER/Pathfinder flat-plate experience)
            double eff = _cellEff.Value * (1 + _tempCoeff.Value * (cellTemp - _tRef.Value));
            return ArrayAreaM2 * eff * ghi / 1000.0 * derate;
        }

        public override void Tick(SimContext ctx)
        {
            _ageYears += ctx.DtSeconds / (365.25 * 86400.0);

            if (EffectiveFidelity == FidelityLevel.L2_Physics)
            {
                double rate = ctx.Env.GlobalDustStorm || ctx.Env.OpticalDepthTau > 2.0
                    ? _dustStormRatePerSol.Value : _dustRatePerSol.Value;
                DustFraction = Math.Min(0.95, DustFraction + rate * ctx.DtSols);

                if (DustFraction > _cleaningThreshold.Value && ArrayAreaM2 > 0)
                {
                    if (_electrostaticCleaning.Value > 0.5)
                    {
                        DustFraction *= Math.Pow(0.5, ctx.DtSols); // EDS clears in ~1 sol
                    }
                    else if (_cleaningHoursWanted > 0)
                    {
                        // Cleaning labor was requested in PreTick; work what was granted.
                        double granted = ctx.Labor.GrantedFraction(this, TaskType.Logistics);
                        double areaCleaned = granted * 0.2 * ArrayAreaM2 * ctx.DtSols;
                        DustFraction = Math.Max(0, DustFraction - DustFraction * areaCleaned / Math.Max(1, ArrayAreaM2));
                    }
                }
            }
            else if (EffectiveFidelity == FidelityLevel.L1_Analytic)
            {
                // Long-run equilibrium: deposition balanced by periodic cleaning at threshold/2.
                DustFraction = _cleaningThreshold.Value * 0.5;
            }
            else
            {
                DustFraction = 0; // folded into the distilled coefficient
            }

            Record(ctx, "solar.output", "Solar output", "kW", OutputKw);
            Record(ctx, "solar.dust", "Array dust obscuration", "%", DustFraction * 100);
            Record(ctx, "solar.installed", "Solar installed rating", "kW", InstalledKwRating);
        }

        public override string StatusLine =>
            $"{OutputKw:F0} kW of {InstalledKwRating:F0} kW rated, dust {DustFraction:P0}";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Array area", ArrayAreaM2, "m²");
                yield return ("Installed rating", InstalledKwRating, "kW");
                yield return ("Array mass", ArrayMassKg / 1000.0, "t");
                yield return ("Dust obscuration", DustFraction * 100, "%");
            }
        }
    }
}
