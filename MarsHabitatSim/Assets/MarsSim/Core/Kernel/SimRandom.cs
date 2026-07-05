using System;

namespace MarsSim.Core
{
    /// <summary>
    /// Deterministic xorshift128+ RNG. Every module gets its own stream so that adding or
    /// removing one module never perturbs the draws seen by another (Monte Carlo stability).
    /// </summary>
    public sealed class SimRandom
    {
        private ulong _s0, _s1;

        public SimRandom(ulong seed)
        {
            // SplitMix64 to expand the seed into two non-zero state words.
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            _s0 = Mix(ref z);
            _s1 = Mix(ref z);
            if (_s0 == 0 && _s1 == 0) _s1 = 1;
        }

        public SimRandom(ulong masterSeed, string streamName)
            : this(masterSeed ^ Fnv1a(streamName)) { }

        private static ulong Mix(ref ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            ulong x = z;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }

        private static ulong Fnv1a(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        public ulong NextULong()
        {
            ulong x = _s0, y = _s1;
            _s0 = y;
            x ^= x << 23;
            _s1 = x ^ y ^ (x >> 17) ^ (y >> 26);
            return _s1 + y;
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        public double Range(double min, double max) => min + (max - min) * NextDouble();

        public int NextInt(int maxExclusive) => (int)(NextULong() % (ulong)maxExclusive);

        /// <summary>Standard normal via Box–Muller.</summary>
        public double NextGaussian()
        {
            double u1 = 1.0 - NextDouble();
            double u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        /// <summary>Exponential inter-arrival sample with the given mean.</summary>
        public double NextExponential(double mean) => -mean * Math.Log(1.0 - NextDouble());

        /// <summary>Weibull sample (shape k, scale lambda).</summary>
        public double NextWeibull(double shape, double scale)
            => scale * Math.Pow(-Math.Log(1.0 - NextDouble()), 1.0 / shape);

        /// <summary>Bernoulli event with probability p for this step.</summary>
        public bool Chance(double p) => NextDouble() < p;
    }
}
