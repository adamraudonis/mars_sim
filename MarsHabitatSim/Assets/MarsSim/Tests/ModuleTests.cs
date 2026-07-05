using System;
using MarsSim.Core;
using MarsSim.Core.Modules;
using MarsSim.Core.Params;
using MarsSim.Core.Study;
using NUnit.Framework;

namespace MarsSim.Tests
{
    public class IsruChemistryTests
    {
        private static IsruPropellantPlant BuildPlant(out SimulationEngine engine)
        {
            var reg = new ParameterRegistry();
            engine = new SimulationEngine(reg, new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Units.SolSeconds / 24.0, 1);
            var plant = engine.Add(new IsruPropellantPlant(), "isru_plant");
            engine.Initialize();
            return plant;
        }

        [Test]
        public void WaterBasedChainMatchesStoichiometry()
        {
            var plant = BuildPlant(out _);
            var (kwh, water, h2) = plant.ChainCoefficients(h2Import: false);
            // Net reaction 2H2O + CO2 -> CH4 + 2O2 gives 2.25 kg fresh water per kg CH4
            // = 0.489 kg per kg of O:F 3.6 mix.
            Assert.That(water, Is.EqualTo(0.489).Within(0.01), "fresh water per kg propellant");
            Assert.That(h2, Is.EqualTo(0));
            // Full-chain specific energy should land in the published 6-10 kWh/kg band.
            Assert.That(kwh, Is.InRange(5.5, 10.5), $"chain energy {kwh} kWh/kg");
        }

        [Test]
        public void H2ImportChainNeedsNoWaterAndQuarterKgH2PerKgCh4()
        {
            var plant = BuildPlant(out _);
            var (kwh, water, h2) = plant.ChainCoefficients(h2Import: true);
            Assert.That(water, Is.EqualTo(0).Within(1e-9), "no mined water in H2-import mode");
            // 0.25 kg H2 per kg CH4 (half recycled from Sabatier water) = 0.0543 per kg mix.
            Assert.That(h2, Is.EqualTo(0.0543).Within(0.003));
            // SOXE O2 makes this mode more energy-intensive than the water-based chain.
            var (kwhWater, _, _) = plant.ChainCoefficients(h2Import: false);
            Assert.That(kwh, Is.GreaterThan(kwhWater));
        }

        [Test]
        public void RefuelOneStarshipAnchor()
        {
            // Anchor the headline number: ~1100 t propellant needs ~538 t of mined water and
            // ~2.1 GWh of plant energy on the water-based chain (Kleinhenz & Paz-class values).
            var plant = BuildPlant(out _);
            var (kwh, water, _) = plant.ChainCoefficients(h2Import: false);
            double tonnesProp = 1100;
            double waterT = tonnesProp * water;
            double gwh = tonnesProp * 1000 * kwh / 1e6;
            Assert.That(waterT, Is.InRange(450, 620), $"water for one refuel: {waterT} t");
            Assert.That(gwh, Is.InRange(5, 12), $"energy for one refuel: {gwh} GWh");
        }
    }

    public class EnvironmentTests
    {
        private static SimulationEngine BuildEnv(ulong seed, FidelityLevel fidelity)
        {
            var reg = new ParameterRegistry();
            var engine = new SimulationEngine(reg, new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Units.SolSeconds / 24.0, seed);
            var env = engine.Add(new MarsEnvironment { LatitudeDeg = 40 }, "environment");
            env.Fidelity = fidelity;
            engine.Initialize();
            return engine;
        }

        [Test]
        public void AnnualInsolationInPublishedBand()
        {
            var engine = BuildEnv(1, FidelityLevel.L1_Analytic);
            // Integrate GHI per sol over a full Mars year (seasons + tau climatology).
            int sols = (int)Units.SolsPerMarsYear;
            double total = 0, best = 0;
            for (int d = 0; d < sols; d++)
            {
                double day = 0;
                for (int i = 0; i < 24; i++)
                {
                    engine.Step();
                    day += engine.Context.Env.GlobalHorizontalWm2 / 1000.0 * Units.SolHours / 24.0;
                }
                total += day;
                best = Math.Max(best, day);
            }
            double annualMean = total / sols;
            // Appelbaum & Flood-class values at a 40N site with seasonal tau: annual mean
            // ~1.5-3.5 kWh/m2/sol, clear-summer peak >2.5.
            Assert.That(annualMean, Is.InRange(1.2, 4.0), $"annual mean {annualMean:F2} kWh/m2/sol");
            Assert.That(best, Is.GreaterThan(2.2), $"best-sol insolation {best:F2} kWh/m2/sol");
        }

