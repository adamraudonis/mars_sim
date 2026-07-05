/**
 * Deterministic sfc32 RNG. Every module gets its own stream (seeded from master seed +
 * stream name) so adding/removing one module never perturbs another's draws — required
 * for Monte Carlo stability and replay determinism.
 *
 * Note: the Unity reference used xorshift128+; sfc32 is used here because JS lacks cheap
 * 64-bit integer ops. Distributions are identical in character; runs are deterministic
 * within this implementation but not bit-identical to the C# one.
 */
export class SimRandom {
  private a: number;
  private b: number;
  private c: number;
  private d: number;

  constructor(seed: number | bigint, streamName = '') {
    let h = 0x9e3779b9 ^ Number(BigInt(seed) & 0xffffffffn);
    for (let i = 0; i < streamName.length; i++) {
      h ^= streamName.charCodeAt(i);
      h = Math.imul(h, 0x01000193);
    }
    // SplitMix32 to expand into four non-zero state words.
    const next = () => {
      h |= 0;
      h = (h + 0x9e3779b9) | 0;
      let z = h;
      z = Math.imul(z ^ (z >>> 16), 0x21f0aaad);
      z = Math.imul(z ^ (z >>> 15), 0x735a2d97);
      return (z ^ (z >>> 15)) >>> 0;
    };
    this.a = next();
    this.b = next();
    this.c = next();
    this.d = next();
    for (let i = 0; i < 8; i++) this.nextUint(); // warm up
  }

  nextUint(): number {
    const t = (((this.a + this.b) | 0) + this.d) | 0;
    this.d = (this.d + 1) | 0;
    this.a = this.b ^ (this.b >>> 9);
    this.b = (this.c + (this.c << 3)) | 0;
    this.c = (this.c << 21) | (this.c >>> 11);
    this.c = (this.c + t) | 0;
    return t >>> 0;
  }

  /** Uniform in [0, 1). */
  next(): number {
    return this.nextUint() / 4294967296;
  }

  range(min: number, max: number): number {
    return min + (max - min) * this.next();
  }

  nextInt(maxExclusive: number): number {
    return this.nextUint() % maxExclusive;
  }

  /** Standard normal via Box–Muller. */
  gaussian(): number {
    const u1 = 1 - this.next();
    const u2 = this.next();
    return Math.sqrt(-2 * Math.log(u1)) * Math.sin(2 * Math.PI * u2);
  }

  exponential(mean: number): number {
    return -mean * Math.log(1 - this.next());
  }

  weibull(shape: number, scale: number): number {
    return scale * Math.pow(-Math.log(1 - this.next()), 1 / shape);
  }

  chance(p: number): boolean {
    return this.next() < p;
  }

  /** Knuth Poisson sampler — fine for the small per-step lambdas seen here. */
  poisson(lambda: number): number {
    if (lambda <= 0) return 0;
    if (lambda < 1e-4) return this.chance(lambda) ? 1 : 0;
    const l = Math.exp(-lambda);
    let k = 0;
    let p = 1;
    do {
      k++;
      p *= this.next();
    } while (p > l && k < 50);
    return k - 1;
  }
}
