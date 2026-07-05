using System;
using MarsSim.Core;
using MarsSim.Core.Json;
using MarsSim.Core.Params;
using NUnit.Framework;

namespace MarsSim.Tests
{
    public class ClockTests
    {
        [Test]
        public void LsMatchesAllisonMcEwenAnchors()
        {
            // At the J2000 epoch, Mars was in late northern autumn: Ls ≈ 274-278°
            // (Allison & McEwen 2000 give Ls ≈ 277.2° at 2000-01-06; our simplified series
            // lands within a couple of degrees).
            double ls = SimClock.ComputeLs(0);
            Assert.That(ls, Is.InRange(270.0, 282.0));
        }

        [Test]
        public void MarsYearIs668Sols()
        {
            var clock = new SimClock(new DateTime(2031, 9, 15, 0, 0, 0, DateTimeKind.Utc), Units.SolSeconds / 24.0);
            double ls0 = clock.Ls;
            int steps = 0;
            // Advance until Ls wraps back past the start.
            double prev = ls0;
            bool wrapped = false;
            while (steps < 24 * 800)
            {
                clock.Advance();
                steps++;
                if (prev > 300 && clock.Ls < 60) wrapped = true;
                if (wrapped && clock.Ls >= ls0 && clock.Ls < ls0 + 1.0) break;
                prev = clock.Ls;
            }
            double sols = steps / 24.0;
            Assert.That(sols, Is.InRange(660, 677), $"Mars year measured as {sols} sols");
        }

        [Test]
        public void SunGeometryIsSane()
        {
            // Equinox (Ls=0), equator, local noon: sun near zenith.
            double el = SimClock.SunElevationDeg(0, 0, 0.5);
            Assert.That(el, Is.InRange(85, 90.01));
            // Midnight: sun well below horizon.
            Assert.That(SimClock.SunElevationDeg(0, 0, 0.0), Is.LessThan(-50));
            // Northern winter solstice at 40N: low noon sun.
            double elWinter = SimClock.SunElevationDeg(270, 40, 0.5);
            Assert.That(elWinter, Is.InRange(15, 35));
        }

        [Test]
        public void MarsSunDistanceSpansPerihelionAphelion()
        {
            Assert.That(SimClock.MarsSunDistanceAu(251), Is.EqualTo(1.381).Within(0.01), "perihelion");
            Assert.That(SimClock.MarsSunDistanceAu(71), Is.EqualTo(1.666).Within(0.01), "aphelion");
        }
    }

    public class JsonTests
    {
        [Test]
        public void RoundTripsNestedStructures()
        {
            const string src = "{\"a\": [1, 2.5, -3e2], \"b\": {\"c\": \"hi\\nthere\", \"d\": null, \"e\": true}}";
            var v = JsonValue.Parse(src);
            Assert.That(v["a"][2].AsDouble(), Is.EqualTo(-300));
            Assert.That(v["b"]["c"].AsString(), Is.EqualTo("hi\nthere"));
            Assert.That(v["b"]["d"].IsNull, Is.True);
            var re = JsonValue.Parse(v.Serialize());
            Assert.That(re["b"]["e"].AsBool(), Is.True);
        }

        [Test]
        public void RejectsMalformedJson()
        {
            Assert.That(JsonValue.TryParse("{\"a\": }", out _, out _), Is.False);
            Assert.That(JsonValue.TryParse("[1, 2,, 3]", out _, out _), Is.False);
        }
    }

    public class StoreTests
    {
        [Test]
        public void LedgerAlwaysBalances()
        {
            var rng = new SimRandom(7);
            var store = new Store("test", Resource.WaterPotable, 1000, 100);
            for (int i = 0; i < 10000; i++)
            {
                if (rng.Chance(0.5)) store.Deposit(rng.Range(0, 60));
                else store.Withdraw(rng.Range(0, 60));
            }
            Assert.That(store.TotalDepositedKg - store.TotalWithdrawnKg, Is.EqualTo(store.AmountKg).Within(1e-6));
            Assert.That(store.AmountKg, Is.InRange(0, 1000));
        }
    }

    public class PowerBusTests
    {
        private sealed class DummyModule : SimModule
        {
            public override void Tick(SimContext ctx) { }
        }

        [Test]
        public void CriticalLoadsAreServedFirst()
        {
            var bus = new PowerBus();
            var critical = new DummyModule();
            var low = new DummyModule();

            bus.BeginStep();
            bus.Offer(100);
            bus.Request(critical, 80, LoadPriority.Critical);
            bus.Request(low, 80, LoadPriority.Low);
            bus.Resolve(1.0);

            Assert.That(bus.GrantedFraction(critical), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(bus.GrantedFraction(low), Is.EqualTo(0.25).Within(1e-9));
            Assert.That(bus.UnmetKw, Is.EqualTo(60).Within(1e-9));
        }

        [Test]
        public void BatteryAbsorbsSurplusAndCoversDeficit()
        {
            var bus = new PowerBus();
            bus.Battery.CapacityKwh = 100;
            bus.Battery.EnergyKwh = 50;
            bus.Battery.ChargeEfficiency = 1.0;
            bus.Battery.MinSocFraction = 0;
            var load = new DummyModule();

            // Surplus hour: 100 kW offered, 40 kW load -> 60 kWh charged.
            bus.BeginStep();
            bus.Offer(100);
            bus.Request(load, 40, LoadPriority.Normal);
            bus.Resolve(1.0);
            Assert.That(bus.Battery.EnergyKwh, Is.EqualTo(100).Within(1e-6)); // clamped at capacity
            Assert.That(bus.CurtailedKw, Is.GreaterThan(0));

            // Deficit hour: 0 offered, 30 kW load -> battery covers.
            bus.BeginStep();
            bus.Request(load, 30, LoadPriority.Normal);
            bus.Resolve(1.0);
            Assert.That(bus.GrantedFraction(load), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(bus.Battery.EnergyKwh, Is.EqualTo(70).Within(1e-6));
        }
    }

    public class LaborPoolTests
    {
        private sealed class DummyModule : SimModule
        {
            public override void Tick(SimContext ctx) { }
        }

        [Test]
        public void RobotsSubstituteAtEffectivenessRatio()
        {
            var pool = new LaborPool();
            var m = new DummyModule();
            pool.BeginStep();
            pool.SetRobotEffectiveness(TaskType.Logistics, 0.5);
            pool.SupplyRobotHours(10);        // worth 5 crew-eq hours
            pool.SupplyCrewHours(2);
            pool.Request(m, TaskType.Logistics, 6, LaborPriority.Normal);
            pool.Resolve();
            // 5 robot-eq + 1 crew hour needed -> fully served.
            Assert.That(pool.GrantedFraction(m, TaskType.Logistics), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(pool.RobotHoursUsed, Is.EqualTo(10).Within(1e-9));
            Assert.That(pool.CrewHoursUsed, Is.EqualTo(1.0).Within(1e-9));
        }
    }

    public class RandomTests
    {
        [Test]
        public void StreamsAreIndependentAndDeterministic()
        {
            var a1 = new SimRandom(42, "solar");
            var a2 = new SimRandom(42, "solar");
            var b = new SimRandom(42, "environment");
            for (int i = 0; i < 100; i++)
                Assert.That(a1.NextDouble(), Is.EqualTo(a2.NextDouble()));
            // Different stream diverges.
            var a3 = new SimRandom(42, "solar");
            bool differs = false;
            for (int i = 0; i < 20; i++)
                if (Math.Abs(a3.NextDouble() - b.NextDouble()) > 1e-12) { differs = true; break; }
            Assert.That(differs, Is.True);
        }
    }
}
