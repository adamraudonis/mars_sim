using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Water production from subsurface ice (excavate-and-heat baseline; a Rodwell would set
    /// a lower kWh/kg — that's a parameter, not a code change). Produces raw water, then a
    /// small cleanup plant (perchlorate removal, filtration) lifts it to potable/feedstock.
    /// Heavily robot-friendly work: labor demand is mostly excavation/hauling.
    /// </summary>
    public sealed class IceMine : SimModule
    {
        public override string DisplayName => "Ice mine";

        /// <summary>Installed extraction capacity, kg water per sol.</summary>
        public double CapacityKgPerSol { get; set; }

        public double ProductionKgPerSol { get; private set; }

        private Param _kwhPerKg, _cleanupKwhPerKg, _oreWaterFraction, _laborHoursPerTonne,
                      _rigMassPerKgSol, _cleanupLoss;

        private Store _feedstock, _water, _regolith;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _kwhPerKg = p.GetOrRegister("isru_water.extraction_kwh_per_kg", "Water extraction energy (excavate+heat)", 1.5, "kWh/kg H2O",
                "NASA ISRU studies: 0.9-2.5 kWh/kg for icy regolith thermal extraction (Kleinhenz)");
            _cleanupKwhPerKg = p.GetOrRegister("isru_water.cleanup_kwh_per_kg", "Water cleanup energy", 0.1, "kWh/kg",
                "Filtration + perchlorate treatment estimate");
            _cleanupLoss = p.GetOrRegister("isru_water.cleanup_loss_fraction", "Cleanup reject fraction", 0.05, "",
                "Brine/contaminant reject estimate");
            _oreWaterFraction = p.GetOrRegister("isru_water.ore_water_fraction", "Water content of mined material", 0.6, "",
                "SWIM: mid-latitude excess ice >50% purity in places (Morgan et al. 2021)");
            _laborHoursPerTonne = p.GetOrRegister("isru_water.labor_hours_per_tonne", "Mining labor per tonne water", 4, "crew-eq h/t",
                "Estimate: excavator ops, hauling, rig maintenance");
            _rigMassPerKgSol = p.GetOrRegister("isru_water.rig_mass_kg_per_kg_sol", "Mining system specific mass", 15, "kg per (kg/sol)",
                "RASSOR-class excavators + processing plant scaling");

            _feedstock = ctx.Stores.GetOrCreate("water_feedstock", Resource.WaterFeedstock, 200000);
            _water = ctx.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            _regolith = ctx.Stores.GetOrCreate("regolith", Resource.Regolith, double.MaxValue);
        }

        public double SystemMassKg => CapacityKgPerSol * _rigMassPerKgSol.Value;

        /// <summary>Excavator wear tracks actual throughput, not calendar time.</summary>
        public override double FailureDutyCycle => CapacityKgPerSol <= 0 ? 0
            : Math.Clamp(ProductionKgPerSol / Math.Max(1, CapacityKgPerSol), 0.05, 1.0);

        public override void PreTick(SimContext ctx)
        {
            if (CapacityKgPerSol <= 0) return;
            // Scale the request by how much the tank farm can actually absorb, so a full
            // tank doesn't bill the bus (and drain the battery) for phantom production.
            double intake = Math.Min(1.0, _water.FreeKg / Math.Max(1.0, CapacityKgPerSol * ctx.DtSols));
            double fullKw = CapacityKgPerSol * (_kwhPerKg.Value + _cleanupKwhPerKg.Value) / Units.SolHours * intake;
            if (fullKw > 0) ctx.Power.Request(this, fullKw, LoadPriority.Normal);
            ctx.Labor.Request(this, TaskType.IsruOps,
                CapacityKgPerSol / 1000.0 * _laborHoursPerTonne.Value * ctx.DtSols, LaborPriority.Normal);
        }

        public override void Tick(SimContext ctx)
        {
            if (CapacityKgPerSol <= 0) { ProductionKgPerSol = 0; return; }

            double powerGrant = ctx.Power.GrantedFraction(this);
            double laborGrant = ctx.Labor.GrantedFraction(this, TaskType.IsruOps);
            // Mining hard-requires both energy (heat) and labor (excavation/hauling),
            // and throttles when the tank farm is full rather than venting product.
            double made = CapacityKgPerSol * ctx.DtSols * powerGrant * (0.3 + 0.7 * laborGrant) * CapacityFactor;
            made = Math.Min(made, _water.FreeKg / Math.Max(0.01, 1 - _cleanupLoss.Value));

            if (made > 1e-9)
            {
                _regolith.Deposit(made / Math.Max(0.05, _oreWaterFraction.Value) * (1 - _oreWaterFraction.Value));
                double clean = made * (1 - _cleanupLoss.Value);
                _water.Deposit(clean);
                _feedstock.Deposit(made - clean); // reject stream retained as low-grade feedstock
            }
            ProductionKgPerSol = made / ctx.DtSols;

            Record(ctx, "icemine.production", "Water production", "kg/sol", ProductionKgPerSol);
        }

        public override string StatusLine => CapacityKgPerSol <= 0 ? "Not installed"
            : $"{ProductionKgPerSol:F0} kg/sol of {CapacityKgPerSol:F0} kg/sol";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Capacity", CapacityKgPerSol, "kg/sol");
                yield return ("Production", ProductionKgPerSol, "kg/sol");
                yield return ("System mass", SystemMassKg / 1000.0, "t");
            }
        }
    }
}
