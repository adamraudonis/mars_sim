using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Li-ion energy storage. The PowerBus owns the instantaneous charge/discharge decision;
    /// this module owns sizing, mass accounting, and calendar/cycle fade.
    /// </summary>
    public sealed class BatteryBank : SimModule
    {
        public override string DisplayName => "Battery storage";

        /// <summary>Nameplate capacity as landed, kWh (before fade).</summary>
        public double NameplateKwh { get; set; }

        private double _ageYears;
        private Param _specificEnergy, _roundTrip, _minSoc, _fadePerYear, _maxCRate, _criticalReserve;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _specificEnergy = p.GetOrRegister("power_solar.battery_wh_per_kg", "Battery pack specific energy", 180, "Wh/kg",
                "Li-ion aerospace packs 150-200 Wh/kg (BVAD power tables)");
            _roundTrip = p.GetOrRegister("power_solar.battery_round_trip_eff", "Battery round-trip efficiency", 0.94, "",
                "Li-ion typical 92-96%");
            _minSoc = p.GetOrRegister("power_solar.battery_min_soc", "Battery minimum state of charge", 0.20, "fraction",
                "DoD 80% for cycle life (aerospace practice)");
            _fadePerYear = p.GetOrRegister("power_solar.battery_fade_per_year", "Capacity fade", 0.02, "fraction/year",
                "Li-ion calendar+cycle fade under daily cycling");
            _maxCRate = p.GetOrRegister("power_solar.battery_max_c_rate", "Max charge/discharge C-rate", 0.5, "1/h",
                "Conservative thermal limit");
            _criticalReserve = p.GetOrRegister("power_solar.battery_reserve_soc_for_critical", "Battery SoC reserved for critical loads", 0.5, "fraction",
                "Ops policy: below this SoC, ISRU/greenhouse loads are shed to protect life support overnight");

            SyncBus(ctx);
            // Start the day-night buffer full.
            ctx.Power.Battery.EnergyKwh = ctx.Power.Battery.CapacityKwh;
        }

        public double MassKg => NameplateKwh * 1000.0 / _specificEnergy.Value;

        private void SyncBus(SimContext ctx)
        {
            var b = ctx.Power.Battery;
            double faded = NameplateKwh * System.Math.Pow(1 - _fadePerYear.Value, _ageYears);
            b.CapacityKwh = faded * CapacityFactor;
            b.ChargeEfficiency = _roundTrip.Value;
            b.MinSocFraction = _minSoc.Value;
            b.CriticalReserveSoc = _criticalReserve.Value;
            b.MaxChargeRateKw = faded * _maxCRate.Value;
            b.MaxDischargeRateKw = faded * _maxCRate.Value;
            if (b.EnergyKwh > b.CapacityKwh) b.EnergyKwh = b.CapacityKwh;
        }

        public override void PreTick(SimContext ctx) => SyncBus(ctx);

        public override void Tick(SimContext ctx)
        {
            _ageYears += ctx.DtSeconds / (365.25 * 86400.0);
            Record(ctx, "battery.capacity", "Battery capacity", "kWh", ctx.Power.Battery.CapacityKwh);
            Record(ctx, "battery.energy", "Battery energy", "kWh", ctx.Power.Battery.EnergyKwh);
        }

        public override string StatusLine =>
            $"{Engine.Power.Battery.EnergyKwh:F0} / {Engine.Power.Battery.CapacityKwh:F0} kWh ({Engine.Power.Battery.Soc:P0})";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Capacity", Engine.Power.Battery.CapacityKwh, "kWh");
                yield return ("State of charge", Engine.Power.Battery.Soc * 100, "%");
                yield return ("Bank mass", MassKg / 1000.0, "t");
            }
        }
    }
}
