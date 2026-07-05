import { SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { LoadPriority } from '../power';
import type { Store } from '../store';
import { MaintenanceSystem } from './maintenance';

const R = 8.314;
const MOL_O2 = 0.032;
const MOL_CO2 = 0.044;
const MOL_N2 = 0.028;

/**
 * The pressurized envelope: cabin atmosphere as a well-mixed ideal-gas volume (O2/CO2/N2
 * masses → partial pressures). Crew/ECLSS/greenhouse exchange gas through this module, so
 * the UI charts the ppO2/ppCO2 the crew actually breathe. Handles leakage, N2 makeup,
 * emergency O2 makeup, the emergency open-loop CO2 purge, and the habitat's baseline load.
 */
export class Habitat extends SimModule {
  pressurizedVolumeM3 = 0;
  cabinO2Kg = 0;
  cabinCO2Kg = 0;
  cabinN2Kg = 0;
  cabinTempC = 22;

  private purging = false;

  private totalPressure!: Param;
  private ppO2Set!: Param;
  private ppCO2Max!: Param;
  private purgePpCO2!: Param;
  private leakPerDay!: Param;
  private basePowerPerM3!: Param;
  private powerPerCrew!: Param;
  private shieldFactor!: Param;

  private o2Store!: Store;
  private n2Store!: Store;

  override get displayName() {
    return 'Habitat';
  }

  get ppO2Kpa(): number {
    return this.partialPressureKpa(this.cabinO2Kg, MOL_O2);
  }
  get ppCO2Kpa(): number {
    return this.partialPressureKpa(this.cabinCO2Kg, MOL_CO2);
  }
  get ppN2Kpa(): number {
    return this.partialPressureKpa(this.cabinN2Kg, MOL_N2);
  }
  get totalPressureKpa(): number {
    return this.ppO2Kpa + this.ppCO2Kpa + this.ppN2Kpa;
  }
  get shieldingFactor(): number {
    return this.shieldFactor.value;
  }

  private partialPressureKpa(kg: number, molarMass: number): number {
    if (this.pressurizedVolumeM3 <= 0) return 0;
    const tK = this.cabinTempC + 273.15;
    return ((kg / molarMass) * R * tK) / this.pressurizedVolumeM3 / 1000;
  }

  private kgForPartialPressure(kpa: number, molarMass: number): number {
    const tK = this.cabinTempC + 273.15;
    return (kpa * 1000 * this.pressurizedVolumeM3 * molarMass) / (R * tK);
  }

  /** Mass of O2 needed to raise cabin ppO2 by the given kPa (0 if no volume). */
  kgToRaisePpO2(kpa: number): number {
    return kpa <= 0 ? 0 : this.kgForPartialPressure(kpa, MOL_O2);
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.totalPressure = p.getOrRegister('eclss.cabin_total_pressure', 'Cabin total pressure', 101.3, 'kPa',
      'ISS standard; 70.3 kPa is the exploration alternative (BVAD)');
    this.ppO2Set = p.getOrRegister('eclss.ppo2_setpoint', 'ppO2 setpoint', 21.2, 'kPa',
      'ISS 19.5-23.1 kPa range (NASA-STD-3001)');
    this.ppCO2Max = p.getOrRegister('eclss.ppco2_limit', 'ppCO2 alarm limit', 0.667, 'kPa',
      '5.3 mmHg 180-day SMAC (NASA-STD-3001; ISS ops target lower)');
    this.purgePpCO2 = p.getOrRegister('eclss.emergency_purge_ppco2_kpa', 'Emergency open-loop purge threshold', 2.0, 'kPa',
      'Hold cabin below the ~2 kPa sustained-harm level (NASA exposure limits)');
    this.leakPerDay = p.getOrRegister('eclss.leakage_fraction_per_day', 'Cabin leakage', 0.0005, 'fraction/day',
      'ISS-class: ~0.05%/day of cabin mass (BVAD structural leak spec)');
    this.basePowerPerM3 = p.getOrRegister('eclss.hab_base_power_w_per_m3', 'Habitat baseline power density', 12, 'W/m3',
      'Estimate: thermal control + avionics + lighting for insulated Mars hab');
    this.powerPerCrew = p.getOrRegister('eclss.hab_power_per_crew_kw', 'Additional habitat power per crew', 0.3, 'kW',
      'Galley, hygiene, plug loads (BVAD)');
    this.shieldFactor = p.getOrRegister('human_factors.hab_shielding_factor', 'Habitat GCR shielding factor', 0.5, '',
      'Unburied hab ~0.5; 2-3 m regolith cover ~0.15 (Simonsen & Nealy)');

    this.o2Store = ctx.stores.getOrCreate('o2_reserve', 'o2', 0);
    this.n2Store = ctx.stores.getOrCreate('n2_reserve', 'n2', 0);
  }

  addVolume(ctx: SimContext, m3: number, arrivesPressurized = false): void {
    this.pressurizedVolumeM3 += m3;
    if (arrivesPressurized) {
      this.cabinO2Kg +=
        (this.kgForPartialPressure(this.ppO2Set.value, MOL_O2) * m3) /
        Math.max(1, this.pressurizedVolumeM3);
      this.cabinN2Kg +=
        (this.kgForPartialPressure(this.totalPressure.value - this.ppO2Set.value, MOL_N2) * m3) /
        Math.max(1, this.pressurizedVolumeM3);
    }
    this.log(ctx, 'milestone', `Pressurized volume now ${this.pressurizedVolumeM3.toFixed(0)} m³`);
  }

  injectO2(kg: number): void {
    this.cabinO2Kg += Math.max(0, kg);
  }
  drawO2(kg: number): number {
    const got = Math.min(kg, this.cabinO2Kg);
    this.cabinO2Kg -= got;
    return got;
  }
  injectCO2(kg: number): void {
    this.cabinCO2Kg += Math.max(0, kg);
  }
  drawCO2(kg: number): number {
    const got = Math.min(kg, this.cabinCO2Kg);
    this.cabinCO2Kg -= got;
    return got;
  }

  override preTick(ctx: SimContext): void {
    const crew = this.engine.find(CrewModule);
    const baseKw =
      (this.pressurizedVolumeM3 * this.basePowerPerM3.value) / 1000 +
      (crew?.count ?? 0) * this.powerPerCrew.value;
    ctx.power.request(this, baseKw, LoadPriority.Critical);
  }

  override tick(ctx: SimContext): void {
    if (this.pressurizedVolumeM3 <= 0) return;

    // Structural leakage, proportional per gas.
    const leak = this.leakPerDay.value * (ctx.dtSeconds / 86400);
    this.cabinO2Kg *= 1 - leak;
    this.cabinCO2Kg *= 1 - leak;
    this.cabinN2Kg *= 1 - leak;

    // N2 makeup toward total pressure setpoint.
    const n2Deficit = this.kgForPartialPressure(
      Math.max(0, this.totalPressure.value - this.ppO2Kpa - this.ppCO2Kpa - this.ppN2Kpa),
      MOL_N2,
    );
    if (n2Deficit > 1e-6) this.cabinN2Kg += this.n2Store.withdraw(n2Deficit);

    // Emergency O2 makeup from reserve if ppO2 sags below 95% of setpoint.
    const ppO2 = this.ppO2Kpa;
    if (ppO2 < this.ppO2Set.value * 0.95) {
      const deficitKg = this.kgForPartialPressure(this.ppO2Set.value - ppO2, MOL_O2);
      this.cabinO2Kg += this.o2Store.withdraw(deficitKg);
    }

    // Emergency open-loop purge (Apollo-13 style): vent cabin air, repressurize from
    // stores — dumps CO2 at the cost of O2/N2 reserve mass.
    if (this.ppCO2Kpa > this.purgePpCO2.value && this.o2Store.amountKg > 1) {
      let ventFrac = Math.min(0.5, 0.6 * ctx.dtSols);
      const o2Needed = this.cabinO2Kg * ventFrac;
      const o2Got = this.o2Store.withdraw(o2Needed);
      ventFrac *= o2Needed > 1e-9 ? o2Got / o2Needed : 0;
      this.cabinCO2Kg *= 1 - ventFrac;
      this.cabinN2Kg -= this.cabinN2Kg * ventFrac - this.n2Store.withdraw(this.cabinN2Kg * ventFrac);
      this.cabinO2Kg += o2Got - this.cabinO2Kg * ventFrac;
      if (!this.purging) {
        this.purging = true;
        this.log(ctx, 'critical', 'EMERGENCY: open-loop CO2 purge — venting cabin air, repressurizing from reserves');
      }
    } else if (this.purging && this.ppCO2Kpa < this.purgePpCO2.value * 0.5) {
      this.purging = false;
      this.log(ctx, 'milestone', 'Open-loop CO2 purge secured — scrubbers holding');
    }

    if (this.ppCO2Kpa > this.ppCO2Max.value && ctx.clock.stepCount % 25 === 0)
      this.log(ctx, 'warning', `ppCO2 ${(this.ppCO2Kpa * 7.50062).toFixed(1)} mmHg above limit`);

    this.record(ctx, 'hab.ppo2', 'Cabin ppO₂', 'kPa', this.ppO2Kpa);
    this.record(ctx, 'hab.ppco2', 'Cabin ppCO₂', 'kPa', this.ppCO2Kpa);
    this.record(ctx, 'hab.pressure', 'Cabin pressure', 'kPa', this.totalPressureKpa);
    this.record(ctx, 'hab.volume', 'Pressurized volume', 'm³', this.pressurizedVolumeM3);
  }

  override get statusLine(): string {
    return `${this.totalPressureKpa.toFixed(1)} kPa, ppO₂ ${this.ppO2Kpa.toFixed(1)} kPa, ppCO₂ ${(this.ppCO2Kpa * 7.50062).toFixed(1)} mmHg`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Volume', this.pressurizedVolumeM3, 'm³'],
      ['ppO₂', this.ppO2Kpa, 'kPa'],
      ['ppCO₂', this.ppCO2Kpa * 7.50062, 'mmHg'],
      ['Pressure', this.totalPressureKpa, 'kPa'],
    ];
  }
}

