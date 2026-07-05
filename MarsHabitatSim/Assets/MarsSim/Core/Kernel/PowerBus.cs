using System;
using System.Collections.Generic;

namespace MarsSim.Core
{
    /// <summary>Load-shed order: lower value = shed last.</summary>
    public enum LoadPriority
    {
        Critical = 0,      // life support, habitat thermal
        High = 1,          // comms, medical, robot charging for critical tasks
        Normal = 2,        // ISRU production, ice mining
        Low = 3,           // greenhouse supplemental lighting, science
        Opportunistic = 4, // propellant liquefaction topping, dumps surplus
    }

    /// <summary>
    /// Instantaneous electrical bus, resolved every step:
    /// generators offer kW, consumers demand kW at a priority, the battery bridges the gap.
    /// Demands are served strictly by priority; each consumer gets a granted fraction it must
    /// respect this step (modules scale their throughput accordingly).
    /// </summary>
    public sealed class PowerBus
    {
        private struct Demand { public SimModule Module; public double Kw; public LoadPriority Priority; }

        private readonly List<Demand> _demands = new();
        private readonly Dictionary<SimModule, double> _granted = new();
        private double _offeredKw;

        public BatteryState Battery { get; } = new BatteryState();

        // Step results (for charts / diagnostics)
        public double GenerationOfferedKw { get; private set; }
        public double GenerationUsedKw { get; private set; }
        public double DemandKw { get; private set; }
        public double ServedKw { get; private set; }
        public double UnmetKw { get; private set; }
        public double CurtailedKw { get; private set; }
        public double BatteryFlowKw { get; private set; } // + discharging, - charging

        public void BeginStep()
        {
            _demands.Clear();
            _granted.Clear();
            _offeredKw = 0;
        }

        public void Offer(double kw)
        {
            if (kw > 0) _offeredKw += kw;
        }

        public void Request(SimModule module, double kw, LoadPriority priority)
        {
            if (kw <= 0) return;
            _demands.Add(new Demand { Module = module, Kw = kw, Priority = priority });
        }

        /// <summary>Fraction of its request a module actually received this step [0,1].</summary>
        public double GrantedFraction(SimModule module)
            => _granted.TryGetValue(module, out double f) ? f : 1.0;

        public void Resolve(double dtHours)
        {
            GenerationOfferedKw = _offeredKw;

            double totalDemand = 0;
            foreach (var d in _demands) totalDemand += d.Kw;
            DemandKw = totalDemand;

            // Two battery budgets: Critical/High may drain to the hard floor; Normal and
            // below only down to the critical-reserve SoC. This is what keeps a night-time
            // ISRU plant from starving the life support at 3 AM.
            double battFullKw = Battery.MaxDischargeKw(dtHours);
            double battAboveReserveKwh = Math.Max(0,
                Battery.EnergyKwh - Battery.CriticalReserveSoc * Battery.CapacityKwh);
            double battLowPrioKw = dtHours > 0
                ? Math.Min(Battery.MaxDischargeRateKw, Math.Min(battFullKw, battAboveReserveKwh / dtHours))
                : 0;

            _demands.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            double servedTotal = 0;
            double genLeft = _offeredKw;
            double battUsedKw = 0;
            int i = 0;
            while (i < _demands.Count)
            {
                // Group same-priority demands: if underserved, they share pro-rata.
                int j = i;
                double groupKw = 0;
                while (j < _demands.Count && _demands[j].Priority == _demands[i].Priority)
                {
                    groupKw += _demands[j].Kw;
                    j++;
                }
                double battCapKw = _demands[i].Priority <= LoadPriority.High ? battFullKw : battLowPrioKw;
                double available = genLeft + Math.Max(0, battCapKw - battUsedKw);
                double fraction = groupKw <= available ? 1.0 : (groupKw > 0 ? available / groupKw : 0);
                double servedGroup = 0;
                for (int k = i; k < j; k++)
                {
                    double f = Math.Clamp(fraction, 0, 1);
                    // A module may issue several requests; keep the most constrained fraction.
                    if (_granted.TryGetValue(_demands[k].Module, out double prev))
                        _granted[_demands[k].Module] = Math.Min(prev, f);
                    else
                        _granted[_demands[k].Module] = f;
                    servedGroup += _demands[k].Kw * f;
                }
                double fromGen = Math.Min(servedGroup, genLeft);
                genLeft -= fromGen;
                battUsedKw += servedGroup - fromGen;
                servedTotal += servedGroup;
                i = j;
            }

            ServedKw = servedTotal;
            UnmetKw = Math.Max(0, totalDemand - servedTotal);

            // Battery bookkeeping (gen is always consumed before battery within each group,
            // so genLeft > 0 implies battUsedKw == 0).
            if (battUsedKw > 1e-12)
            {
                double discharged = Battery.Discharge(battUsedKw, dtHours);
                BatteryFlowKw = discharged;
                GenerationUsedKw = _offeredKw - genLeft;
                CurtailedKw = 0;
            }
            else
            {
                double absorbed = Battery.Charge(genLeft, dtHours);
                BatteryFlowKw = -absorbed;
                GenerationUsedKw = (_offeredKw - genLeft) + absorbed;
                CurtailedKw = Math.Max(0, genLeft - absorbed);
            }
        }
    }

    /// <summary>
    /// Aggregated battery bank. Round-trip efficiency is applied on charge; depth-of-discharge
    /// floor protects cycle life (both sourced from params via the BatteryBank module).
    /// </summary>
    public sealed class BatteryState
    {
        public double CapacityKwh { get; set; }
        public double EnergyKwh { get; set; }
        public double MaxChargeRateKw { get; set; } = double.PositiveInfinity;
        public double MaxDischargeRateKw { get; set; } = double.PositiveInfinity;
        public double ChargeEfficiency { get; set; } = 0.95;   // round-trip applied on the way in
        public double MinSocFraction { get; set; } = 0.15;

        /// <summary>SoC below which only Critical/High loads may discharge (night reserve for life support).</summary>
        public double CriticalReserveSoc { get; set; } = 0.5;

        public double Soc => CapacityKwh > 0 ? EnergyKwh / CapacityKwh : 0;
        public double UsableKwh => Math.Max(0, EnergyKwh - MinSocFraction * CapacityKwh);

        public double MaxDischargeKw(double dtHours)
            => dtHours <= 0 ? 0 : Math.Min(MaxDischargeRateKw, UsableKwh / dtHours);

        /// <summary>Try to discharge at kw for dtHours; returns actual kW delivered.</summary>
        public double Discharge(double kw, double dtHours)
        {
            double actualKw = Math.Min(kw, MaxDischargeKw(dtHours));
            EnergyKwh -= actualKw * dtHours;
            return actualKw;
        }

        /// <summary>Try to absorb kw for dtHours; returns actual kW absorbed (grid side).</summary>
        public double Charge(double kw, double dtHours)
        {
            if (dtHours <= 0 || CapacityKwh <= 0) return 0;
            double headroomKwh = Math.Max(0, CapacityKwh - EnergyKwh);
            double maxGridKw = Math.Min(MaxChargeRateKw, headroomKwh / (dtHours * ChargeEfficiency));
            double actualKw = Math.Min(kw, maxGridKw);
            EnergyKwh += actualKw * dtHours * ChargeEfficiency;
            return actualKw;
        }
    }
}
