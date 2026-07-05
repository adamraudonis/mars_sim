import { SimClock } from '../clock';
import { Fidelity, SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { Units } from '../units';

const D2R = Units.deg2rad;

/**
 * The Mars surface environment at the settlement site: sun geometry, insolation vs optical
 * depth, dust storm process, temperature, pressure, radiation.
 *
 * Insolation: TOA flux from the Mars–Sun distance; direct beam via Beer's law; global
 * horizontal via a normalized net-flux approximation calibrated to Appelbaum & Flood
 * (NASA TM-102299) — Mars dust is strongly forward-scattering (Pollack), so a τ=5 storm
 * cuts GHI ~3×, not e⁻⁵. L2 adds the stochastic regional/global storm process; L1 is the
 * seasonal climatology; L0 a single mean τ.
 */
export class MarsEnvironment extends SimModule {
  latitudeDeg = 40;
  elevationKm = -3;

  override get displayName() {
    return 'Mars environment';
  }
  override get maxFidelity() {
    return Fidelity.L2;
  }
  override get isEnvironmentProvider() {
    return true;
  }

  private regionalStorm = false;
  private globalStorm = false;
  private stormStartSol = 0;
  private stormEndSol = 0;
  private stormTau = 0;
  private lastStormCheckSol = -1;

  private tauClear!: Param;
  private tauDusty!: Param;
  private tauMean!: Param;
  private pGlobalStormPerYear!: Param;
  private stormTauGlobal!: Param;
  private stormDurGlobal!: Param;
  private pRegionalPerSol!: Param;
  private stormTauRegional!: Param;
  private stormDurRegional!: Param;
  private netFluxK!: Param;
  private groundAlbedo!: Param;
  private tMeanAnnual!: Param;
  private tSeasonalAmp!: Param;
  private tDiurnalAmp!: Param;
  private pMean!: Param;
  private pSeasonalAmp!: Param;
  private doseSurface!: Param;
  private windMean!: Param;
  private solarConstant!: Param;

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.solarConstant = p.getOrRegister('mars_environment.solar_constant_1au', 'Solar constant at 1 AU', 1361, 'W/m2',
      'Kopp & Lean 2011, TSI composite');
    this.tauClear = p.getOrRegister('mars_environment.tau_clear', 'Background optical depth, clear season (Ls 0-135)', 0.4, 'tau',
      'MER/MSL climatology (Lemmon et al. 2015)');
    this.tauDusty = p.getOrRegister('mars_environment.tau_dusty', 'Background optical depth, dusty season (Ls 180-330)', 0.9, 'tau',
      'MER/MSL climatology (Lemmon et al. 2015)');
    this.tauMean = p.getOrRegister('mars_environment.tau_mean', 'Mean optical depth (L0)', 0.6, 'tau',
      'Derived: annual mean of climatology');
    this.pGlobalStormPerYear = p.getOrRegister('mars_environment.global_storm_prob_per_year', 'Global dust storm probability per Mars year', 0.33, 'probability',
      'Zurek & Martin 1993 (~1 per 3 Mars years)');
    this.stormTauGlobal = p.getOrRegister('mars_environment.global_storm_tau', 'Global storm peak optical depth', 6.0, 'tau',
      '2018 storm: tau 8-11 at Opportunity (Guzewich et al. 2019); 5-6 typical');
    this.stormDurGlobal = p.getOrRegister('mars_environment.global_storm_duration', 'Global storm duration', 90, 'sols',
      '2018 & 2001 storm observations');
    this.pRegionalPerSol = p.getOrRegister('mars_environment.regional_storm_prob_per_sol', 'Regional storm probability per sol (dusty season)', 0.004, 'probability',
      'Estimate: several regional storms per dusty season (Kahre et al. 2017)');
    this.stormTauRegional = p.getOrRegister('mars_environment.regional_storm_tau', 'Regional storm peak optical depth', 2.5, 'tau',
      'MER observations of regional events');
    this.stormDurRegional = p.getOrRegister('mars_environment.regional_storm_duration', 'Regional storm duration', 20, 'sols',
      'MER observations of regional events');
    this.netFluxK = p.getOrRegister('mars_environment.net_flux_k', 'Net-flux extinction coefficient (GHI approximation)', 0.26, '',
      'Calibrated to Appelbaum & Flood 1990 (NASA TM-102299) net flux tables; forward-scattering dust');
    this.groundAlbedo = p.getOrRegister('mars_environment.ground_albedo', 'Surface albedo', 0.25, '',
      'TES albedo maps, mid-latitude average');
    this.tMeanAnnual = p.getOrRegister('mars_environment.t_mean_annual', 'Annual mean air temperature (site)', -55, 'degC',
      'REMS/Viking mid-latitude records');
    this.tSeasonalAmp = p.getOrRegister('mars_environment.t_seasonal_amp', 'Seasonal temperature amplitude', 20, 'degC',
      'Viking lander records vs Ls');
    this.tDiurnalAmp = p.getOrRegister('mars_environment.t_diurnal_amp', 'Diurnal temperature half-amplitude', 35, 'degC',
      'REMS diurnal range 60-80 degC');
    this.pMean = p.getOrRegister('mars_environment.pressure_mean', 'Mean surface pressure (site)', 720, 'Pa',
      'Viking/REMS, adjusted for low-elevation site');
    this.pSeasonalAmp = p.getOrRegister('mars_environment.pressure_seasonal_amp', 'Seasonal pressure amplitude', 70, 'Pa',
      'Viking annual CO2 condensation cycle');
    this.doseSurface = p.getOrRegister('human_factors.surface_dose_msv_per_sol', 'GCR surface dose rate (unshielded)', 0.7, 'mSv/sol',
      'MSL RAD: 0.64-0.71 mSv/day (Hassler et al. 2014)');
    this.windMean = p.getOrRegister('mars_environment.wind_mean', 'Mean near-surface wind speed', 7, 'm/s',
      'Viking/InSight anemometry');
  }

  override tick(ctx: SimContext): void {
    const env = ctx.env;
    const ls = ctx.clock.ls;
    const timeOfSol = ctx.clock.timeOfSol;

    // --- Optical depth ---
    let tau =
      this.effectiveFidelity === Fidelity.L0 ? this.tauMean.value : this.seasonalTau(ls);
    if (this.effectiveFidelity === Fidelity.L2) {
      this.updateStormProcess(ctx, ls);
      if (this.globalStorm || this.regionalStorm)
        tau = Math.max(tau, this.stormTauNow(ctx.clock.sol));
    }
    env.opticalDepthTau = tau;
    env.globalDustStorm = this.globalStorm;

    // --- Sun geometry & insolation ---
    const rAu = SimClock.marsSunDistanceAu(ls);
    const toa = this.solarConstant.value / (rAu * rAu);
    env.topOfAtmosphereWm2 = toa;

    const el = SimClock.sunElevationDeg(ls, this.latitudeDeg, timeOfSol);
    env.sunElevationDeg = el;
    env.sunAzimuthDeg = SimClock.sunAzimuthDeg(ls, this.latitudeDeg, timeOfSol);

    if (el > 0.1) {
      const sinEl = Math.sin(el * D2R);
      const airmass = 1 / Math.max(sinEl, 0.05);
      env.directNormalWm2 = toa * Math.exp(-tau * airmass);
      // Diffuse dominates at low sun (A&F: f(z=85°, τ=0.5) ≈ 0.15-0.2) — cap the airmass
      // in the global term; min(1,·) keeps albedo bounce under the no-atmosphere limit.
      const effAirmass = Math.min(airmass, 3);
      env.globalHorizontalWm2 =
        toa *
        sinEl *
        Math.min(1, Math.exp(-this.netFluxK.value * tau * effAirmass) * (1 + 0.15 * this.groundAlbedo.value));
    } else {
      env.directNormalWm2 = 0;
      env.globalHorizontalWm2 = 0;
    }

    // --- Temperature & pressure ---
    const seasonal =
      this.tSeasonalAmp.value * Math.sin((ls - 70) * D2R) * (this.latitudeDeg >= 0 ? 1 : -1);
    const diurnal = this.tDiurnalAmp.value * Math.cos((timeOfSol - 14 / Units.solHours) * 2 * Math.PI);
    env.airTemperatureC = this.tMeanAnnual.value + seasonal + diurnal;
    env.groundTemperatureC = env.airTemperatureC + (el > 0 ? 10 : -10);

    env.pressurePa =
      this.pMean.value +
      this.pSeasonalAmp.value *
        (0.7 * Math.sin((ls - 240) * D2R) + 0.3 * Math.sin(2 * (ls - 240) * D2R));
    env.windSpeedMs = this.windMean.value * (this.globalStorm ? 2 : 1);
    env.surfaceDoseMsvPerSol = this.doseSurface.value;
  }

  private seasonalTau(ls: number): number {
    let dusty = 0.5 * (1 + Math.cos((ls - 255) * D2R)); // peaks at Ls 255
    dusty = Math.pow(dusty, 1.5);
    return this.tauClear.value + (this.tauDusty.value - this.tauClear.value) * dusty;
  }

  private updateStormProcess(ctx: SimContext, ls: number): void {
    const sol = ctx.clock.sol;
    if (Math.floor(sol) > Math.floor(this.lastStormCheckSol)) {
      const stormSeason = ls > 180 && ls < 330;
      if (!this.globalStorm && !this.regionalStorm && stormSeason) {
        const pGlobalPerSol =
          this.pGlobalStormPerYear.value / (Units.solsPerMarsYear * (150 / 360));
        if (this.rng.chance(pGlobalPerSol)) {
          this.globalStorm = true;
          this.stormTau = this.stormTauGlobal.value * this.rng.range(0.8, 1.3);
          this.stormStartSol = sol;
          this.stormEndSol = sol + this.stormDurGlobal.value * this.rng.range(0.7, 1.4);
          this.log(ctx, 'critical',
            `GLOBAL DUST STORM began (tau ${this.stormTau.toFixed(1)}, forecast ${(this.stormEndSol - sol).toFixed(0)} sols)`);
        } else if (this.rng.chance(this.pRegionalPerSol.value)) {
          this.regionalStorm = true;
          this.stormTau = this.stormTauRegional.value * this.rng.range(0.7, 1.4);
          this.stormStartSol = sol;
          this.stormEndSol = sol + this.stormDurRegional.value * this.rng.range(0.5, 1.5);
          this.log(ctx, 'warning', `Regional dust storm began (tau ${this.stormTau.toFixed(1)})`);
        }
      } else if ((this.globalStorm || this.regionalStorm) && sol > this.stormEndSol) {
        this.log(ctx, 'milestone',
          this.globalStorm ? 'Global dust storm cleared' : 'Regional dust storm cleared');
        this.globalStorm = false;
        this.regionalStorm = false;
      }
    }
    this.lastStormCheckSol = sol;
  }

  private stormTauNow(sol: number): number {
    // Ramp ~5 sols, plateau, decay over the last third — of the ACTUAL randomized duration.
    const elapsed = sol - this.stormStartSol;
    const remaining = this.stormEndSol - sol;
    const total = Math.max(1, this.stormEndSol - this.stormStartSol);
    if (elapsed < 5) return this.stormTau * (0.3 + (0.7 * elapsed) / 5);
    if (remaining < total * 0.33) return this.stormTau * Math.max(0.1, remaining / (total * 0.33));
    return this.stormTau;
  }

  override get statusLine(): string {
    return this.globalStorm ? 'GLOBAL DUST STORM' : this.regionalStorm ? 'Regional dust storm' : 'Nominal';
  }

  override get keyFigures(): Array<[string, number, string]> {
    const env = this.engine.context.env;
    return [
      ['Latitude', this.latitudeDeg, '°N'],
      ['Optical depth τ', env.opticalDepthTau, ''],
      ['Air temp', env.airTemperatureC, '°C'],
    ];
  }
}