/**
 * The humans: metabolism (BVAD rates), labor supply, EVA exposure, radiation dose, and a
 * sober health model — sustained deprivation degrades a health index that cuts
 * productivity and ultimately causes loss of crew.
 */
export class CrewModule extends SimModule {
  count = 0;
  healthIndex = 1;
  cumulativeDoseMsv = 0;
  fatalities = 0;

  private hypoxiaSols = 0;
  private hypercapniaSols = 0;
  private dehydrationSols = 0;
  private starvationSols = 0;

  private o2PerCmDay!: Param;
  private co2PerCmDay!: Param;
  private waterPerCmDay!: Param;
  private wastePerCmDay!: Param;
  private kcalPerCmDay!: Param;
  private kcalPerKgFood!: Param;
  private workHoursPerSol!: Param;
  private evaFraction!: Param;
  private doseLimit!: Param;
  private hypoxiaPpO2!: Param;
  private hypercapniaKpa!: Param;

  private water!: Store;
  private wasteWater!: Store;
  private food!: Store;
  private hab?: Habitat;

  override get displayName() {
    return 'Crew';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.o2PerCmDay = p.getOrRegister('eclss.o2_consumption_kg_cm_day', 'O2 consumption', 0.82, 'kg/CM-day',
      'NASA BVAD (nominal metabolic load)');
    this.co2PerCmDay = p.getOrRegister('eclss.co2_production_kg_cm_day', 'CO2 production', 1.04, 'kg/CM-day',
      'NASA BVAD (respiratory quotient ~0.87)');
    this.waterPerCmDay = p.getOrRegister('eclss.water_use_kg_cm_day', 'Potable water use', 3.6, 'kg/CM-day',
      'NASA BVAD water balance tables');
    this.wastePerCmDay = p.getOrRegister('eclss.wastewater_kg_cm_day', 'Wastewater return', 3.4, 'kg/CM-day',
      'NASA BVAD water balance tables');
    this.kcalPerCmDay = p.getOrRegister('food.kcal_per_cm_day', 'Dietary energy requirement', 3040, 'kcal/CM-day',
      'NASA BVAD (moderate activity, mixed crew)');
    this.kcalPerKgFood = p.getOrRegister('food.kcal_per_kg_packaged', 'Packaged food energy density (as-shipped)', 1650, 'kcal/kg',
      'BVAD: ~1.83 kg/CM-day for 3040 kcal incl. packaging');
    this.workHoursPerSol = p.getOrRegister('human_factors.work_hours_per_sol', 'Productive work hours per crew per sol', 6.5, 'h/sol',
      'ISS experience: ~6.5 h scheduled work/day');
    this.evaFraction = p.getOrRegister('human_factors.eva_fraction', 'Fraction of work hours on EVA', 0.15, '',
      'Ops assumption (tunable)');
    this.doseLimit = p.getOrRegister('human_factors.career_dose_limit_msv', 'Career radiation dose limit', 600, 'mSv',
      'NASA-STD-3001 (2021 update): 600 mSv career');
    this.hypoxiaPpO2 = p.getOrRegister('human_factors.hypoxia_ppo2_kpa', 'Hypoxia threshold ppO2', 15, 'kPa',
      'NASA-STD-3001 minimum alveolar requirement region');
    this.hypercapniaKpa = p.getOrRegister('human_factors.hypercapnia_ppco2_kpa', 'Hypercapnia health threshold', 2, 'kPa',
      'NASA exposure limits: sustained >15 mmHg degrades cognition/health');

    this.water = ctx.stores.getOrCreate('water_potable', 'waterPotable', 0);
    this.wasteWater = ctx.stores.getOrCreate('water_waste', 'waterWaste', 0);
    this.food = ctx.stores.getOrCreate('food', 'food', 0);
    this.hab = this.engine.find(Habitat);
  }

