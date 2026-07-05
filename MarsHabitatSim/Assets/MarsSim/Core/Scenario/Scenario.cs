using System;
using System.Collections.Generic;
using MarsSim.Core.Json;

namespace MarsSim.Core.Scenario
{
    /// <summary>Spares lot in a cargo manifest.</summary>
    public sealed class SparesLot
    {
        public string EquipmentClass;
        public int Units;
        public double UnitMassKg;
    }

    /// <summary>What one wave of ships delivers. Every field is optional (zero default).</summary>
    public sealed class CargoManifest
    {
        public double SolarAreaM2;
        public double BatteryKwh;
        public int NuclearUnits;
        public double IsruCapacityKgPerSol;
        public double IceCapacityKgPerSol;
        public double GreenhouseM2;
        public double HabVolumeM3;
        public int EclssCrewCapacity;
        public double FoodKg;
        public double WaterKg;
        public double O2Kg;
        public double N2Kg;
        public double H2Kg;
        public int Robots;
        public List<SparesLot> Spares = new();

        /// <summary>Rough landed mass of this manifest, tonnes (for feasibility checks vs ship count).</summary>
        public double EstimateMassTonnes(Params.ParameterRegistry p)
        {
            double kg = 0;
            kg += SolarAreaM2 * SafeV(p, "power_solar.specific_mass_kg_m2", 2.5);
            kg += BatteryKwh * 1000.0 / SafeV(p, "power_solar.battery_wh_per_kg", 180);
            kg += NuclearUnits * SafeV(p, "power_nuclear.unit_mass_kg", 6000);
            kg += IsruCapacityKgPerSol * SafeV(p, "isru_atmosphere.plant_mass_kg_per_kg_sol", 8);
            kg += IceCapacityKgPerSol * SafeV(p, "isru_water.rig_mass_kg_per_kg_sol", 15);
            kg += GreenhouseM2 * SafeV(p, "food.system_mass_kg_m2", 90);
            kg += HabVolumeM3 * 200;                      // outfitted hab module ~0.2 t/m3
            kg += EclssCrewCapacity * 1500;               // ECLSS ~1.5 t/crew (BVAD-class)
            kg += FoodKg + WaterKg + O2Kg + N2Kg + H2Kg * 8; // H2 x8 for tank+cryo penalty
            kg += Robots * SafeV(p, "robots.unit_mass_kg", 60);
            foreach (var s in Spares) kg += s.Units * s.UnitMassKg;
            return kg / 1000.0;
        }

        private static double SafeV(Params.ParameterRegistry p, string id, double fallback)
            => p.Has(id) ? p.V(id) : fallback;
    }

    public sealed class ShipArrival
    {
        public string Name;
        public string Role = "Cargo";           // Cargo | CrewTransport | TankerDepot
        public bool ContributesHabitatVolume;
    }

    public sealed class Flight
    {
        public double Sol;
        public string Label;
        public List<ShipArrival> Ships = new();
        public int CrewArriving;
        public CargoManifest Cargo = new();
    }

    /// <summary>A complete mission definition, loaded from StreamingAssets/scenarios/*.json.</summary>
    public sealed class Scenario
    {
        public string Name = "Unnamed";
        public string Description = "";
        public double LatitudeDeg = 40.0;
        public double ElevationKm = -3.0;
        public DateTime EpochUtc = new(2031, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        public double TimestepSeconds = Units.SolSeconds / 24.0;
        public ulong Seed = 42;
        public double DurationSols = 2000;
        public Dictionary<string, FidelityLevel> Fidelity = new();
        public Dictionary<string, double> Overrides = new();
        public List<Flight> Flights = new();
        public List<double> ReturnWindowSols = new();

        public static Scenario FromJson(string text)
        {
            var j = JsonValue.Parse(text);
            var s = new Scenario
            {
                Name = j["name"].AsString("Unnamed"),
                Description = j["description"].AsString(""),
                LatitudeDeg = j["site"]["latitude_deg"].AsDouble(40),
                ElevationKm = j["site"]["elevation_km"].AsDouble(-3),
                TimestepSeconds = j["timestep_seconds"].AsDouble(Units.SolSeconds / 24.0),
                Seed = (ulong)j["seed"].AsDouble(42),
                DurationSols = j["duration_sols"].AsDouble(2000),
            };
            if (!j["epoch_utc"].IsNull &&
                DateTime.TryParse(j["epoch_utc"].AsString(), null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var epoch))
                s.EpochUtc = epoch;

            foreach (var (key, v) in j["fidelity"])
                s.Fidelity[key] = v.AsString("L1") switch
                {
                    "L0" => FidelityLevel.L0_Distilled,
                    "L2" => FidelityLevel.L2_Physics,
                    _ => FidelityLevel.L1_Analytic,
                };

            foreach (var (key, v) in j["overrides"])
                s.Overrides[key] = v.AsDouble();

            foreach (var f in j["flights"].Items)
            {
                var flight = new Flight
                {
                    Sol = f["sol"].AsDouble(),
                    Label = f["label"].AsString($"Flight @ sol {f["sol"].AsDouble():F0}"),
                    CrewArriving = f["crew"].AsInt(),
                };
                foreach (var ship in f["ships"].Items)
                    flight.Ships.Add(new ShipArrival
                    {
                        Name = ship["name"].AsString("Ship"),
                        Role = ship["role"].AsString("Cargo"),
                        ContributesHabitatVolume = ship["habitat"].AsBool(),
                    });
                var c = f["cargo"];
                flight.Cargo = new CargoManifest
                {
                    SolarAreaM2 = c["solar_area_m2"].AsDouble(),
                    BatteryKwh = c["battery_kwh"].AsDouble(),
                    NuclearUnits = c["nuclear_units"].AsInt(),
                    IsruCapacityKgPerSol = c["isru_capacity_kg_sol"].AsDouble(),
                    IceCapacityKgPerSol = c["ice_capacity_kg_sol"].AsDouble(),
                    GreenhouseM2 = c["greenhouse_m2"].AsDouble(),
                    HabVolumeM3 = c["hab_volume_m3"].AsDouble(),
                    EclssCrewCapacity = c["eclss_crew_capacity"].AsInt(),
                    FoodKg = c["food_kg"].AsDouble(),
                    WaterKg = c["water_kg"].AsDouble(),
                    O2Kg = c["o2_kg"].AsDouble(),
                    N2Kg = c["n2_kg"].AsDouble(),
                    H2Kg = c["h2_kg"].AsDouble(),
                    Robots = c["robots"].AsInt(),
                };
                foreach (var sp in c["spares"].Items)
                    flight.Cargo.Spares.Add(new SparesLot
                    {
                        EquipmentClass = sp["class"].AsString("general"),
                        Units = sp["units"].AsInt(),
                        UnitMassKg = sp["unit_kg"].AsDouble(50),
                    });
                s.Flights.Add(flight);
            }

            foreach (var w in j["return_windows_sols"].Items)
                s.ReturnWindowSols.Add(w.AsDouble());

            s.Flights.Sort((a, b) => a.Sol.CompareTo(b.Sol));
            return s;
        }
    }
}
