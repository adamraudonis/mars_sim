using System;
using System.Collections.Generic;
using System.Linq;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>One repairable component population inside a module (e.g. "OGA pumps ×2").</summary>
    public sealed class ComponentGroup
    {
        public string Id;
        public SimModule Owner;
        public string EquipmentClass;      // spares are pooled by class
        public string FunctionTag;         // sub-function within the owner (null = whole module)
        public int Total;                  // installed units
        public int Working;                // currently functional
        public double MtbfHours;           // per unit
        public double RepairCrewHours;     // per failure
        public double SpareMassKg;         // per replacement unit
        public double WeibullShape = 1.0;  // 1 = exponential (random); >1 = wear-out

        public double AgeHours;            // for Weibull hazard
        public double CapacityContribution => Total > 0 ? (double)Working / Total : 1.0;
    }

    internal sealed class RepairJob
    {
        public ComponentGroup Group;
        public double HoursRemaining;
        public bool SpareConsumed;
        public double QueuedSol;
    }

    /// <summary>
    /// Stochastic failure + repair + sparing engine (the Owens &amp; de Weck problem).
    /// Modules register component groups; failures are sampled per step from the hazard rate
    /// (k-factor inflates predictions, matching flight experience), repairs consume
    /// maintenance labor and class-pooled spares. A failed unit without a spare stays down —
    /// with no resupply for ~26 months, spares mass is a survival parameter, and Monte Carlo
    /// runs over the master seed quantify it.
    /// </summary>
    public sealed class MaintenanceSystem : SimModule
    {
        public override string DisplayName => "Maintenance & spares";

        private readonly List<ComponentGroup> _groups = new();
        private readonly List<RepairJob> _queue = new();
        private readonly Dictionary<string, int> _sparesByClass = new();

        private Param _kFactor, _preventiveHoursPerGroupSol, _cannibalize;
        private Store _sparesMass;

        public IReadOnlyList<ComponentGroup> Groups => _groups;
        public int QueueLength => _queue.Count;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _kFactor = p.GetOrRegister("reliability.k_factor", "Failure rate uncertainty multiplier", 2.0, "",
                "ISS experience: observed rates ~2x predictions early (Stromgren)");
            _preventiveHoursPerGroupSol = p.GetOrRegister("reliability.preventive_hours_per_group_sol", "Preventive maintenance per component group", 0.05, "crew-eq h/sol",
                "ISS: ~2-10 h/wk preventive across systems, scaled per group");
            _cannibalize = p.GetOrRegister("reliability.allow_cannibalization", "Cannibalize failed units for spares (0/1)", 1, "bool",
                "Ops policy");
            _sparesMass = ctx.Stores.GetOrCreate("spares", Resource.SparesMass, 1e9);
        }

        public ComponentGroup Register(SimModule owner, string id, string equipmentClass, int count,
            double mtbfHours, double repairCrewHours, double spareMassKg, double weibullShape = 1.0,
            string functionTag = null)
        {
            var g = new ComponentGroup
            {
                Id = id, Owner = owner, EquipmentClass = equipmentClass, FunctionTag = functionTag,
                Total = count, Working = count,
                MtbfHours = mtbfHours, RepairCrewHours = repairCrewHours, SpareMassKg = spareMassKg,
                WeibullShape = weibullShape,
            };
            _groups.Add(g);
            return g;
        }

        /// <summary>
        /// Availability of one sub-function of a module (min over its tagged groups x module-wide
        /// groups). A dead water processor must not zero the CO2 scrubbers.
        /// </summary>
        public double FunctionCapacity(SimModule owner, string functionTag)
        {
            double cap = 1.0;
            foreach (var g in _groups)
            {
                if (g.Owner != owner) continue;
                if (g.FunctionTag == null || g.FunctionTag == functionTag)
                    cap = Math.Min(cap, g.CapacityContribution);
            }
            return cap;
        }

        /// <summary>Grow an installed population (new hardware lands).</summary>
        public void AddUnits(string groupId, int count)
        {
            var g = _groups.FirstOrDefault(x => x.Id == groupId);
            if (g == null) return;
            g.Total += count;
            g.Working += count;
        }

        public void AddSpares(string equipmentClass, int units, double unitMassKg)
        {
            _sparesByClass.TryGetValue(equipmentClass, out int cur);
            _sparesByClass[equipmentClass] = cur + units;
            _sparesMass.Deposit(units * unitMassKg);
        }

        public int SpareCount(string equipmentClass)
            => _sparesByClass.TryGetValue(equipmentClass, out int n) ? n : 0;

        private double _requestedCorrectiveHours;

        public override void PreTick(SimContext ctx)
        {
            double preventive = _groups.Count * _preventiveHoursPerGroupSol.Value * ctx.DtSols;
            double corrective = 0;
            foreach (var j in _queue)
            {
                // Jobs stuck waiting for a spare (with no cannibalization pair) can't be
                // worked — don't charge the labor market for them.
                if (!j.SpareConsumed && SpareCount(j.Group.EquipmentClass) == 0 &&
                    _queue.Count(q => q.Group == j.Group && !q.SpareConsumed) < 2)
                    continue;
                corrective += Math.Min(j.HoursRemaining, ctx.DtSols * 8);
            }
            _requestedCorrectiveHours = corrective;
            if (preventive + corrective > 0)
                ctx.Labor.Request(this, TaskType.Maintenance, preventive + corrective, LaborPriority.High);
        }

        public override void Tick(SimContext ctx)
        {
            double laborGrant = ctx.Labor.GrantedFraction(this, TaskType.Maintenance);

            // --- Sample failures (per operating hour: duty cycle from the owner) ---
            foreach (var g in _groups)
            {
                if (g.Working <= 0 || g.MtbfHours <= 0) continue;
                double duty = Math.Clamp(g.Owner?.FailureDutyCycle ?? 1.0, 0, 1);
                if (duty <= 0) continue;
                g.AgeHours += ctx.DtHours * duty;
                double hazardPerHour = duty * _kFactor.Value / g.MtbfHours;
                if (g.WeibullShape > 1.001)
                {
                    // Weibull hazard h(t) = (k/λ)(t/λ)^(k-1), λ chosen so mean = MTBF.
                    double lambda = g.MtbfHours / GammaMeanFactor(g.WeibullShape);
                    hazardPerHour = duty * _kFactor.Value * (g.WeibullShape / lambda)
                                    * Math.Pow(Math.Max(1e-6, g.AgeHours) / lambda, g.WeibullShape - 1);
                }
                // Poisson draw so multi-unit groups can shed several units in one step
                // (a single Bernoulli undercounts for large fleets / long timesteps).
                double lambdaStep = hazardPerHour * ctx.DtHours * g.Working;
                int failures = Math.Min(g.Working, SamplePoisson(lambdaStep));
                for (int n = 0; n < failures; n++)
                {
                    g.Working--;
                    _queue.Add(new RepairJob { Group = g, HoursRemaining = g.RepairCrewHours, QueuedSol = ctx.Clock.Sol });
                    Log(ctx, EventSeverity.Warning,
                        $"FAILURE: {g.Id} ({g.Owner.DisplayName}) — {g.Working}/{g.Total} units up, spare {(SpareCount(g.EquipmentClass) > 0 ? "available" : "NOT available")}");
                }
            }

            // --- Work the repair queue (labor-limited; budget from what PreTick actually
            //     requested, so this-tick failures wait one step for labor) ---
            double hoursAvailable = laborGrant * _requestedCorrectiveHours;
            foreach (var job in _queue.ToList())
            {
                if (hoursAvailable <= 0) break;

                if (!job.SpareConsumed)
                {
                    if (SpareCount(job.Group.EquipmentClass) > 0)
                    {
                        _sparesByClass[job.Group.EquipmentClass]--;
                        _sparesMass.Withdraw(job.Group.SpareMassKg);
                        job.SpareConsumed = true;
                    }
                    else if (_cannibalize.Value > 0.5 && job.Group.Total >= 2 &&
                             _queue.Count(q => q.Group == job.Group && !q.SpareConsumed) >= 2)
                    {
                        // Two failed units of one group -> strip one for parts, repair the other
                        // (never sacrifice a job that already has a spare installed).
                        var other = _queue.First(q => q.Group == job.Group && q != job && !q.SpareConsumed);
                        _queue.Remove(other);
                        job.Group.Total--;
                        job.SpareConsumed = true;
                        Log(ctx, EventSeverity.Info, $"Cannibalized a failed {job.Group.Id} unit for parts");
                    }
                    else continue; // stuck waiting for a spare
                }

                double work = Math.Min(job.HoursRemaining, hoursAvailable);
                job.HoursRemaining -= work;
                hoursAvailable -= work;
                if (job.HoursRemaining <= 1e-6)
                {
                    _queue.Remove(job);
                    job.Group.Working = Math.Min(job.Group.Total, job.Group.Working + 1);
                    // Renewal: a fresh unit dilutes the population age (Weibull hazard resets
                    // proportionally instead of growing without bound).
                    if (job.Group.Total > 0)
                        job.Group.AgeHours *= 1.0 - 1.0 / job.Group.Total;
                }
            }

            // --- Propagate availability to owners ---
            // Modules with per-function groups (FunctionTag set) query FunctionCapacity()
            // themselves; their module-level CapacityFactor is the capacity-weighted mean so a
            // single dead subunit degrades rather than kills the whole module. Untagged
            // modules keep the conservative min.
            foreach (var ownerGroup in _groups.GroupBy(g => g.Owner))
            {
                bool tagged = ownerGroup.Any(g => g.FunctionTag != null);
                double cap;
                if (tagged)
                {
                    double sum = 0; int n = 0;
                    foreach (var g in ownerGroup) { sum += g.CapacityContribution; n++; }
                    cap = n > 0 ? sum / n : 1.0;
                }
                else
                {
                    cap = 1.0;
                    foreach (var g in ownerGroup) cap = Math.Min(cap, g.CapacityContribution);
                }
                ownerGroup.Key.CapacityFactor = cap;
                double worst = 1.0;
                foreach (var g in ownerGroup) worst = Math.Min(worst, g.CapacityContribution);
                ownerGroup.Key.Health = worst >= 0.999 ? ModuleHealth.Nominal
                    : worst > 0.4 ? ModuleHealth.Degraded : ModuleHealth.Failed;
            }

            Record(ctx, "maint.queue", "Open repair jobs", "", _queue.Count);
            Record(ctx, "maint.spares_mass", "Spares inventory", "kg", _sparesMass.AmountKg);
            Record(ctx, "maint.awaiting_spares", "Jobs stuck without spares", "",
                _queue.Count(j => !j.SpareConsumed));
        }

        /// <summary>Knuth Poisson sampler — fine for the small per-step lambdas seen here.</summary>
        private int SamplePoisson(double lambda)
        {
            if (lambda <= 0) return 0;
            if (lambda < 1e-4) return Rng.Chance(lambda) ? 1 : 0;
            double l = Math.Exp(-lambda);
            int k = 0;
            double p = 1;
            do { k++; p *= Rng.NextDouble(); } while (p > l && k < 50);
            return k - 1;
        }

        /// <summary>Gamma(1 + 1/k) via Stirling-adequate approximation (scale-from-mean for Weibull).</summary>
        private static double GammaMeanFactor(double k)
        {
            double x = 1.0 + 1.0 / k;
            // Lanczos-lite: good to <0.5% for x in [1,2], fine for hazard scaling.
            double[] g = { 1.000000000190015, 76.18009172947146, -86.50532032941677, 24.01409824083091,
                           -1.231739572450155, 1.208650973866179e-3, -5.395239384953e-6 };
            double sum = g[0];
            for (int i = 1; i < 7; i++) sum += g[i] / (x + i - 1);
            double t = x + 4.5;
            return Math.Exp((x - 0.5) * Math.Log(t) - t + Math.Log(sum * 2.5066282746310005));
        }

        public override string StatusLine =>
            $"{_queue.Count} open jobs ({_queue.Count(j => !j.SpareConsumed)} awaiting spares)";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Component groups", _groups.Count, "");
                yield return ("Open repairs", _queue.Count, "");
                yield return ("Spares mass", _sparesMass.AmountKg / 1000.0, "t");
            }
        }
    }
}