  arrive(ctx: SimContext, people: number): void {
    this.count += people;
    this.log(ctx, 'milestone', `${people} crew arrived — ${this.count} now on Mars`);
  }

  override preTick(ctx: SimContext): void {
    if (this.count === 0) return;
    const hours =
      this.count * this.workHoursPerSol.value * ctx.dtSols * Math.max(0, Math.min(1, this.healthIndex));
    ctx.labor.supplyCrewHours(hours);
  }

  override tick(ctx: SimContext): void {
    if (this.count === 0) return;
    const cmDays = (this.count * ctx.dtSeconds) / 86400;

    // --- Breathe ---
    const o2Needed = this.o2PerCmDay.value * cmDays;
    const o2Got = this.hab ? this.hab.drawO2(o2Needed) : o2Needed;
    this.hab?.injectCO2(this.co2PerCmDay.value * cmDays);

    const hypoxic =
      this.hab !== undefined &&
      (this.hab.ppO2Kpa < this.hypoxiaPpO2.value || o2Got < o2Needed * 0.95);
    this.hypoxiaSols = hypoxic ? this.hypoxiaSols + ctx.dtSols : 0;

    const hypercapnic = this.hab !== undefined && this.hab.ppCO2Kpa > this.hypercapniaKpa.value;
    this.hypercapniaSols = hypercapnic
      ? this.hypercapniaSols + ctx.dtSols
      : Math.max(0, this.hypercapniaSols - ctx.dtSols);

    // --- Drink / wash ---
    const waterNeeded = this.waterPerCmDay.value * cmDays;
    const waterGot = this.water.withdraw(waterNeeded);
    this.wasteWater.deposit(this.wastePerCmDay.value * cmDays * (waterGot / Math.max(1e-9, waterNeeded)));
    this.dehydrationSols =
      waterGot < waterNeeded * 0.7
        ? this.dehydrationSols + ctx.dtSols
        : Math.max(0, this.dehydrationSols - ctx.dtSols);

    // --- Eat ---
    const kcalNeeded = this.kcalPerCmDay.value * cmDays;
    const foodGot = this.food.withdraw(kcalNeeded / this.kcalPerKgFood.value);
    this.starvationSols =
      foodGot < (kcalNeeded / this.kcalPerKgFood.value) * 0.8
        ? this.starvationSols + ctx.dtSols
        : Math.max(0, this.starvationSols - 0.5 * ctx.dtSols);

    // --- Radiation ---
    const doseRate = ctx.env.surfaceDoseMsvPerSol;
    const shield = this.hab?.shieldingFactor ?? 1;
    const evaFrac = this.evaFraction.value * (this.workHoursPerSol.value / 24.66);
    this.cumulativeDoseMsv += doseRate * (evaFrac + (1 - evaFrac) * shield) * ctx.dtSols;
    if (this.cumulativeDoseMsv > this.doseLimit.value && ctx.clock.stepCount % 1000 === 0)
      this.log(ctx, 'warning', `Average crew dose ${this.cumulativeDoseMsv.toFixed(0)} mSv exceeds career limit`);

    // --- Health integration ---
    let drain = 0;
    if (this.hypoxiaSols > 0.05) drain += 2 * ctx.dtSols;
    if (this.hypercapniaSols > 0.5) drain += 0.1 * ctx.dtSols;
    if (this.dehydrationSols > 1) drain += 0.25 * ctx.dtSols;
    if (this.starvationSols > 10) drain += 0.03 * ctx.dtSols;
    const recovery = drain === 0 ? 0.02 * ctx.dtSols : 0;
    this.healthIndex = Math.max(0, Math.min(1, this.healthIndex - drain + recovery));

    if (this.healthIndex <= 0 && this.count > 0) {
      const lost = Math.max(1, Math.floor(this.count * 0.1));
      this.count -= lost;
      this.fatalities += lost;
      this.healthIndex = 0.3;
      this.log(ctx, 'critical', `LOSS OF CREW: ${lost} fatalities (${this.count} remain)`);
    }

    this.record(ctx, 'crew.count', 'Crew on Mars', 'people', this.count);
    this.record(ctx, 'crew.health', 'Crew health index', '', this.healthIndex);
    this.record(ctx, 'crew.dose', 'Cumulative dose (avg)', 'mSv', this.cumulativeDoseMsv);
    this.record(ctx, 'crew.food_days', 'Food reserve at current crew', 'sols',
      this.count > 0
        ? (this.food.amountKg * this.kcalPerKgFood.value) / (this.kcalPerCmDay.value * this.count * 1.0275)
        : 0);
    this.record(ctx, 'crew.water_days', 'Water reserve at current crew', 'sols',
      this.count > 0 ? this.water.amountKg / (this.waterPerCmDay.value * this.count * 1.0275) : 0);
  }

