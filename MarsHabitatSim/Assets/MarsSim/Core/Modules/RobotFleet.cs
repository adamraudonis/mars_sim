using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Humanoid robot workforce (Optimus-class, marked speculative in the parameter DB).
    /// Robots supply robot-hours to the LaborPool with per-task effectiveness ratios; they
    /// draw charging power, take maintenance labor themselves, and a fraction is down at any
    /// time. Setting count = 0 recovers the humans-only baseline — the labor trade is then
    /// just a scenario diff.
    /// </summary>
    public sealed class RobotFleet : SimModule
    {
        public override string DisplayName => "Robot fleet";

        public int Count { get; set; }

        private Param _workHoursPerSol, _availability, _chargeKwhPerWorkHour,
                      _effExcavation, _effConstruction, _effLogistics, _effAgriculture,
                      _effMaintenance, _effIsru, _maintHoursPerRobotSol, _unitMassKg;

        private double _chargeKw;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _workHoursPerSol = p.GetOrRegister("robots.work_hours_per_sol", "Robot work hours per sol", 16, "h/sol",
                "Duty cycle limited by charging + thermal (Tesla claims, speculative)");
            _availability = p.GetOrRegister("robots.availability", "Fleet availability", 0.8, "",
                "Estimate: dust, wear, software — mark speculative");
            _chargeKwhPerWorkHour = p.GetOrRegister("robots.charge_kwh_per_work_hour", "Charging energy per work hour", 0.35, "kWh/h",
                "Optimus ~2.3 kWh battery / ~6-8 h work (company claim)");
            _unitMassKg = p.GetOrRegister("robots.unit_mass_kg", "Robot unit mass", 60, "kg",
                "Optimus Gen 2 ~57-73 kg (company claim)");
            _effExcavation = p.GetOrRegister("robots.effectiveness_excavation", "Robot effectiveness: excavation/mining ops", 0.7, "crew-eq",
                "Structured outdoor task, teleop/scripted");
            _effConstruction = p.GetOrRegister("robots.effectiveness_construction", "Robot effectiveness: construction", 0.5, "crew-eq",
                "Semi-structured assembly");
            _effLogistics = p.GetOrRegister("robots.effectiveness_logistics", "Robot effectiveness: hauling/cleaning", 0.8, "crew-eq",
                "Warehouse-analog structured work");
            _effAgriculture = p.GetOrRegister("robots.effectiveness_agriculture", "Robot effectiveness: greenhouse tending", 0.4, "crew-eq",
                "Delicate manipulation, lower ratio");
            _effMaintenance = p.GetOrRegister("robots.effectiveness_maintenance", "Robot effectiveness: equipment repair", 0.3, "crew-eq",
                "Diagnosis-heavy work stays human-led");
            _effIsru = p.GetOrRegister("robots.effectiveness_isru_ops", "Robot effectiveness: plant operations", 0.6, "crew-eq",
                "Monitoring rounds, valve/panel work");
            _maintHoursPerRobotSol = p.GetOrRegister("robots.maintenance_hours_per_robot_sol", "Upkeep labor per robot", 0.05, "crew-eq h/sol",
                "Estimate: ~1 shop-hour per robot per 20 sols");
        }

        public double FleetMassKg => Count * _unitMassKg.Value;

        public override void PreTick(SimContext ctx)
        {
            if (Count == 0) return;

            // Publish effectiveness ratios (params may have changed live).
            ctx.Labor.SetRobotEffectiveness(TaskType.Maintenance, _effMaintenance.Value);
            ctx.Labor.SetRobotEffectiveness(TaskType.IsruOps, _effIsru.Value);
            ctx.Labor.SetRobotEffectiveness(TaskType.Agriculture, _effAgriculture.Value);
            ctx.Labor.SetRobotEffectiveness(TaskType.Construction, _effConstruction.Value);
            ctx.Labor.SetRobotEffectiveness(TaskType.Logistics, _effLogistics.Value);
            ctx.Labor.SetRobotEffectiveness(TaskType.Science, 0.2);

            // Work hours scale with the previous step's charging grant: a power-starved
            // fleet genuinely delivers less labor (energy-labor coupling).
            double hours = Count * _availability.Value * CapacityFactor * _workHoursPerSol.Value * ctx.DtSols
                           * Math.Clamp(0.2 + 0.8 * _lastChargeGrant, 0, 1);
            ctx.Labor.SupplyRobotHours(hours);

            _chargeKw = Count * _availability.Value * CapacityFactor * _workHoursPerSol.Value * ctx.DtSols
                        * _chargeKwhPerWorkHour.Value / ctx.DtHours;
            ctx.Power.Request(this, _chargeKw, LoadPriority.High);

            // Robots need human/robot shop time.
            ctx.Labor.Request(this, TaskType.Maintenance,
                Count * _maintHoursPerRobotSol.Value * ctx.DtSols, LaborPriority.High);
        }

        private double _lastChargeGrant = 1.0;

        public override void Tick(SimContext ctx)
        {
            if (Count == 0) return;
            // Power-starved charging reduces next step's labor supply (see PreTick).
            double grant = ctx.Power.GrantedFraction(this);
            _lastChargeGrant = grant;
            if (grant < 0.5 && ctx.Clock.StepCount % 50 == 0)
                Log(ctx, EventSeverity.Warning, "Robot charging power-starved — fleet output reduced");

            Record(ctx, "robots.count", "Robots", "", Count);
            Record(ctx, "robots.hours", "Robot hours supplied", "h/sol",
                Count * _availability.Value * CapacityFactor * _workHoursPerSol.Value * grant);
            Record(ctx, "robots.charge_kw", "Robot charging", "kW", _chargeKw * grant);
        }

        public override string StatusLine => Count == 0 ? "None deployed"
            : $"{Count} robots, availability {_availability.Value * CapacityFactor:P0}";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Robots", Count, "");
                yield return ("Fleet mass", FleetMassKg / 1000.0, "t");
                yield return ("Charging draw", _chargeKw, "kW");
            }
        }
    }
}
