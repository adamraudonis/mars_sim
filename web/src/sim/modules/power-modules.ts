import { Fidelity, SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { LoadPriority } from '../power';
import { LaborPriority } from '../labor';
import { Units } from '../units';

/**
 * Photovoltaic farm.
 * L2: per-step irradiance geometry × cell efficiency × temperature derate × dust factor,
 *     with dust deposition (faster in storms) and labor-driven cleaning.
 * L1: same, dust held at cleaning equilibrium.
 * L0: distilled scalar — installed kW × (kWh/sol per kW) spread uniformly over the sol.
 */
export class SolarFarm extends SimModule {
  arrayAreaM2 = 0;
  dustFraction = 0;
  outputKw = 0;

  private ageYears = 0;
  private cleaningHoursWanted = 0;

  private cellEff!: Param;
  private tempCoeff!: Param;
  private tRef!: Param;
  private dustRate!: Param;
  private dustStormRate!: Param;
  private cleaningThreshold!: Param;
  private cleaningHoursPer100m2!: Param;
  private electrostatic!: Param;
  private specificMass!: Param;
  private l0Yield!: Param;
  private degradationPerYear!: Param;

  override get displayName() {
    return 'Solar farm';
  }
  override get maxFidelity() {
    return Fidelity.L2;
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.cellEff = p.getOrRegister('power_solar.cell_efficiency', 'PV cell efficiency (BOL)', 0.3, '',
      'Flight multi-junction (XTJ/IMM) class');
    this.tempCoeff = p.getOrRegister('power_solar.temp_coeff', 'Efficiency temperature coefficient', -0.0025, '1/degC',
      'Triple-junction typical -0.2..-0.3 %/degC');
    this.tRef = p.getOrRegister('power_solar.t_ref', 'Cell reference temperature', 25, 'degC', 'Standard rating condition');
    this.dustRate = p.getOrRegister('power_solar.dust_rate_per_sol', 'Dust obscuration accumulation', 0.0025, 'fraction/sol',
      'MER flight data ~0.28%/sol early mission (Kinch et al.)');
    this.dustStormRate = p.getOrRegister('power_solar.dust_storm_rate_per_sol', 'Dust accumulation during storms', 0.01, 'fraction/sol',
      'InSight 2018 storm experience');
    this.cleaningThreshold = p.getOrRegister('power_solar.cleaning_threshold', 'Dust fraction triggering cleaning', 0.15, '',
      'Ops policy (tunable)');
    this.cleaningHoursPer100m2 = p.getOrRegister('power_solar.cleaning_hours_per_100m2', 'Cleaning labor per 100 m²', 1, 'crew-eq h',
      'Estimate: brushing/wiping rate analog');
    this.electrostatic = p.getOrRegister('power_solar.electrostatic_cleaning', 'Electrostatic dust removal installed (0/1)', 0, 'bool',
      'EDS technology (Calle et al., KSC) — optional upgrade');
    this.specificMass = p.getOrRegister('power_solar.specific_mass_kg_m2', 'Array system specific mass', 2.5, 'kg/m2',
      'NASA Mars surface array studies (ROSA-derived, incl. structure)');
    this.l0Yield = p.getOrRegister('power_solar.l0_kwh_per_sol_per_kw', 'Distilled yield per installed kW (L0)', 4.5, 'kWh/sol/kW',
      'Derived: distilled from L2 run at 40N (rating basis 500 W/m2 clear noon)');
    this.degradationPerYear = p.getOrRegister('power_solar.degradation_per_earth_year', 'Cell degradation', 0.01, 'fraction/year',
      'GaAs on-orbit experience ~0.5-1.5%/yr');
  }

  /** Installed kW rating at reference conditions (Mars noon, clear, 25 °C, 500 W/m²). */
  get installedKwRating(): number {
    return (this.arrayAreaM2 * this.cellEff.value * 500) / 1000;
  }

  get arrayMassKg(): number {
    return this.arrayAreaM2 * this.specificMass.value;
  }

  override preTick(ctx: SimContext): void {
    this.outputKw = this.computeOutputKw(ctx);
    ctx.power.offer(this.outputKw);

    // Cleaning labor requested here (before labor resolve) so the grant read in tick
    // reflects a real allocation. Badly dusted arrays outrank ordinary construction.
    this.cleaningHoursWanted = 0;
    if (
      this.effectiveFidelity === Fidelity.L2 &&
      this.arrayAreaM2 > 0 &&
      this.dustFraction > this.cleaningThreshold.value &&
      this.electrostatic.value < 0.5
    ) {
      this.cleaningHoursWanted =
        (this.arrayAreaM2 / 100) * this.cleaningHoursPer100m2.value * 0.2 * ctx.dtSols;
      const prio =
        this.dustFraction > 2 * this.cleaningThreshold.value ? LaborPriority.High : LaborPriority.Normal;
      ctx.labor.request(this, 'logistics', this.cleaningHoursWanted, prio);
    }
  }

  private computeOutputKw(ctx: SimContext): number {
    if (this.arrayAreaM2 <= 0) return 0;
    const derate =
      (1 - this.dustFraction) *
      this.capacityFactor *
      Math.pow(1 - this.degradationPerYear.value, this.ageYears);

    if (this.effectiveFidelity === Fidelity.L0)
      return ((this.installedKwRating * this.l0Yield.value) / Units.solHours) * derate;

    const ghi = ctx.env.globalHorizontalWm2;
    if (ghi <= 0) return 0;
    const cellTemp = ctx.env.airTemperatureC + ghi * 0.06; // ~+30C at 500 W/m² (MER experience)
    const eff = this.cellEff.value * (1 + this.tempCoeff.value * (cellTemp - this.tRef.value));
    return ((this.arrayAreaM2 * eff * ghi) / 1000) * derate;
  }

  override tick(ctx: SimContext): void {
    this.ageYears += ctx.dtSeconds / (365.25 * 86400);

    if (this.effectiveFidelity === Fidelity.L2) {
      const rate =
        ctx.env.globalDustStorm || ctx.env.opticalDepthTau > 2
          ? this.dustStormRate.value
          : this.dustRate.value;
      this.dustFraction = Math.min(0.95, this.dustFraction + rate * ctx.dtSols);

      if (this.dustFraction > this.cleaningThreshold.value && this.arrayAreaM2 > 0) {
        if (this.electrostatic.value > 0.5) {
          this.dustFraction *= Math.pow(0.5, ctx.dtSols); // EDS clears in ~1 sol
        } else if (this.cleaningHoursWanted > 0) {
          const granted = ctx.labor.grantedFraction(this, 'logistics');
          const areaCleaned = granted * 0.2 * this.arrayAreaM2 * ctx.dtSols;
          this.dustFraction = Math.max(
            0,
            this.dustFraction - (this.dustFraction * areaCleaned) / Math.max(1, this.arrayAreaM2),
          );
        }
      }
    } else if (this.effectiveFidelity === Fidelity.L1) {
      this.dustFraction = this.cleaningThreshold.value * 0.5; // cleaning equilibrium
    } else {
      this.dustFraction = 0; // folded into the distilled coefficient
    }

    this.record(ctx, 'solar.output', 'Solar output', 'kW', this.outputKw);
    this.record(ctx, 'solar.dust', 'Array dust obscuration', '%', this.dustFraction * 100);
    this.record(ctx, 'solar.installed', 'Solar installed rating', 'kW', this.installedKwRating);
  }

  override get statusLine(): string {
    return `${this.outputKw.toFixed(0)} kW of ${this.installedKwRating.toFixed(0)} kW rated, dust ${(this.dustFraction * 100).toFixed(0)}%`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Array area', this.arrayAreaM2, 'm²'],
      ['Installed rating', this.installedKwRating, 'kW'],
      ['Array mass', this.arrayMassKg / 1000, 't'],
      ['Dust obscuration', this.dustFraction * 100, '%'],
    ];
  }
}