        [Test]
        public void GlobalStormsEventuallyHappenAndRaiseTau()
        {
            var engine = BuildEnv(3, FidelityLevel.L2_Physics);
            double maxTau = 0;
            engine.RunToSol(668 * 6); // six Mars years — expect ~2 global storms at p=1/3 per year
            var tau = engine.History.Get("env.tau");
            for (int i = 0; i < tau.Count; i++) maxTau = Math.Max(maxTau, tau[i]);
            Assert.That(maxTau, Is.GreaterThan(3.0), $"max tau over 6 Mars years was {maxTau}");
        }

        [Test]
        public void DeterministicReplay()
        {
            var a = BuildEnv(7, FidelityLevel.L2_Physics);
            var b = BuildEnv(7, FidelityLevel.L2_Physics);
            a.RunToSol(700);
            b.RunToSol(700);
            var ta = a.History.Get("env.tau");
            var tb = b.History.Get("env.tau");
            Assert.That(ta.Count, Is.EqualTo(tb.Count));
            for (int i = 0; i < ta.Count; i += 13)
                Assert.That(ta[i], Is.EqualTo(tb[i]), $"tau diverged at step {i}");
        }
    }

    public class SolarTests
    {
        [Test]
        public void DistilledSolarYieldIsPlausible()
        {
            var reg = new ParameterRegistry();
            var result = Distiller.DistillSolar(reg, latitudeDeg: 40, seed: 5, sols: 700);
            double y = result.FittedParams["power_solar.l0_kwh_per_sol_per_kw"];
            // Rating basis is 500 W/m2 clear-noon GHI; research anchor: ~7.4 equivalent
            // peak hours at the equator (clear), so a 40N annual mean of ~4-6 is expected.
            Assert.That(y, Is.InRange(3.0, 7.0), result.Summary);
        }
    }

    public class MaintenanceTests
    {
        [Test]
        public void FailuresDegradeAndSparesRepair()
        {
            var reg = new ParameterRegistry();
            var engine = new SimulationEngine(reg, new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Units.SolSeconds / 24.0, 11);
            engine.Add(new MarsEnvironment(), "environment");
            var crew = engine.Add(new Crew(), "crew");
            var maint = engine.Add(new MaintenanceSystem(), "maintenance");
            var nuclear = engine.Add(new NuclearPlant { Units = 4 }, "nuclear");
            engine.Initialize();

            // Provisions so the crew survives the test window (stores created by Crew.Init
            // have zero capacity until tankage "lands" — grow them explicitly here).
            var food = engine.Stores.GetOrCreate("food", Resource.Food, 0);
            food.AddCapacity(1e6);
            food.Deposit(50000);
            var water = engine.Stores.GetOrCreate("water_potable", Resource.WaterPotable, 0);
            water.AddCapacity(1e6);
            water.Deposit(50000);
            crew.Arrive(engine.Context, 4);

            maint.Register(nuclear, "test_bop", "power_electronics", 4,
                mtbfHours: 500, repairCrewHours: 2, spareMassKg: 20);
            maint.AddSpares("power_electronics", 200, 20);   // deep pool: test the repair loop, not exhaustion

            engine.RunToSol(300);

            // With MTBF 500 h over 300 sols (7400 h), each of 4 units fails many times;
            // spares + labor keep availability high.
            var queue = engine.History.Get("maint.queue");
            double maxQ = 0;
            for (int i = 0; i < queue.Count; i++) maxQ = Math.Max(maxQ, queue[i]);
            Assert.That(maxQ, Is.GreaterThan(0), "no failures ever sampled — hazard model broken");
            Assert.That(nuclear.CapacityFactor, Is.GreaterThan(0.4), "repairs are not keeping up");
            Assert.That(engine.Stores.Get("spares").AmountKg, Is.LessThan(4000), "spares were never consumed");
        }
    }

    public class CrewSurvivalTests
    {
        [Test]
        public void CrewDiesWithoutWaterEventually()
        {
            var reg = new ParameterRegistry();
            var engine = new SimulationEngine(reg, new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Units.SolSeconds / 24.0, 13);
            engine.Add(new MarsEnvironment(), "environment");
            var hab = engine.Add(new Habitat(), "habitat");
            var crew = engine.Add(new Crew(), "crew");
            engine.Initialize();
            hab.AddVolume(engine.Context, 1000, arrivesPressurized: true);
            var food = engine.Stores.GetOrCreate("food", Resource.Food, 0);
            food.AddCapacity(1e6);
            food.Deposit(10000);
            var o2 = engine.Stores.GetOrCreate("o2_reserve", Resource.O2, 0);
            o2.AddCapacity(1e6);
            o2.Deposit(20000);
            // No water at all.
            crew.Arrive(engine.Context, 4);
            engine.RunToSol(60);
            Assert.That(crew.Fatalities, Is.GreaterThan(0), "dehydration must be lethal within 60 sols");
        }
    }
}
