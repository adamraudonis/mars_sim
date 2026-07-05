using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Fission surface power: N reactor units of a given class (Kilopower 10 kWe or
    /// FSP 40 kWe). Output is essentially weather-independent — that is the whole trade
    /// against solar. Units degrade slowly and retire at end-of-life; individual unit
    /// failures come through the MaintenanceSystem (CapacityFactor).
    /// </summary>
    public sealed class NuclearPlant : SimModule
    {
        public override string DisplayName => "Fission power plant";
        public override FidelityLevel MaxFidelity => FidelityLevel.L1_Analytic;

        public int Units { get; set; }
        public double OutputKw { get; private set; }
        private double _ageYears;

        private Param _unitKwe, _unitMassKg, _lifetimeYears, _deployCrewHours, _keepOutM;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _unitKwe = p.GetOrRegister("power_nuclear.unit_kwe", "Reactor unit electrical power", 40, "kWe",
                "NASA Fission Surface Power project target (2022 industry awards)");
            _unitMassKg = p.GetOrRegister("power_nuclear.unit_mass_kg", "Reactor unit mass (landed, shielded)", 6000, "kg",
                "FSP 40 kWe target <=6 t (NASA 2022); Kilopower 10 kWe ~1500 kg (KRUSTY-based)");
            _lifetimeYears = p.GetOrRegister("power_nuclear.lifetime_years", "Design lifetime", 10, "years",
                "FSP requirement: 10 years unattended");
            _deployCrewHours = p.GetOrRegister("power_nuclear.deploy_crew_hours", "Deployment labor per unit", 40, "crew-eq h",
                "Estimate: robotic emplacement + cabling");
            _keepOutM = p.GetOrRegister("power_nuclear.keep_out_distance_m", "Crew keep-out distance (unshielded side)", 1000, "m",
                "Kilopower siting studies (Gibson et al. 2017)");
        }

        public double PlantMassKg => Units * _unitMassKg.Value;

        public override void PreTick(SimContext ctx)
        {
            double eol = _ageYears > _lifetimeYears.Value ? 0.0 : 1.0;
            // Small burnup/degradation ramp: 5% linear over life (Stirling convertor margin).
            double degradation = 1.0 - 0.05 * Math.Min(1.0, _ageYears / _lifetimeYears.Value);
            OutputKw = Units * _unitKwe.Value * degradation * CapacityFactor * eol;
            ctx.Power.Offer(OutputKw);
        }

        public override void Tick(SimContext ctx)
        {
            double prevAge = _ageYears;
            _ageYears += ctx.DtSeconds / (365.25 * 86400.0);
            if (prevAge <= _lifetimeYears.Value && _ageYears > _lifetimeYears.Value && Units > 0)
                Log(ctx, EventSeverity.Critical, $"Reactor fleet reached end of design life ({_lifetimeYears.Value:F0} yr)");

            Record(ctx, "nuclear.output", "Nuclear output", "kW", OutputKw);
        }

        public override string StatusLine => Units == 0 ? "No units deployed"
            : $"{Units} × {_unitKwe.Value:F0} kWe, output {OutputKw:F0} kW";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Units", Units, "");
                yield return ("Output", OutputKw, "kW");
                yield return ("Plant mass", PlantMassKg / 1000.0, "t");
                yield return ("Fleet age", _ageYears, "yr");
            }
        }
    }
}
