using System;
using System.Collections.Generic;

namespace MarsSim.Core
{
    public enum TaskType
    {
        Maintenance,    // preventive + corrective repair work
        IsruOps,        // running/maintaining propellant + water plants
        Agriculture,    // greenhouse tending, harvest
        Construction,   // assembly of landed modules, berm building, cabling
        Logistics,      // unloading ships, hauling, inventory
        Science,        // exploration, research (the point of being there)
        Crew,           // self-care overhead already excluded from supply; medical, ops
    }

    public enum LaborPriority { Critical = 0, High = 1, Normal = 2, Low = 3 }

    /// <summary>
    /// Per-step labor market. Crew and robots supply work-hours; modules demand hours per task
    /// type. Robots substitute for humans at a per-task effectiveness ratio (e.g. 0.5 means a
    /// robot-hour accomplishes half a crew-hour of that task). Allocation is by priority, then
    /// pro-rata within a priority band, preferring robots for robot-friendly tasks so scarce
    /// crew time is preserved.
    /// </summary>
    public sealed class LaborPool
    {
        private struct Demand
        {
            public SimModule Module;
            public TaskType Task;
            public double CrewEquivalentHours;
            public LaborPriority Priority;
            public int Seq;   // insertion order: stable, deterministic sort key
        }

        private readonly List<Demand> _demands = new();
        private readonly Dictionary<(SimModule, TaskType), double> _granted = new();

        public double CrewHoursAvailable { get; private set; }
        public double RobotHoursAvailable { get; private set; }
        private readonly Dictionary<TaskType, double> _robotEffectiveness = new();

        // Step diagnostics
        public double CrewHoursUsed { get; private set; }
        public double RobotHoursUsed { get; private set; }
        public double DemandCrewEqHours { get; private set; }
        public double ServedCrewEqHours { get; private set; }
        public double UnmetCrewEqHours { get; private set; }

        public void BeginStep()
        {
            _demands.Clear();
            _granted.Clear();
            CrewHoursAvailable = 0;
            RobotHoursAvailable = 0;
            CrewHoursUsed = 0;
            RobotHoursUsed = 0;
        }

        public void SupplyCrewHours(double hours) { if (hours > 0) CrewHoursAvailable += hours; }
        public void SupplyRobotHours(double hours) { if (hours > 0) RobotHoursAvailable += hours; }

        public void SetRobotEffectiveness(TaskType task, double ratio) => _robotEffectiveness[task] = ratio;

        public double RobotEffectiveness(TaskType task)
            => _robotEffectiveness.TryGetValue(task, out double r) ? r : 0.0;

        /// <summary>Request labor in crew-equivalent hours for this step.</summary>
        public void Request(SimModule module, TaskType task, double crewEquivalentHours, LaborPriority priority)
        {
            if (crewEquivalentHours <= 0) return;
            _demands.Add(new Demand
            {
                Module = module, Task = task, CrewEquivalentHours = crewEquivalentHours,
                Priority = priority, Seq = _demands.Count,
            });
        }

        /// <summary>Fraction of the request fulfilled this step [0,1].</summary>
        public double GrantedFraction(SimModule module, TaskType task)
            => _granted.TryGetValue((module, task), out double f) ? f : 1.0;

        public void Resolve()
        {
            DemandCrewEqHours = 0;
            foreach (var d in _demands) DemandCrewEqHours += d.CrewEquivalentHours;

            double crewLeft = CrewHoursAvailable;
            double robotLeft = RobotHoursAvailable;
            double served = 0;

            // Stable, deterministic: priority then insertion order.
            _demands.Sort((a, b) =>
            {
                int c = a.Priority.CompareTo(b.Priority);
                return c != 0 ? c : a.Seq.CompareTo(b.Seq);
            });

            int i = 0;
            while (i < _demands.Count)
            {
                int j = i;
                while (j < _demands.Count && _demands[j].Priority == _demands[i].Priority) j++;

                // Pro-rata within the band: every demand receives the same fraction f of its
                // request, with f sized (bisection) so robot-substituted supply just covers it.
                double f = BandFraction(i, j, crewLeft, robotLeft);
                for (int k = i; k < j; k++)
                {
                    var d = _demands[k];
                    double target = d.CrewEquivalentHours * f;
                    double got = 0;
                    double eff = RobotEffectiveness(d.Task);
                    if (eff > 0 && robotLeft > 0)
                    {
                        double robotHours = Math.Min(target / eff, robotLeft);
                        robotLeft -= robotHours;
                        RobotHoursUsed += robotHours;
                        got += robotHours * eff;
                    }
                    if (got < target - 1e-12 && crewLeft > 0)
                    {
                        double crewHours = Math.Min(target - got, crewLeft);
                        crewLeft -= crewHours;
                        CrewHoursUsed += crewHours;
                        got += crewHours;
                    }

                    double fr = d.CrewEquivalentHours > 0 ? Math.Clamp(got / d.CrewEquivalentHours, 0, 1) : 1;
                    var key = (d.Module, d.Task);
                    if (_granted.TryGetValue(key, out double prev)) _granted[key] = Math.Min(prev, fr);
                    else _granted[key] = fr;
                    served += got;
                }
                i = j;
            }

            ServedCrewEqHours = served;
            UnmetCrewEqHours = Math.Max(0, DemandCrewEqHours - served);
        }

        /// <summary>
        /// Largest uniform fraction of every band demand that the remaining crew+robot supply
        /// can serve (robots substitute per-task; feasibility is monotonic in f, so bisect).
        /// </summary>
        private double BandFraction(int i, int j, double crewLeft, double robotLeft)
        {
            bool Feasible(double f)
            {
                double crew = crewLeft, robot = robotLeft;
                for (int k = i; k < j; k++)
                {
                    var d = _demands[k];
                    double target = d.CrewEquivalentHours * f;
                    double eff = RobotEffectiveness(d.Task);
                    if (eff > 0 && robot > 0)
                    {
                        double rh = Math.Min(target / eff, robot);
                        robot -= rh;
                        target -= rh * eff;
                    }
                    if (target > 1e-12)
                    {
                        double ch = Math.Min(target, crew);
                        crew -= ch;
                        target -= ch;
                    }
                    if (target > 1e-9) return false;
                }
                return true;
            }

            if (Feasible(1)) return 1;
            double lo = 0, hi = 1;
            for (int iter = 0; iter < 24; iter++)
            {
                double mid = (lo + hi) / 2;
                if (Feasible(mid)) lo = mid; else hi = mid;
            }
            return lo;
        }
    }
}