/**
 * Fission surface power: N reactor units (FSP 40 kWe class by default). Output is
 * weather-independent — that is the whole trade against solar.
 */
export class NuclearPlant extends SimModule {
  units = 0;
  outputKw = 0;
  private ageYears = 0;

  private unitKwe!: Param;
  private unitMassKg!: Param;
  private lifetimeYears!: Param;

  override get displayName() {
    return 'Fission power plant';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.unitKwe = p.getOrRegister('power_nuclear.unit_kwe', 'Reactor unit electrical power', 40, 'kWe',
      'NASA Fission Surface Power project target (2022 industry awards)');
    this.unitMassKg = p.getOrRegister('power_nuclear.unit_mass_kg', 'Reactor unit mass (landed, shielded)', 6820, 'kg',
      'FSP 40 kWe reference design 6.8 t (research campaign)');
    this.lifetimeYears = p.getOrRegister('power_nuclear.lifetime_years', 'Design lifetime', 10, 'years',
      'FSP requirement: 10 years unattended');
    p.getOrRegister('power_nuclear.keep_out_distance_m', 'Crew keep-out distance (shadow shield)', 1000, 'm',
      'Kilopower siting studies (Gibson et al. 2017)');
  }

  get plantMassKg(): number {
    return this.units * this.unitMassKg.value;
  }

