using System;
using System.IO;
using MarsSim.Core;
using MarsSim.Core.Modules;
using MarsSim.Core.Params;
using MarsSim.Core.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MarsSim.Tests
{
    /// <summary>
    /// End-to-end smoke: load the real shipped scenario + (if present) the real research
    /// parameter database, run deep into the crewed era, and assert the settlement holds
    /// together — no NaNs, ledgers balanced, crew alive, propellant accumulating.
    /// </summary>
    public class ScenarioSmokeTests
    {
        private static ParameterRegistry LoadParams()
        {
            var reg = new ParameterRegistry();
            string db = Path.Combine(Application.streamingAssetsPath, "parameters_master.json");
            if (File.Exists(db)) reg.LoadDatabaseJson(File.ReadAllText(db), out _);
            return reg;
        }

        private static SimulationEngine RunBaseline(double sols, ulong seed = 42)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "scenarios", "baseline.json");
            var scenario = Scenario.FromJson(File.ReadAllText(path));
            scenario.Seed = seed;
            var engine = SimulationBuilder.Build(scenario, LoadParams());
            engine.RunToSol(sols);
            return engine;
        }

        [Test]
        public void BaselineSurvivesIntoCrewedEra()
        {
            var engine = RunBaseline(1000);

            // Ledger balance is exact by construction; any drift means a module bypassed stores.
            Assert.That(engine.ConservationError(), Is.LessThan(1e-3), "mass ledger drift");

            var crew = engine.Find<Crew>();
            Assert.That(crew.Count, Is.EqualTo(12), "crew should be alive at sol 1000");
            Assert.That(crew.Fatalities, Is.EqualTo(0), "no fatalities expected in baseline");
            Assert.That(crew.HealthIndex, Is.GreaterThan(0.6), $"crew health {crew.HealthIndex}");

            // Life support: ppO2 held near setpoint while crew aboard.
            var ppo2 = engine.History.Get("hab.ppo2");
            for (int i = (int)(800 * 24); i < ppo2.Count; i += 24)
                Assert.That(ppo2[i], Is.InRange(15f, 30f), $"ppO2 out of range at step {i}");

            // Propellant is accumulating toward the return flight.
            var fleet = engine.Find<StarshipFleet>();
            Assert.That(fleet.ReturnPropellantFraction, Is.GreaterThan(0.3),
                $"return propellant only {fleet.ReturnPropellantFraction:P0} by sol 1000");

            // No series may contain NaN/Inf.
            foreach (var s in engine.History.All)
                for (int i = 0; i < s.Count; i += 7)
                    Assert.That(float.IsFinite(s[i]), Is.True, $"{s.Id} has non-finite value at {i}");
        }

        [Test]
        public void DeterministicAcrossRebuilds()
        {
            var a = RunBaseline(400, seed: 7);
            var b = RunBaseline(400, seed: 7);
            var sa = a.History.Get("power.offered");
            var sb = b.History.Get("power.offered");
            Assert.That(sa.Count, Is.EqualTo(sb.Count));
            for (int i = 0; i < sa.Count; i += 17)
                Assert.That(sa[i], Is.EqualTo(sb[i]), $"power series diverged at step {i}");
        }

        [Test]
        public void H2ImportScenarioProducesPropellantWithoutIceMine()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "scenarios", "h2_import.json");
            var scenario = Scenario.FromJson(File.ReadAllText(path));
            var engine = SimulationBuilder.Build(scenario, LoadParams());
            engine.RunToSol(500);

            Assert.That(engine.Find<IceMine>().CapacityKgPerSol, Is.EqualTo(0), "no ice mine in this architecture");
            double propT = (engine.Stores.Get("depot_ch4").AmountKg + engine.Stores.Get("depot_lox").AmountKg) / 1000.0;
            Assert.That(propT, Is.GreaterThan(50), $"only {propT:F0} t propellant by sol 500");
            // H2 is actually being drawn down.
            Assert.That(engine.Stores.Get("h2_import").AmountKg, Is.LessThan(35000));
        }

        [Test]
        public void ManifestMassFitsShipCount()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "scenarios", "baseline.json");
            var scenario = Scenario.FromJson(File.ReadAllText(path));
            var reg = LoadParams();
            // Register code defaults by building once.
            SimulationBuilder.Build(scenario, reg);
            foreach (var flight in scenario.Flights)
            {
                double t = flight.Cargo.EstimateMassTonnes(reg);
                double capacity = Math.Max(1, flight.Ships.Count) * 100.0; // ~100 t/ship (SpaceX claim)
                Assert.That(t, Is.LessThanOrEqualTo(capacity * 1.05),
                    $"{flight.Label}: manifest {t:F0} t exceeds {capacity:F0} t lift");
            }
        }
    }
}