  override get statusLine(): string {
    return this.count === 0
      ? 'No crew on surface'
      : `${this.count} crew, health ${(this.healthIndex * 100).toFixed(0)}%, dose ${this.cumulativeDoseMsv.toFixed(0)} mSv`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Crew', this.count, 'people'],
      ['Health index', this.healthIndex * 100, '%'],
      ['Avg dose', this.cumulativeDoseMsv, 'mSv'],
      ['Fatalities', this.fatalities, ''],
    ];
  }
}

/**
 * Regenerative life support chain (ISS-heritage, scaled by design crew): CO2 removal, O2
 * generation (mass-based deficit control), Sabatier (throughput-capped), water recovery.
 * Each function degrades with its own hardware via MaintenanceSystem function tags.
 */
export class Eclss extends SimModule {
  designCrew = 0;

  private co2CapPerCm!: Param;
  private co2KwhPerKg!: Param;
  private ogaCapPerCm!: Param;
  private ogaKwhPerKgO2!: Param;
  private sabConversion!: Param;
  private sabVent!: Param;
  private wrsRecovery!: Param;
  private wrsKwhPerKg!: Param;
  private o2ReserveTargetSols!: Param;
  private baseKwPerCm!: Param;

  private co2!: Store;
  private h2!: Store;
  private o2Store!: Store;
  private water!: Store;
  private wasteWater!: Store;
  private ch4Depot!: Store;
  private hab?: Habitat;
  private crew?: CrewModule;
  private maint?: MaintenanceSystem;

