using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// The humans: metabolism (BVAD rates), labor supply, EVA exposure, radiation dose,
    /// and a sober health model — sustained deprivation of O2/water/food degrades a health
    /// index that first cuts productivity and ultimately causes loss of crew.
    /// </summary>
    public sealed class Crew : SimModule
    {
        public override string DisplayName => "Crew";

        public int Count { get; private set; }
        public double HealthIndex { get; private set; } = 1.0;   // 1 = nominal
        public double CumulativeDoseMsv { get; private set; }    // mission average per crew member
        public int Fatalities { get; private set; }

        private double _hypoxiaSols, _hypercapniaSols, _dehydrationSols, _starvationSols;

        private Param _o2PerCmDay, _co2PerCmDay, _waterPerCmDay, _wastePerCmDay,
                      _kcalPerCmDay, _kcalPerKgFood, _workHoursPerSol, _evaFraction,
                      _doseLimit, _hypoxiaPpO2, _hypercapniaKpa;

        private Store _water, _wasteWater, _food;
        private Habitat _hab;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _o2PerCmDay = p.GetOrRegister("eclss.o2_consumption_kg_cm_day", "O2 consumption", 0.82, "kg/CM-day",
                "NASA BVAD (nominal metabolic load)");
            _co2PerCmDay = p.GetOrRegister("eclss.co2_production_kg_cm_day", "CO2 production", 1.04, "kg/CM-day",
                "NASA BVAD (respiratory quotient ~0.87)");
            _waterPerCmDay = p.GetOrRegister("eclss.water_use_kg_cm_day", "Potable water use (drink+food prep+hygiene)", 3.6, "kg/CM-day",
                "NASA BVAD water balance tables");
            _wastePerCmDay = p.GetOrRegister("eclss.wastewater_kg_cm_day", "Wastewater return (urine+condensate+hygiene)", 3.4, "kg/CM-day",
                "NASA BVAD water balance tables");
            _kcalPerCmDay = p.GetOrRegister("food.kcal_per_cm_day", "Dietary energy requirement", 3040, "kcal/CM-day",
                "NASA BVAD (moderate activity, mixed crew)");
            _kcalPerKgFood = p.GetOrRegister("food.kcal_per_kg_packaged", "Packaged food energy density (as-shipped incl. packaging)", 1650, "kcal/kg",
                "BVAD: ~1.83 kg/CM-day for 3040 kcal incl. packaging");
            _workHoursPerSol = p.GetOrRegister("human_factors.work_hours_per_sol", "Productive work hours per crew per sol", 6.5, "h/sol",
                "ISS experience: ~6.5 h scheduled work/day");
            _evaFraction = p.GetOrRegister("human_factors.eva_fraction", "Fraction of work hours on EVA", 0.15, "",
                "Ops assumption (tunable)");
            _doseLimit = p.GetOrRegister("human_factors.career_dose_limit_msv", "Career radiation dose limit", 600, "mSv",
                "NASA-STD-3001 (2021 update): 600 mSv career");
            _hypoxiaPpO2 = p.GetOrRegister("human_factors.hypoxia_ppo2_kpa", "Hypoxia threshold ppO2", 15.0, "kPa",
                "NASA-STD-3001 minimum alveolar requirement region");
            _hypercapniaKpa = p.GetOrRegister("human_factors.hypercapnia_ppco2_kpa", "Hypercapnia health threshold", 2.0, "kPa",
                "NASA exposure limits: sustained >15 mmHg degrades cognition/health (absolute, not the ops alarm)");

            _water = ctx.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            _wasteWater = ctx.Stores.GetOrCreate("water_waste", Resource.WaterWaste, 0);
            _food = ctx.Stores.GetOrCreate("food", Resource.Food, 0);
            _hab = Engine.Find<Habitat>();
        }

        public void Arrive(SimContext ctx, int people)
        {
            Count += people;
            Log(ctx, EventSeverity.Milestone, $"{people} crew arrived — {Count} now on Mars");
        }

        public void Depart(SimContext ctx, int people)
        {
            Count = Math.Max(0, Count - people);
            Log(ctx, EventSeverity.Milestone, $"{people} crew departed for Earth — {Count} remain");
        }

        public override void PreTick(SimContext ctx)
        {
            if (Count == 0) return;
            // Labor supply: productive hours scaled by health.
            double hours = Count * _workHoursPerSol.Value * ctx.DtSols * Math.Clamp(HealthIndex, 0, 1);
            ctx.Labor.SupplyCrewHours(hours);
        }

        public override void Tick(SimContext ctx)
        {
            if (Count == 0) return;
            double cmDays = Count * ctx.DtSeconds / 86400.0;  // crew-member-days this step

            // --- Breathe ---
            double o2Needed = _o2PerCmDay.Value * cmDays;
            double o2Got = _hab != null ? _hab.DrawO2(o2Needed) : o2Needed;
            _hab?.InjectCO2(_co2PerCmDay.Value * cmDays);

            bool hypoxic = _hab != null && (_hab.PpO2Kpa < _hypoxiaPpO2.Value || o2Got < o2Needed * 0.95);
            _hypoxiaSols = hypoxic ? _hypoxiaSols + ctx.DtSols : 0;

            // Hypercapnia: sustained ppCO2 above ~2 kPa degrades cognition then health
            // (NASA exposure limits; absolute threshold, not the ops alarm target).
            bool hypercapnic = _hab != null && _hab.PpCO2Kpa > _hypercapniaKpa.Value;
            _hypercapniaSols = hypercapnic ? _hypercapniaSols + ctx.DtSols : Math.Max(0, _hypercapniaSols - ctx.DtSols);

            // --- Drink / wash ---
            double waterNeeded = _waterPerCmDay.Value * cmDays;
            double waterGot = _water.Withdraw(waterNeeded);
            _wasteWater.Deposit(_wastePerCmDay.Value * cmDays * (waterGot / Math.Max(1e-9, waterNeeded)));
            _dehydrationSols = waterGot < waterNeeded * 0.7 ? _dehydrationSols + ctx.DtSols : Math.Max(0, _dehydrationSols - ctx.DtSols);

            // --- Eat ---
            double kcalNeeded = _kcalPerCmDay.Value * cmDays;
            double foodKgNeeded = kcalNeeded / _kcalPerKgFood.Value;
            double foodGot = _food.Withdraw(foodKgNeeded);
            _starvationSols = foodGot < foodKgNeeded * 0.8 ? _starvationSols + ctx.DtSols : Math.Max(0, _starvationSols - 0.5 * ctx.DtSols);

            // --- Radiation ---
            double doseRate = ctx.Env.SurfaceDoseMsvPerSol;
            double shield = _hab?.ShieldingFactor ?? 1.0;
            double evaFrac = _evaFraction.Value * (_workHoursPerSol.Value / 24.66);
            CumulativeDoseMsv += doseRate * (evaFrac * 1.0 + (1 - evaFrac) * shield) * ctx.DtSols;
            if (CumulativeDoseMsv > _doseLimit.Value && ctx.Clock.StepCount % 1000 == 0)
                Log(ctx, EventSeverity.Warning, $"Average crew dose {CumulativeDoseMsv:F0} mSv exceeds career limit");

            // --- Health integration (deprivation -> degradation -> fatalities) ---
            double healthDrain = 0;
            if (_hypoxiaSols > 0.05) healthDrain += 2.0 * ctx.DtSols;         // minutes matter; sol-scale drain is severe
            if (_hypercapniaSols > 0.5) healthDrain += 0.1 * ctx.DtSols;
            if (_dehydrationSols > 1) healthDrain += 0.25 * ctx.DtSols;
            if (_starvationSols > 10) healthDrain += 0.03 * ctx.DtSols;
            double recovery = healthDrain == 0 ? 0.02 * ctx.DtSols : 0;
            HealthIndex = Math.Clamp(HealthIndex - healthDrain + recovery, 0, 1);

            if (HealthIndex <= 0 && Count > 0)
            {
                int lost = Math.Max(1, (int)(Count * 0.1));
                Count -= lost;
                Fatalities += lost;
                HealthIndex = 0.3; // survivors stabilize on remaining margins
                Log(ctx, EventSeverity.Critical, $"LOSS OF CREW: {lost} fatalities ({Count} remain)");
            }

            Record(ctx, "crew.count", "Crew on Mars", "people", Count);
            Record(ctx, "crew.health", "Crew health index", "", HealthIndex);
            Record(ctx, "crew.dose", "Cumulative dose (avg)", "mSv", CumulativeDoseMsv);
            Record(ctx, "crew.food_days", "Food reserve at current crew", "sols",
                Count > 0 ? _food.AmountKg * _kcalPerKgFood.Value / (_kcalPerCmDay.Value * Count * 1.0275) : 0);
            Record(ctx, "crew.water_days", "Water reserve at current crew", "sols",
                Count > 0 ? _water.AmountKg / (_waterPerCmDay.Value * Count * 1.0275) : 0);
        }

        public override string StatusLine => Count == 0 ? "No crew on surface"
            : $"{Count} crew, health {HealthIndex:P0}, dose {CumulativeDoseMsv:F0} mSv";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Crew", Count, "people");
                yield return ("Health index", HealthIndex * 100, "%");
                yield return ("Avg dose", CumulativeDoseMsv, "mSv");
                yield return ("Fatalities", Fatalities, "");
            }
        }
    }
}
