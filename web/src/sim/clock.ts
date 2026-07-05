import { Units } from './units';

const D2R = Units.deg2rad;
const R2D = Units.rad2deg;

function wrap360(x: number): number {
  x %= 360;
  return x < 0 ? x + 360 : x;
}

/**
 * Mission clock: mission-elapsed sols, time within the sol, areocentric solar longitude Ls
 * (Allison & McEwen 2000, accurate to ~0.01° at these timescales), and Earth UTC mapping.
 */
export class SimClock {
  readonly epochUtcMs: number;
  dtSeconds: number;

  sol = 0;
  ls: number;
  marsYear = 0;
  stepCount = 0;

  private readonly epochJ2000: number;
  private prevLs: number;

  constructor(epochUtcMs: number, dtSeconds: number) {
    this.epochUtcMs = epochUtcMs;
    this.dtSeconds = dtSeconds;
    this.epochJ2000 = (epochUtcMs - Date.UTC(2000, 0, 1, 12, 0, 0)) / 86400000;
    this.ls = SimClock.computeLs(this.epochJ2000);
    this.prevLs = this.ls;
  }

  get dtHours(): number {
    return this.dtSeconds / 3600;
  }

  get dtSols(): number {
    return this.dtSeconds / Units.solSeconds;
  }

  get solNumber(): number {
    return Math.floor(this.sol);
  }

  /** Fraction of the current sol [0,1): 0 = local midnight. */
  get timeOfSol(): number {
    return this.sol - Math.floor(this.sol);
  }

  /** Local true solar time in Mars-hours [0, 24.66). */
  get localSolarHours(): number {
    return this.timeOfSol * Units.solHours;
  }

  /** Earth UTC ms corresponding to current sim time. */
  get earthUtcMs(): number {
    return this.epochUtcMs + this.sol * Units.solSeconds * 1000;
  }

  advance(): void {
    this.sol += this.dtSols;
    this.stepCount++;
    const j2000 = this.epochJ2000 + this.sol * (Units.solSeconds / Units.earthDaySeconds);
    this.ls = SimClock.computeLs(j2000);
    if (this.ls < this.prevLs - 180) this.marsYear++; // wrapped 360 -> 0
    this.prevLs = this.ls;
  }

  /** Allison & McEwen (2000): areocentric solar longitude from days since J2000. */
  static computeLs(daysSinceJ2000: number): number {
    const m = wrap360(19.3871 + 0.52402073 * daysSinceJ2000);
    const alphaFms = wrap360(270.3871 + 0.524038496 * daysSinceJ2000);
    const mRad = m * D2R;
    const eoc =
      (10.691 + 3.0e-7 * daysSinceJ2000) * Math.sin(mRad) +
      0.623 * Math.sin(2 * mRad) +
      0.05 * Math.sin(3 * mRad) +
      0.005 * Math.sin(4 * mRad) +
      0.0005 * Math.sin(5 * mRad);
    return wrap360(alphaFms + eoc);
  }

  /** Solar declination (deg): asin(sin(25.19°)·sin(Ls)). */
  static solarDeclinationDeg(lsDeg: number): number {
    return Math.asin(Math.sin(25.19 * D2R) * Math.sin(lsDeg * D2R)) * R2D;
  }

  /** Sun elevation above the horizon (deg) at a latitude for a time-of-sol. */
  static sunElevationDeg(lsDeg: number, latitudeDeg: number, timeOfSol: number): number {
    const delta = SimClock.solarDeclinationDeg(lsDeg) * D2R;
    const lat = latitudeDeg * D2R;
    const hourAngle = (timeOfSol - 0.5) * 2 * Math.PI;
    const sinEl =
      Math.sin(lat) * Math.sin(delta) + Math.cos(lat) * Math.cos(delta) * Math.cos(hourAngle);
    return Math.asin(Math.max(-1, Math.min(1, sinEl))) * R2D;
  }

  /** Sun azimuth (deg, 0=N, 90=E) for visualization. */
  static sunAzimuthDeg(lsDeg: number, latitudeDeg: number, timeOfSol: number): number {
    const delta = SimClock.solarDeclinationDeg(lsDeg) * D2R;
    const lat = latitudeDeg * D2R;
    const h = (timeOfSol - 0.5) * 2 * Math.PI;
    const el = Math.asin(
      Math.max(
        -1,
        Math.min(
          1,
          Math.sin(lat) * Math.sin(delta) + Math.cos(lat) * Math.cos(delta) * Math.cos(h),
        ),
      ),
    );
    const cosAz =
      (Math.sin(delta) - Math.sin(lat) * Math.sin(el)) / (Math.cos(lat) * Math.cos(el) + 1e-9);
    const az = Math.acos(Math.max(-1, Math.min(1, cosAz))) * R2D;
    return h > 0 ? 360 - az : az;
  }

  /** Mars–Sun distance (AU) for a given Ls; perihelion at Ls=251°. */
  static marsSunDistanceAu(lsDeg: number): number {
    const a = 1.523679;
    const e = 0.0934;
    const trueAnomaly = (lsDeg - 251) * D2R;
    return (a * (1 - e * e)) / (1 + e * Math.cos(trueAnomaly));
  }
}