  private stepPowerKw = 0;

  override get displayName() {
    return 'ECLSS';
  }

  /** ECLSS idles at keep-alive until crew are aboard — dormant racks don't consume MTBF. */
  override get failureDutyCycle(): number {
    return this.designCrew > 0 && (this.crew?.count ?? 0) > 0 ? 1 : 0.05;
  }

  private fcap(tag: string): number {
    return this.maint?.functionCapacity(this, tag) ?? this.capacityFactor;
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.co2CapPerCm = p.getOrRegister('eclss.co2_removal_capacity_kg_cm_day', 'CO2 removal capacity per crew', 1.3, 'kg/CM-day',
      'CDRA sized ~125% of metabolic CO2 (BVAD)');
    this.co2KwhPerKg = p.getOrRegister('eclss.co2_removal_kwh_per_kg', 'CO2 removal specific energy', 4, 'kWh/kg CO2',
      'CDRA-class (ISS power reports)');
    this.ogaCapPerCm = p.getOrRegister('eclss.oga_capacity_kg_cm_day', 'O2 generation capacity per crew', 1, 'kg/CM-day',
      'ISS OGA up to ~9 kg/day for 6+ crew');
    this.ogaKwhPerKgO2 = p.getOrRegister('eclss.oga_kwh_per_kg_o2', 'O2 generation specific energy', 11, 'kWh/kg O2',
      'ISS OGA nominal incl. avionics');
    this.sabConversion = p.getOrRegister('eclss.sabatier_conversion', 'Sabatier CO2 conversion efficiency', 0.9, '',
      'ISS CRA flight performance');
    this.sabVent = p.getOrRegister('eclss.sabatier_vent_ch4', 'Vent Sabatier CH4 (1) or send to depot (0)', 1, 'bool',
      'ISS vents; on Mars CH4 is propellant — a trade the sim exposes');
    this.wrsRecovery = p.getOrRegister('eclss.water_recovery_fraction', 'Water recovery fraction', 0.98, '',
      'ISS WRS demonstrated 98% (NASA 2023)');
    this.wrsKwhPerKg = p.getOrRegister('eclss.wrs_kwh_per_kg', 'Water recovery specific energy', 0.4, 'kWh/kg',
      'UPA+WPA ~1.5 kW for 6 crew scale');
    this.o2ReserveTargetSols = p.getOrRegister('eclss.o2_reserve_target_sols', 'O2 reserve target', 60, 'sols',
      'Ops policy: buffer against OGA outage (tunable)');
    this.baseKwPerCm = p.getOrRegister('eclss.base_kw_per_crew', 'ECLSS hotel load per crew', 0.25, 'kW',
      'ISS ECLSS secondary loads scaled');

    this.co2 = ctx.stores.getOrCreate('co2_buffer', 'co2', 500);
    this.h2 = ctx.stores.getOrCreate('h2_eclss', 'h2', 50);
    this.o2Store = ctx.stores.getOrCreate('o2_reserve', 'o2', 0);
    this.water = ctx.stores.getOrCreate('water_potable', 'waterPotable', 0);
    this.wasteWater = ctx.stores.getOrCreate('water_waste', 'waterWaste', 0);
    this.ch4Depot = ctx.stores.getOrCreate('depot_ch4', 'ch4', 0);
    this.hab = this.engine.find(Habitat);
    this.crew = this.engine.find(CrewModule);
    this.maint = this.engine.find(MaintenanceSystem);
  }