  override preTick(ctx: SimContext): void {
    const eol = this.ageYears > this.lifetimeYears.value ? 0 : 1;
    const degradation = 1 - 0.05 * Math.min(1, this.ageYears / this.lifetimeYears.value);
    this.outputKw = this.units * this.unitKwe.value * degradation * this.capacityFactor * eol;
    ctx.power.offer(this.outputKw);
  }

  override tick(ctx: SimContext): void {
    const prevAge = this.ageYears;
    this.ageYears += ctx.dtSeconds / (365.25 * 86400);
    if (prevAge <= this.lifetimeYears.value && this.ageYears > this.lifetimeYears.value && this.units > 0)
      this.log(ctx, 'critical', `Reactor fleet reached end of design life (${this.lifetimeYears.value.toFixed(0)} yr)`);
    this.record(ctx, 'nuclear.output', 'Nuclear output', 'kW', this.outputKw);
  }

  override get statusLine(): string {
    return this.units === 0
      ? 'No units deployed'
      : `${this.units} × ${this.unitKwe.value.toFixed(0)} kWe, output ${this.outputKw.toFixed(0)} kW`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Units', this.units, ''],
      ['Output', this.outputKw, 'kW'],
      ['Plant mass', this.plantMassKg / 1000, 't'],
      ['Fleet age', this.ageYears, 'yr'],
    ];
  }
}

/**
 * Li-ion storage. The PowerBus owns instantaneous charge/discharge; this module owns
 * sizing, mass, fade, and the reserve policy parameters.
 */
export class BatteryBank extends SimModule {
  nameplateKwh = 0;
  private ageYears = 0;

  private specificEnergy!: Param;
  private roundTrip!: Param;
  private minSoc!: Param;
  private fadePerYear!: Param;
  private maxCRate!: Param;
  private criticalReserve!: Param;

  override get displayName() {
    return 'Battery storage';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.specificEnergy = p.getOrRegister('power_solar.battery_wh_per_kg', 'Battery pack specific energy', 180, 'Wh/kg',
      'Li-ion aerospace packs (BVAD power tables)');
    this.roundTrip = p.getOrRegister('power_solar.battery_round_trip_eff', 'Battery round-trip efficiency', 0.94, '',
      'Li-ion typical 92-96%');
    this.minSoc = p.getOrRegister('power_solar.battery_min_soc', 'Battery minimum state of charge', 0.2, 'fraction',
      'DoD limit for cycle life (aerospace practice)');
    this.fadePerYear = p.getOrRegister('power_solar.battery_fade_per_year', 'Capacity fade', 0.02, 'fraction/year',
      'Li-ion calendar+cycle fade under daily cycling');
    this.maxCRate = p.getOrRegister('power_solar.battery_max_c_rate', 'Max charge/discharge C-rate', 0.5, '1/h',
      'Conservative thermal limit');
    this.criticalReserve = p.getOrRegister('power_solar.battery_reserve_soc_for_critical', 'Battery SoC reserved for critical loads', 0.5, 'fraction',
      'Ops policy: below this SoC, ISRU/greenhouse loads shed to protect life support overnight');

    this.syncBus(ctx);
    ctx.power.battery.energyKwh = ctx.power.battery.capacityKwh;
  }

  get massKg(): number {
    return (this.nameplateKwh * 1000) / this.specificEnergy.value;
  }

  private syncBus(ctx: SimContext): void {
    const b = ctx.power.battery;
    const faded = this.nameplateKwh * Math.pow(1 - this.fadePerYear.value, this.ageYears);
    b.capacityKwh = faded * this.capacityFactor;
    b.chargeEfficiency = this.roundTrip.value;
    b.minSocFraction = this.minSoc.value;
    b.criticalReserveSoc = this.criticalReserve.value;
    b.maxChargeRateKw = faded * this.maxCRate.value;
    b.maxDischargeRateKw = faded * this.maxCRate.value;
    if (b.energyKwh > b.capacityKwh) b.energyKwh = b.capacityKwh;
  }

  override preTick(ctx: SimContext): void {
    this.syncBus(ctx);
  }

  override tick(ctx: SimContext): void {
    this.ageYears += ctx.dtSeconds / (365.25 * 86400);
    this.record(ctx, 'battery.capacity', 'Battery capacity', 'kWh', ctx.power.battery.capacityKwh);
    this.record(ctx, 'battery.energy', 'Battery energy', 'kWh', ctx.power.battery.energyKwh);
  }

  override get statusLine(): string {
    const b = this.engine.power.battery;
    return `${b.energyKwh.toFixed(0)} / ${b.capacityKwh.toFixed(0)} kWh (${(b.soc * 100).toFixed(0)}%)`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    const b = this.engine.power.battery;
    return [
      ['Capacity', b.capacityKwh, 'kWh'],
      ['State of charge', b.soc * 100, '%'],
      ['Bank mass', this.massKg / 1000, 't'],
    ];
  }
}
