using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// The pressurized envelope: cabin atmosphere is modeled as a well-mixed ideal-gas volume
    /// (O2/CO2/N2 masses -> partial pressures). Crew, ECLSS and greenhouse exchange gas with
    /// the cabin through this module, so the UI charts the ppO2/ppCO2 the crew actually
    /// breathes. Handles structural leakage, N2 makeup, emergency O2 makeup from reserve,
    /// and the habitat's baseline electrical load (thermal, avionics, lighting).
    /// </summary>
    public sealed class Habitat : SimModule
    {
        public override string DisplayName => "Habitat";

        public double PressurizedVolumeM3 { get; private set; }

        // Cabin gas inventory, kg.
        public double CabinO2Kg { get; private set; }
        public double CabinCO2Kg { get; private set; }
        public double CabinN2Kg { get; private set; }

        public double CabinTempC { get; set; } = 22.0;

        private const double R = 8.314;
        private const double MolO2 = 0.032, MolCO2 = 0.044, MolN2 = 0.028;

        private Param _totalPressure, _ppO2Set, _ppCO2Max, _purgePpCO2, _leakPerDay, _basePowerPerM3,
                      _powerPerCrew, _shieldFactor;

        private Store _o2Store, _n2Store;
        private bool _purging;

        public double PpO2Kpa => PartialPressureKpa(CabinO2Kg, MolO2);
        public double PpCO2Kpa => PartialPressureKpa(CabinCO2Kg, MolCO2);
        public double PpN2Kpa => PartialPressureKpa(CabinN2Kg, MolN2);
        public double TotalPressureKpa => PpO2Kpa + PpCO2Kpa + PpN2Kpa;

        /// <summary>Fraction of the ambient GCR dose seen inside (regolith shielding etc.).</summary>
        public double ShieldingFactor => _shieldFactor.Value;

        private double PartialPressureKpa(double kg, double molarMassKg)
        {
            if (PressurizedVolumeM3 <= 0) return 0;
            double tK = CabinTempC + 273.15;
            return (kg / molarMassKg) * R * tK / PressurizedVolumeM3 / 1000.0;
        }

        private double KgForPartialPressure(double kpa, double molarMassKg)
        {
            double tK = CabinTempC + 273.15;
            return kpa * 1000.0 * PressurizedVolumeM3 * molarMassKg / (R * tK);
        }

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _totalPressure = p.GetOrRegister("eclss.cabin_total_pressure", "Cabin total pressure", 101.3, "kPa",
                "ISS standard; 70.3 kPa is the exploration alternative (BVAD)");
            _ppO2Set = p.GetOrRegister("eclss.ppo2_setpoint", "ppO2 setpoint", 21.2, "kPa",
                "ISS 19.5-23.1 kPa range (NASA-STD-3001)");
            _ppCO2Max = p.GetOrRegister("eclss.ppco2_limit", "ppCO2 alarm limit", 0.667, "kPa",
                "5.3 mmHg 180-day SMAC (NASA-STD-3001; ISS ops target lower)");
            _purgePpCO2 = p.GetOrRegister("eclss.emergency_purge_ppco2_kpa", "Emergency open-loop purge threshold", 2.0, "kPa",
                "Hold cabin below the ~2 kPa sustained-harm level (NASA exposure limits); independent of the ops alarm target");
            _leakPerDay = p.GetOrRegister("eclss.leakage_fraction_per_day", "Cabin leakage", 0.0005, "fraction/day",
                "ISS-class: ~0.05%/day of cabin mass (BVAD structural leak spec)");
            _basePowerPerM3 = p.GetOrRegister("eclss.hab_base_power_w_per_m3", "Habitat baseline power density", 12, "W/m3",
                "Estimate: thermal control + avionics + lighting for insulated Mars hab");
            _powerPerCrew = p.GetOrRegister("eclss.hab_power_per_crew_kw", "Additional habitat power per crew", 0.3, "kW",
                "Galley, hygiene, plug loads (BVAD)");
            _shieldFactor = p.GetOrRegister("human_factors.hab_shielding_factor", "Habitat GCR shielding factor", 0.5, "",
                "Unburied hab ~0.5; 2-3 m regolith cover ~0.15 (Simonsen & Nealy)");

            _o2Store = ctx.Stores.GetOrCreate("o2_reserve", Resource.O2, 0);
            _n2Store = ctx.Stores.GetOrCreate("n2_reserve", Resource.N2, 0);
        }

        /// <summary>Add pressurized volume (new module or ship). Arrives unpressurized; the cabin gas dilutes and makeup flows kick in.</summary>
        public void AddVolume(SimContext ctx, double m3, bool arrivesPressurized = false)
        {
            PressurizedVolumeM3 += m3;
            if (arrivesPressurized)
            {
                CabinO2Kg += KgForPartialPressure(_ppO2Set.Value, MolO2) * m3 / Math.Max(1, PressurizedVolumeM3);
                CabinN2Kg += KgForPartialPressure(_totalPressure.Value - _ppO2Set.Value, MolN2) * m3 / Math.Max(1, PressurizedVolumeM3);
            }
            Log(ctx, EventSeverity.Milestone, $"Pressurized volume now {PressurizedVolumeM3:F0} m³");
        }

        /// <summary>Mass of O2 needed to raise cabin ppO2 by the given kPa (0 if no volume).</summary>
        public double KgToRaisePpO2(double kpa) => kpa <= 0 ? 0 : KgForPartialPressure(kpa, MolO2);

        // --- Gas exchange API used by Crew / ECLSS / Greenhouse ---
        public void InjectO2(double kg) => CabinO2Kg += Math.Max(0, kg);
        public double DrawO2(double kg) { double got = Math.Min(kg, CabinO2Kg); CabinO2Kg -= got; return got; }
        public void InjectCO2(double kg) => CabinCO2Kg += Math.Max(0, kg);
        public double DrawCO2(double kg) { double got = Math.Min(kg, CabinCO2Kg); CabinCO2Kg -= got; return got; }

        public override void PreTick(SimContext ctx)
        {
            double baseKw = PressurizedVolumeM3 * _basePowerPerM3.Value / 1000.0
                            + (Engine.Find<Crew>()?.Count ?? 0) * _powerPerCrew.Value;
            ctx.Power.Request(this, baseKw, LoadPriority.Critical);
        }

        public override void Tick(SimContext ctx)
        {
            if (PressurizedVolumeM3 <= 0) return;

            // Structural leakage, proportional to each gas partial pressure.
            double leak = _leakPerDay.Value * (ctx.DtSeconds / 86400.0);
            CabinO2Kg *= 1 - leak;
            CabinCO2Kg *= 1 - leak;
            CabinN2Kg *= 1 - leak;

            // N2 makeup toward total pressure setpoint (N2 is the inert filler).
            double n2Deficit = KgForPartialPressure(
                Math.Max(0, _totalPressure.Value - PpO2Kpa - PpCO2Kpa - PpN2Kpa), MolN2);
            if (n2Deficit > 1e-6)
                CabinN2Kg += _n2Store.Withdraw(n2Deficit);

            // Emergency O2 makeup straight from reserve if ppO2 sags below 95% of setpoint
            // (normally the OGA keeps up; this is the bottled backup).
            double ppO2 = PpO2Kpa;
            if (ppO2 < _ppO2Set.Value * 0.95)
            {
                double deficitKg = KgForPartialPressure(_ppO2Set.Value - ppO2, MolO2);
                CabinO2Kg += _o2Store.Withdraw(deficitKg);
            }

            // Emergency open-loop purge (Apollo-13 style): if scrubbers cannot hold ppCO2 and
            // gas reserves exist, vent cabin air and repressurize from stores — dumps CO2 at
            // the cost of O2/N2 reserve mass. Keeps a suffocation death from happening while
            // tonnes of oxygen sit in tanks; the reserve drain is the visible price.
            if (PpCO2Kpa > _purgePpCO2.Value && _o2Store.AmountKg > 1)
            {
                double ventFrac = Math.Min(0.5, 0.6 * ctx.DtSols); // ~60% of cabin volume per sol: purge equilibrium well below the harm threshold
                double o2Needed = CabinO2Kg * ventFrac;
                double o2Got = _o2Store.Withdraw(o2Needed);
                ventFrac *= o2Needed > 1e-9 ? o2Got / o2Needed : 0; // only vent what we can replace
                CabinCO2Kg *= 1 - ventFrac;
                CabinN2Kg -= CabinN2Kg * ventFrac - _n2Store.Withdraw(CabinN2Kg * ventFrac);
                CabinO2Kg += o2Got - CabinO2Kg * ventFrac;
                if (!_purging)
                {
                    _purging = true;
                    Log(ctx, EventSeverity.Critical, "EMERGENCY: open-loop CO2 purge — venting cabin air, repressurizing from reserves");
                }
            }
            else if (_purging && PpCO2Kpa < _purgePpCO2.Value * 0.5)
            {
                _purging = false;
                Log(ctx, EventSeverity.Milestone, "Open-loop CO2 purge secured — scrubbers holding");
            }

            if (PpCO2Kpa > _ppCO2Max.Value && ctx.Clock.StepCount % 25 == 0)
                Log(ctx, EventSeverity.Warning, $"ppCO2 {PpCO2Kpa * 7.50062:F1} mmHg above limit");

            Record(ctx, "hab.ppo2", "Cabin ppO₂", "kPa", PpO2Kpa);
            Record(ctx, "hab.ppco2", "Cabin ppCO₂", "kPa", PpCO2Kpa);
            Record(ctx, "hab.pressure", "Cabin pressure", "kPa", TotalPressureKpa);
            Record(ctx, "hab.volume", "Pressurized volume", "m³", PressurizedVolumeM3);
        }

        public override string StatusLine =>
            $"{TotalPressureKpa:F1} kPa, ppO₂ {PpO2Kpa:F1} kPa, ppCO₂ {PpCO2Kpa * 7.50062:F1} mmHg";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Volume", PressurizedVolumeM3, "m³");
                yield return ("ppO₂", PpO2Kpa, "kPa");
                yield return ("ppCO₂", PpCO2Kpa * 7.50062, "mmHg");
                yield return ("Pressure", TotalPressureKpa, "kPa");
            }
        }
    }
}