  override preTick(ctx: SimContext): void {
    if (this.designCrew === 0 || !this.hab) return;
    const days = ctx.dtSeconds / 86400;
    const co2Max = this.co2CapPerCm.value * this.designCrew * days;
    const o2Max = this.ogaCapPerCm.value * this.designCrew * days;
    const wrsMax = this.wasteWater.amountKg * Math.min(1, 4.8 * ctx.dtSols);
    this.stepPowerKw =
      (co2Max * this.co2KwhPerKg.value + o2Max * this.ogaKwhPerKgO2.value + wrsMax * this.wrsKwhPerKg.value) /
        ctx.dtHours +
      this.baseKwPerCm.value * this.designCrew;
    ctx.power.request(this, this.stepPowerKw, LoadPriority.Critical);
  }

  override tick(ctx: SimContext): void {
    if (this.designCrew === 0 || !this.hab) return;
    const powerGrant = ctx.power.grantedFraction(this);
    const days = ctx.dtSeconds / 86400;

    // --- CO2 removal ---
    const co2Cap = this.co2CapPerCm.value * this.designCrew * days * powerGrant * this.fcap('co2');
    const co2Scrubbed = this.hab.drawCO2(Math.min(co2Cap, this.hab.cabinCO2Kg * 0.5));
    this.co2.deposit(co2Scrubbed);

    // --- OGA: close actual ppO2 deficit (mass-based) + rebuild reserve while crew aboard ---
    const o2Cap = this.ogaCapPerCm.value * this.designCrew * days * powerGrant * this.fcap('oga');
    const deficitKg = this.hab.kgToRaisePpO2(ctx.params.v('eclss.ppo2_setpoint') - this.hab.ppO2Kpa);
    const o2ForCabin = Math.min(o2Cap, deficitKg);
    const crewCount = this.crew?.count ?? 0;
    const reserveTargetKg =
      crewCount > 0
        ? ctx.params.v('eclss.o2_consumption_kg_cm_day') * crewCount * 1.0275 * this.o2ReserveTargetSols.value
        : 0;
    const o2ForReserve =
      this.o2Store.amountKg < reserveTargetKg && this.o2Store.freeKg > o2Cap ? o2Cap * 0.3 : 0;
    const o2ToMake = Math.min(o2Cap, o2ForCabin + o2ForReserve);

    // Electrolysis stoichiometry: 9 kg H2O -> 8 kg O2 + 1 kg H2.
    const waterNeeded = (o2ToMake * 9) / 8;
    const waterGot = this.water.withdraw(waterNeeded);
    const o2Made = (waterGot * 8) / 9;
    const h2Made = waterGot / 9;
    if (o2ForCabin > 0) {
      const toCabin = Math.min(o2Made, o2ForCabin);
      this.hab.injectO2(toCabin);
      this.o2Store.deposit(o2Made - toCabin);
    } else this.o2Store.deposit(o2Made);
    this.h2.deposit(h2Made);

    // --- Sabatier: CO2 + 4H2 -> CH4 + 2H2O (per kg CH4: 2.75 CO2 + 0.5 H2 -> 2.25 H2O),
    //     throughput-capped to the reactor rate so behavior is timestep-independent ---
    const reactorCapKg = co2Cap / 2.75;
    const ch4Possible =
      Math.min(reactorCapKg, Math.min(this.h2.amountKg / 0.5, this.co2.amountKg / 2.75)) *
      this.sabConversion.value *
      powerGrant *
      this.fcap('oga');
    if (ch4Possible > 1e-9) {
      this.h2.withdraw(ch4Possible * 0.5);
      this.co2.withdraw(ch4Possible * 2.75);
      this.water.deposit(ch4Possible * 2.25);
      if (this.sabVent.value < 0.5) this.ch4Depot.deposit(ch4Possible);
    }

    // --- Water recovery (per-sol drain rate) ---
    const wrsDrainFrac = Math.min(1, 4.8 * ctx.dtSols) * powerGrant * this.fcap('wrs');
    const wrsIn = this.wasteWater.withdraw(this.wasteWater.amountKg * wrsDrainFrac);
    this.water.deposit(wrsIn * this.wrsRecovery.value); // brine remainder is lost mass

    this.record(ctx, 'eclss.power', 'ECLSS power draw', 'kW', this.stepPowerKw * powerGrant);
    this.record(ctx, 'eclss.o2_production', 'O2 generation rate', 'kg/sol', o2Made / ctx.dtSols);
    this.record(ctx, 'eclss.co2_scrubbed', 'CO2 removal rate', 'kg/sol', co2Scrubbed / ctx.dtSols);
  }

  override get statusLine(): string {
    return this.designCrew === 0 ? 'Not installed' : `Sized for ${this.designCrew} crew (${this.health})`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Design crew', this.designCrew, 'people'],
      ['O₂ reserve', this.engine.stores.get('o2_reserve')?.amountKg ?? 0, 'kg'],
      ['Potable water', this.engine.stores.get('water_potable')?.amountKg ?? 0, 'kg'],
    ];
  }
}
