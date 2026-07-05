using System;
using System.Collections.Generic;

namespace MarsSim.Core
{
    /// <summary>Bulk commodities tracked by mass. Cabin atmosphere is NOT a store (see Habitat).</summary>
    public enum Resource
    {
        O2,             // gaseous/liquid O2 for life support reserve, kg
        N2,             // pressurant / atmosphere makeup, kg
        CO2,            // scrubbed/captured CO2 buffer for Sabatier, kg
        H2,             // hydrogen feedstock (Earth-imported or electrolysis), kg
        CH4,            // liquid methane propellant, kg
        LOX,            // liquid oxygen propellant (kept separate from breathing O2), kg
        WaterPotable,   // clean water, kg
        WaterWaste,     // grey/waste water awaiting recovery, kg
        WaterFeedstock, // mined/ISRU water not yet purified, kg
        Food,           // packaged + grown food, kg (kcal tracked alongside)
        Biomass,        // inedible plant matter, kg
        Regolith,       // excavated regolith, kg
        SparesMass,     // aggregate spares inventory, kg (units tracked by MaintenanceSystem)
    }

    public static class ResourceInfo
    {
        public static string DisplayName(Resource r) => r switch
        {
            Resource.O2 => "Oxygen reserve",
            Resource.N2 => "Nitrogen",
            Resource.CO2 => "CO₂ buffer",
            Resource.H2 => "Hydrogen",
            Resource.CH4 => "Methane (propellant)",
            Resource.LOX => "LOX (propellant)",
            Resource.WaterPotable => "Potable water",
            Resource.WaterWaste => "Wastewater",
            Resource.WaterFeedstock => "Raw water (ISRU)",
            Resource.Food => "Food",
            Resource.Biomass => "Biomass",
            Resource.Regolith => "Regolith",
            Resource.SparesMass => "Spares",
            _ => r.ToString()
        };
    }

    /// <summary>
    /// A mass buffer with capacity. All withdrawals/deposits are clamped and logged so the
    /// engine can audit conservation: sum(deposits) - sum(withdrawals) == delta(amount).
    /// </summary>
    public sealed class Store
    {
        public string Id { get; }
        public Resource Resource { get; }
        public string DisplayName { get; set; }

        public double CapacityKg { get; set; }
        public double AmountKg { get; private set; }

        /// <summary>Cumulative totals for conservation audits.</summary>
        public double TotalDepositedKg { get; private set; }
        public double TotalWithdrawnKg { get; private set; }
        public double TotalVentedKg { get; private set; }   // deposits rejected for lack of capacity

        public Store(string id, Resource resource, double capacityKg, double initialKg = 0, string displayName = null)
        {
            Id = id;
            Resource = resource;
            CapacityKg = capacityKg;
            AmountKg = Math.Min(initialKg, capacityKg);
            TotalDepositedKg = AmountKg;
            DisplayName = displayName ?? ResourceInfo.DisplayName(resource);
        }

        public double FreeKg => Math.Max(0, CapacityKg - AmountKg);
        public double Fraction => CapacityKg > 0 ? AmountKg / CapacityKg : 0;

        /// <summary>Deposit up to <paramref name="kg"/>; returns the amount actually stored. Excess is vented/discarded (tracked).</summary>
        public double Deposit(double kg)
        {
            if (kg <= 0) return 0;
            double stored = Math.Min(kg, FreeKg);
            AmountKg += stored;
            TotalDepositedKg += stored;
            TotalVentedKg += kg - stored;
            return stored;
        }

        /// <summary>Withdraw up to <paramref name="kg"/>; returns the amount actually obtained.</summary>
        public double Withdraw(double kg)
        {
            if (kg <= 0) return 0;
            double got = Math.Min(kg, AmountKg);
            AmountKg -= got;
            TotalWithdrawnKg += got;
            return got;
        }

        /// <summary>Capacity increase when new tankage/pantry lands.</summary>
        public void AddCapacity(double kg) => CapacityKg += kg;
    }

    /// <summary>Registry of stores keyed by id, with per-resource aggregate queries.</summary>
    public sealed class StoreSet
    {
        private readonly Dictionary<string, Store> _stores = new();
        private readonly List<Store> _ordered = new();

        public IReadOnlyList<Store> All => _ordered;

        public Store Create(string id, Resource r, double capacityKg, double initialKg = 0, string displayName = null)
        {
            if (_stores.ContainsKey(id)) throw new InvalidOperationException($"duplicate store id '{id}'");
            var s = new Store(id, r, capacityKg, initialKg, displayName);
            _stores[id] = s;
            _ordered.Add(s);
            return s;
        }

        public Store Get(string id) => _stores.TryGetValue(id, out var s) ? s : null;

        public Store GetOrCreate(string id, Resource r, double capacityKg, double initialKg = 0)
            => Get(id) ?? Create(id, r, capacityKg, initialKg);

        public double TotalAmount(Resource r)
        {
            double sum = 0;
            foreach (var s in _ordered) if (s.Resource == r) sum += s.AmountKg;
            return sum;
        }

        public double TotalCapacity(Resource r)
        {
            double sum = 0;
            foreach (var s in _ordered) if (s.Resource == r) sum += s.CapacityKg;
            return sum;
        }
    }
}
