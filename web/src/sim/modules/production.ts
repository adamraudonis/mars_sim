import { Fidelity, SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { LoadPriority } from '../power';
import { LaborPriority } from '../labor';
import type { Store } from '../store';
import { Units } from '../units';
import { Habitat } from './life-support';

interface CropBatch {
  areaM2: number;
  plantedSol: number;
  cycleSols: number;
  progress: number;
}

/**
 * LED-lit crop production (shielded growth modules — light is all electric, which is what
 * makes food-vs-power one of the core trades). L1: staggered crop batches with discrete
 * harvests; growth drives cabin gas exchange continuously. L0: distilled continuous
 * production (nominal rate × distilled utilization).
 */
export class Greenhouse extends SimModule {
  growingAreaM2 = 0;

  private batches: CropBatch[] = [];
  private lastPlantSol = -999;
  private lightsKw = 0;

  private kcalPerM2Sol!: Param;
  private kwhPerM2Sol!: Param;
  private photoperiodH!: Param;
  private cycleSols!: Param;
  private edibleFraction!: Param;
  private waterPerM2Sol!: Param;
  private transpirationRecovery!: Param;
  private laborPerM2Sol!: Param;
  private kcalPerKgFood!: Param;
  private co2PerKgBiomass!: Param;
  private o2PerKgBiomass!: Param;
  private systemMassPerM2!: Param;
  private failureRework!: Param;

  private food!: Store;
  private water!: Store;
  private biomass!: Store;
  private o2Reserve!: Store;
  private hab?: Habitat;

  override get displayName() {
    return 'Greenhouse';
  }
  override get maxFidelity() {
    return Fidelity.L2;
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.kcalPerM2Sol = p.getOrRegister('food.kcal_per_m2_sol', 'Edible output per growing area', 80, 'kcal/m2/sol',
      'BVAD crop tables: potato/wheat class at high PPF');
    this.kwhPerM2Sol = p.getOrRegister('food.led_kwh_per_m2_sol', 'LED electrical energy per growing area', 18, 'kWh/m2/sol',
      'PPF ~800 umol/m2-s, 20 h photoperiod, LED efficacy ~2.8 umol/J');
    this.photoperiodH = p.getOrRegister('food.photoperiod_hours', 'Photoperiod', 20, 'h/sol', 'BVAD crop tables');
    this.cycleSols = p.getOrRegister('food.crop_cycle_sols', 'Mean crop cycle', 85, 'sols',
      'BVAD: potato ~132 d, wheat ~62 d, lettuce ~28 d — area-weighted mix');
    this.edibleFraction = p.getOrRegister('food.harvest_index', 'Edible fraction of biomass (harvest index)', 0.45, '',
      'BVAD crop tables');
    this.waterPerM2Sol = p.getOrRegister('food.water_per_m2_sol', 'Crop water throughput', 2.5, 'kg/m2/sol',
      'Transpiration-dominated (BVAD: 1.5-4 L/m2-day)');
    this.transpirationRecovery = p.getOrRegister('food.transpiration_recovery', 'Transpired water recovered as condensate', 0.95, '',
      'Closed-loop condensing HX assumption');
    this.laborPerM2Sol = p.getOrRegister('food.labor_min_per_m2_sol', 'Tending labor', 0.015, 'crew-eq h/m2/sol',
      'CELSS studies: ~1.5 h/day per 100 m2');
    this.kcalPerKgFood = p.getOrRegister('food.kcal_per_kg_packaged', 'Packaged food energy density (as-shipped)', 1650, 'kcal/kg',
      'BVAD: ~1.83 kg/CM-day for 3040 kcal incl. packaging');
    p.getOrRegister('food.l0_utilization', 'Distilled batch-model utilization (L0)', 0.9, '',
      'Derived: staggering gaps + batch losses measured by greenhouse distillation');
    this.co2PerKgBiomass = p.getOrRegister('food.co2_per_kg_biomass', 'CO2 fixed per kg dry biomass', 1.6, 'kg/kg',
      'Photosynthesis stoichiometry (CH2O basis)');
    this.o2PerKgBiomass = p.getOrRegister('food.o2_per_kg_biomass', 'O2 released per kg dry biomass', 1.2, 'kg/kg',
      'Photosynthesis stoichiometry');
    this.systemMassPerM2 = p.getOrRegister('food.system_mass_kg_m2', 'Growth system mass per area', 90, 'kg/m2',
      'BVAD biomass production chamber estimates');
    this.failureRework = p.getOrRegister('food.crop_loss_probability', 'Probability a batch is lost', 0.05, 'probability',
      'CELSS risk studies (tunable)');

    this.food = ctx.stores.getOrCreate('food', 'food', 0);
    this.water = ctx.stores.getOrCreate('water_potable', 'waterPotable', 0);
    this.biomass = ctx.stores.getOrCreate('biomass', 'biomass', 100000);
    this.o2Reserve = ctx.stores.getOrCreate('o2_reserve', 'o2', 0);
    this.hab = this.engine.find(Habitat);
  }

  get systemMassKg(): number {
    return this.growingAreaM2 * this.systemMassPerM2.value;
  }

  override preTick(ctx: SimContext): void {
    if (this.growingAreaM2 <= 0) return;

    const lightsOn =
      this.effectiveFidelity === Fidelity.L0 || ctx.clock.localSolarHours < this.photoperiodH.value;
    const avgKw = (this.growingAreaM2 * this.kwhPerM2Sol.value) / Units.solHours;
    this.lightsKw =
      this.effectiveFidelity === Fidelity.L0
        ? avgKw
        : lightsOn
          ? avgKw * (Units.solHours / this.photoperiodH.value)
          : 0;

    ctx.power.request(this, this.lightsKw, LoadPriority.Low);
    ctx.labor.request(this, 'agriculture', this.growingAreaM2 * this.laborPerM2Sol.value * ctx.dtSols, LaborPriority.Normal);
  }

  override tick(ctx: SimContext): void {
    if (this.growingAreaM2 <= 0) return;
    const powerGrant = ctx.power.grantedFraction(this);
    const laborGrant = ctx.labor.grantedFraction(this, 'agriculture');
    const effectiveness = Math.min(powerGrant, 0.5 + 0.5 * laborGrant) * this.capacityFactor;

    // kcalGrowth drives gas exchange continuously; harvests deposit food.
    let kcalGrowth: number;
    if (this.effectiveFidelity === Fidelity.L0) {
      const util = ctx.params.v('food.l0_utilization');
      kcalGrowth = this.growingAreaM2 * this.kcalPerM2Sol.value * util * ctx.dtSols * effectiveness;
      this.food.deposit(kcalGrowth / this.kcalPerKgFood.value);
    } else {
      kcalGrowth = this.tickBatches(ctx, effectiveness);
    }

    // Gas exchange: crops preferentially scrub cabin CO2 (helping ECLSS); the shortfall is
    // enriched from the 95%-CO2 atmosphere. O2 tops the cabin to setpoint; surplus banks.
    const dryBiomass = kcalGrowth / 4000 / Math.max(0.05, this.edibleFraction.value);
    if (this.hab && dryBiomass > 0) {
      const co2Wanted = dryBiomass * this.co2PerKgBiomass.value;
      this.hab.drawCO2(Math.min(co2Wanted, this.hab.cabinCO2Kg * 0.5));
      const o2Made = dryBiomass * this.o2PerKgBiomass.value;
      const deficitKg = this.hab.kgToRaisePpO2(ctx.params.v('eclss.ppo2_setpoint') - this.hab.ppO2Kpa);
      const toCabin = Math.min(o2Made, deficitKg);
      this.hab.injectO2(toCabin);
      this.o2Reserve.deposit(o2Made - toCabin);
    }

    // Water loop: transpiration mostly recovered.
    const waterUsed = this.growingAreaM2 * this.waterPerM2Sol.value * ctx.dtSols * effectiveness;
    const got = this.water.withdraw(waterUsed);
    this.water.deposit(got * this.transpirationRecovery.value);

    this.record(ctx, 'greenhouse.kcal_per_sol', 'Food production', 'kcal/sol', kcalGrowth / ctx.dtSols);
    this.record(ctx, 'greenhouse.power', 'Greenhouse lighting', 'kW', this.lightsKw * powerGrant);
    this.record(ctx, 'greenhouse.area', 'Growing area', 'm²', this.growingAreaM2);
  }

  /** Returns kcal of biomass GROWN this step; harvests deposit food at the pantry density. */
  private tickBatches(ctx: SimContext, effectiveness: number): number {
    let planted = 0;
    for (const b of this.batches) planted += b.areaM2;
    if (planted < this.growingAreaM2 - 1 && ctx.clock.sol - this.lastPlantSol > this.cycleSols.value / 8) {
      this.batches.push({
        areaM2: Math.min(this.growingAreaM2 / 8, this.growingAreaM2 - planted),
        plantedSol: ctx.clock.sol,
        cycleSols: this.cycleSols.value * this.rng.range(0.9, 1.1),
        progress: 0,
      });
      this.lastPlantSol = ctx.clock.sol;
    }

    let kcalGrown = 0;
    for (let i = this.batches.length - 1; i >= 0; i--) {
      const b = this.batches[i];
      const progressStep = (ctx.dtSols / b.cycleSols) * effectiveness;
      b.progress += progressStep;
      kcalGrown += b.areaM2 * this.kcalPerM2Sol.value * b.cycleSols * progressStep;
      if (b.progress >= 1) {
        this.batches.splice(i, 1);
        if (this.rng.chance(this.failureRework.value)) {
          this.log(ctx, 'warning', `Crop batch lost (${b.areaM2.toFixed(0)} m²) — disease/system failure`);
          continue;
        }
        const batchKcal = b.areaM2 * this.kcalPerM2Sol.value * b.cycleSols;
        const foodKg = batchKcal / this.kcalPerKgFood.value;
        this.food.deposit(foodKg);
        this.biomass.deposit((foodKg * (1 - this.edibleFraction.value)) / Math.max(0.05, this.edibleFraction.value));
        this.log(ctx, 'info', `Harvest: ${foodKg.toFixed(0)} kg food (${(batchKcal / 1000).toFixed(0)} Mcal) from ${b.areaM2.toFixed(0)} m²`);
      }
    }
    return kcalGrown;
  }

  override get statusLine(): string {
    return this.growingAreaM2 <= 0
      ? 'Not installed'
      : `${this.growingAreaM2.toFixed(0)} m² growing, ${this.batches.length} batches`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Growing area', this.growingAreaM2, 'm²'],
      ['Lighting draw', this.lightsKw, 'kW'],
      ['System mass', this.systemMassKg / 1000, 't'],
    ];
  }
}

/**
 * Methalox propellant production. Two architectures, switchable live:
 * A) water-based (electrolysis supplies H2 and all O2; 2.25 kg fresh water per kg CH4);
 * B) H2-import + SOXE (Sabatier on Earth H2, product water re-electrolyzed recycling half
 *    the H2, remaining O2 from MOXIE-style CO2 electrolysis; zero mined water).
 */
export class IsruPropellantPlant extends SimModule {
  capacityKgPerSol = 0;
  commissioned = 0;
  productionKgPerSol = 0;

  private ofRatio!: Param;
  private co2KwhPerKg!: Param;
  private elecKwhPerKgH2O!: Param;
  private sabKwhPerKgCh4!: Param;
  private liqKwhPerKgCh4!: Param;
  private liqKwhPerKgO2!: Param;
  private architecture!: Param;
  private soxeKwhPerKgO2!: Param;
  private plantMassPerKgSol!: Param;
  private opsHoursPerTonne!: Param;
  private commissionHours!: Param;
  private commissionSols!: Param;
  private l0KwhPerKg!: Param;

  private water!: Store;
  private h2Import!: Store;
  private ch4!: Store;
  private lox!: Store;

  override get displayName() {
    return 'ISRU propellant plant';
  }

  override get failureDutyCycle(): number {
    return this.capacityKgPerSol <= 0
      ? 0
      : this.commissioned < 1
        ? 0.05
        : Math.max(0.05, Math.min(1, this.productionKgPerSol / Math.max(1, this.capacityKgPerSol)));
  }

  private get useH2Import(): boolean {
    return this.architecture.value > 0.5;
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.ofRatio = p.getOrRegister('starship.of_ratio', 'Raptor O:F mixture ratio', 3.6, 'kg O2/kg CH4',
      'Raptor methalox, SpaceX statements');
    this.co2KwhPerKg = p.getOrRegister('isru_atmosphere.co2_acquisition_kwh_per_kg', 'CO2 acquisition energy', 0.9, 'kWh/kg CO2',
      'Cryogenic CO2 freezer studies (Muscatello et al.)');
    this.elecKwhPerKgH2O = p.getOrRegister('isru_atmosphere.electrolysis_kwh_per_kg_h2o', 'Water electrolysis energy', 6, 'kWh/kg H2O',
      'PEM ~50-55 kWh/kg H2 incl. BoP');
    this.sabKwhPerKgCh4 = p.getOrRegister('isru_atmosphere.sabatier_kwh_per_kg_ch4', 'Sabatier system energy', 0.8, 'kWh/kg CH4',
      'Exothermic; energy is compressors/thermal management');
    this.liqKwhPerKgCh4 = p.getOrRegister('isru_atmosphere.liquefaction_kwh_per_kg_ch4', 'CH4 liquefaction energy', 0.9, 'kWh/kg',
      'Mars-surface cryocooler studies');
    this.liqKwhPerKgO2 = p.getOrRegister('isru_atmosphere.liquefaction_kwh_per_kg_o2', 'O2 liquefaction energy', 0.5, 'kWh/kg',
      'Mars-surface cryocooler studies');
    this.architecture = p.getOrRegister('isru_atmosphere.architecture', 'Plant architecture (0=water-based, 1=H2-import+SOXE)', 0, 'mode',
      'Trade switch: ice mining vs Earth H2 + CO2-only O2');
    this.soxeKwhPerKgO2 = p.getOrRegister('isru_atmosphere.soxe_kwh_per_kg_o2', 'SOXE O2 specific energy (full scale)', 11, 'kWh/kg O2',
      'MOXIE flight ~30 kWh/kg system; full-scale projections ~11 (Hecht et al.)');
    this.plantMassPerKgSol = p.getOrRegister('isru_atmosphere.plant_mass_kg_per_kg_sol', 'Plant specific mass', 22.4, 'kg per (kg/sol)',
      'Scaled from NASA ISRU system studies (Kleinhenz & Paz)');
    this.opsHoursPerTonne = p.getOrRegister('isru_atmosphere.ops_hours_per_tonne', 'Operations labor', 1.5, 'crew-eq h/t',
      'Estimate: monitoring + maintenance rounds');
    this.commissionHours = p.getOrRegister('isru_atmosphere.commission_hours_per_kg_sol', 'Commissioning labor per capacity', 0.5, 'crew-eq h per kg/sol',
      'Estimate: deployment, hookup, checkout');
    this.commissionSols = p.getOrRegister('isru_atmosphere.commission_target_sols', 'Commissioning campaign length target', 50, 'sols',
      'Ops assumption');
    this.l0KwhPerKg = p.getOrRegister('isru_atmosphere.l0_kwh_per_kg_propellant', 'Distilled plant energy (L0)', 8, 'kWh/kg propellant',
      'Derived from research chain values (water-based)');

    this.water = ctx.stores.getOrCreate('water_potable', 'waterPotable', 0);
    this.h2Import = ctx.stores.getOrCreate('h2_import', 'h2', 0);
    this.ch4 = ctx.stores.getOrCreate('depot_ch4', 'ch4', 0);
    this.lox = ctx.stores.getOrCreate('depot_lox', 'lox', 0);
  }

  get plantMassKg(): number {
    return this.capacityKgPerSol * this.plantMassPerKgSol.value;
  }

  /** Per-kg-mix chain coefficients (kWh, fresh water kg, imported H2 kg). */
  chainCoefficients(h2Import: boolean): [kwhPerKg: number, waterPerKg: number, h2PerKg: number] {
    const of = this.ofRatio.value;
    const mCh4 = 1 / (1 + of);
    const mO2 = of / (1 + of);
    const sabatierCo2 = 2.75 * mCh4;
    const sabatierWaterOut = 2.25 * mCh4;

    if (!h2Import) {
      // Electrolysis supplies all H2 (4.5 kg H2O/kg CH4) co-producing 4.0 kg O2/kg CH4;
      // Sabatier water recycled. Fresh water: 2.25 kg/kg CH4.
      const waterElectrolyzed = 4.5 * mCh4;
      const freshWater = waterElectrolyzed - sabatierWaterOut;
      const kwh =
        sabatierCo2 * this.co2KwhPerKg.value +
        mCh4 * this.sabKwhPerKgCh4.value +
        waterElectrolyzed * this.elecKwhPerKgH2O.value +
        mCh4 * this.liqKwhPerKgCh4.value +
        mO2 * this.liqKwhPerKgO2.value;
      return [kwh, freshWater, 0];
    }
    // Sabatier product water electrolyzed: recycles 0.25 kg H2/kg CH4, yields 2.0 kg O2;
    // SOXE covers the remaining O2 from CO2 (2CO2 -> 2CO + O2).
    const waterElectrolyzed = sabatierWaterOut;
    const o2FromWater = (waterElectrolyzed * 8) / 9;
    const h2Recycled = waterElectrolyzed / 9;
    const h2Imported = 0.5 * mCh4 - h2Recycled;
    const o2FromSoxe = Math.max(0, mO2 - o2FromWater);
    const soxeCo2 = o2FromSoxe * 2.75;
    const kwh =
      (sabatierCo2 + soxeCo2) * this.co2KwhPerKg.value +
      mCh4 * this.sabKwhPerKgCh4.value +
      waterElectrolyzed * this.elecKwhPerKgH2O.value +
      o2FromSoxe * this.soxeKwhPerKgO2.value +
      mCh4 * this.liqKwhPerKgCh4.value +
      mO2 * this.liqKwhPerKgO2.value;
    return [kwh, 0, h2Imported];
  }

  override preTick(ctx: SimContext): void {
    if (this.capacityKgPerSol <= 0) return;

    if (this.commissioned < 1) {
      const totalHours = this.capacityKgPerSol * this.commissionHours.value;
      ctx.labor.request(this, 'construction',
        (totalHours / Math.max(1, this.commissionSols.value)) * ctx.dtSols, LaborPriority.Normal);
      return;
    }

    const kwhPerKg =
      this.effectiveFidelity === Fidelity.L0 ? this.l0KwhPerKg.value : this.chainCoefficients(this.useH2Import)[0];
    ctx.power.request(this, (this.capacityKgPerSol * kwhPerKg) / Units.solHours, LoadPriority.Normal);
    ctx.labor.request(this, 'isruOps',
      (this.capacityKgPerSol / 1000) * this.opsHoursPerTonne.value * ctx.dtSols, LaborPriority.Normal);
  }

  override tick(ctx: SimContext): void {
    if (this.capacityKgPerSol <= 0) {
      this.productionKgPerSol = 0;
      return;
    }

    if (this.commissioned < 1) {
      const grantC = ctx.labor.grantedFraction(this, 'construction');
      this.commissioned = Math.min(1, this.commissioned + (grantC * ctx.dtSols) / Math.max(1, this.commissionSols.value));
      if (this.commissioned >= 1)
        this.log(ctx, 'milestone', `Propellant plant commissioned (${(this.capacityKgPerSol / 1000).toFixed(1)} t/sol capacity)`);
      this.productionKgPerSol = 0;
      return;
    }

    const h2Mode = this.useH2Import;
    let [kwhPerKg, waterPerKg, h2PerKg] = this.chainCoefficients(h2Mode);
    if (this.effectiveFidelity === Fidelity.L0) kwhPerKg = this.l0KwhPerKg.value;

    const powerGrant = ctx.power.grantedFraction(this);
    const laborGrant = ctx.labor.grantedFraction(this, 'isruOps');
    const throughput =
      this.capacityKgPerSol * ctx.dtSols * powerGrant * Math.min(1, 0.7 + 0.3 * laborGrant) * this.capacityFactor;

    const maxByWater = waterPerKg > 1e-9 ? this.water.amountKg / waterPerKg : Infinity;
    const maxByH2 = h2PerKg > 1e-9 ? this.h2Import.amountKg / h2PerKg : Infinity;
    const made = Math.min(throughput, maxByWater, maxByH2);

    if (made > 1e-9) {
      const of = this.ofRatio.value;
      if (waterPerKg > 0) this.water.withdraw(made * waterPerKg);
      if (h2PerKg > 0) this.h2Import.withdraw(made * h2PerKg);
      this.ch4.deposit(made / (1 + of));
      this.lox.deposit((made * of) / (1 + of));
    }
    this.productionKgPerSol = made / ctx.dtSols;

    this.record(ctx, 'isru.production', 'Propellant production', 'kg/sol', this.productionKgPerSol);
    this.record(ctx, 'isru.kwh_per_kg', 'Plant specific energy', 'kWh/kg', kwhPerKg);
    this.record(ctx, 'isru.water_demand', 'Plant water demand', 'kg/sol', this.productionKgPerSol * waterPerKg);
    this.record(ctx, 'isru.h2_demand', 'Plant H2 import demand', 'kg/sol', this.productionKgPerSol * h2PerKg);
  }

  override get statusLine(): string {
    return this.capacityKgPerSol <= 0
      ? 'Not installed'
      : this.commissioned < 1
        ? `Commissioning ${(this.commissioned * 100).toFixed(0)}%`
        : `${(this.productionKgPerSol / 1000).toFixed(2)} t/sol of ${(this.capacityKgPerSol / 1000).toFixed(2)} t/sol (${this.useH2Import ? 'H2-import+SOXE' : 'water-based'})`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Capacity', this.capacityKgPerSol, 'kg/sol'],
      ['Production', this.productionKgPerSol, 'kg/sol'],
      ['Plant mass', this.plantMassKg / 1000, 't'],
      ['CH₄ in depot', (this.engine.stores.get('depot_ch4')?.amountKg ?? 0) / 1000, 't'],
      ['LOX in depot', (this.engine.stores.get('depot_lox')?.amountKg ?? 0) / 1000, 't'],
    ];
  }
}

/**
 * Water from subsurface ice (excavate-and-heat baseline; a Rodwell is a parameter change,
 * not a code change). Robot-friendly work; throttles when the tank farm is full.
 */
export class IceMine extends SimModule {
  capacityKgPerSol = 0;
  productionKgPerSol = 0;

  private kwhPerKg!: Param;
  private cleanupKwhPerKg!: Param;
  private cleanupLoss!: Param;
  private oreWaterFraction!: Param;
  private laborHoursPerTonne!: Param;
  private rigMassPerKgSol!: Param;

  private feedstock!: Store;
  private water!: Store;
  private regolith!: Store;

  override get displayName() {
    return 'Ice mine';
  }

  override get failureDutyCycle(): number {
    return this.capacityKgPerSol <= 0
      ? 0
      : Math.max(0.05, Math.min(1, this.productionKgPerSol / Math.max(1, this.capacityKgPerSol)));
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.kwhPerKg = p.getOrRegister('isru_water.extraction_kwh_per_kg', 'Water extraction energy (excavate+heat)', 1.0, 'kWh/kg H2O',
      'NASA ISRU studies: icy regolith thermal extraction (Kleinhenz) + hauling');
    this.cleanupKwhPerKg = p.getOrRegister('isru_water.cleanup_kwh_per_kg', 'Water cleanup energy', 0.1, 'kWh/kg',
      'Filtration + perchlorate treatment estimate');
    this.cleanupLoss = p.getOrRegister('isru_water.cleanup_loss_fraction', 'Cleanup reject fraction', 0.05, '',
      'Brine/contaminant reject estimate');
    this.oreWaterFraction = p.getOrRegister('isru_water.ore_water_fraction', 'Water content of mined material', 0.8, '',
      'SWIM: mid-latitude excess ice deposits (Morgan et al. 2021), derated');
    this.laborHoursPerTonne = p.getOrRegister('isru_water.labor_hours_per_tonne', 'Mining labor per tonne water', 4, 'crew-eq h/t',
      'Estimate: excavator ops, hauling, rig maintenance');
    this.rigMassPerKgSol = p.getOrRegister('isru_water.rig_mass_kg_per_kg_sol', 'Mining system specific mass', 15, 'kg per (kg/sol)',
      'RASSOR-class excavators + processing plant scaling');

    this.feedstock = ctx.stores.getOrCreate('water_feedstock', 'waterFeedstock', 200000);
    this.water = ctx.stores.getOrCreate('water_potable', 'waterPotable', 0);
    this.regolith = ctx.stores.getOrCreate('regolith', 'regolith', Number.MAX_VALUE);
  }

  get systemMassKg(): number {
    return this.capacityKgPerSol * this.rigMassPerKgSol.value;
  }

  override preTick(ctx: SimContext): void {
    if (this.capacityKgPerSol <= 0) return;
    const intake = Math.min(1, this.water.freeKg / Math.max(1, this.capacityKgPerSol * ctx.dtSols));
    const fullKw =
      ((this.kwhPerKg.value + this.cleanupKwhPerKg.value) * this.capacityKgPerSol * intake) / Units.solHours;
    if (fullKw > 0) ctx.power.request(this, fullKw, LoadPriority.Normal);
    ctx.labor.request(this, 'isruOps',
      (this.capacityKgPerSol / 1000) * this.laborHoursPerTonne.value * ctx.dtSols, LaborPriority.Normal);
  }

  override tick(ctx: SimContext): void {
    if (this.capacityKgPerSol <= 0) {
      this.productionKgPerSol = 0;
      return;
    }
    const powerGrant = ctx.power.grantedFraction(this);
    const laborGrant = ctx.labor.grantedFraction(this, 'isruOps');
    let made =
      this.capacityKgPerSol * ctx.dtSols * powerGrant * (0.3 + 0.7 * laborGrant) * this.capacityFactor;
    made = Math.min(made, this.water.freeKg / Math.max(0.01, 1 - this.cleanupLoss.value));

    if (made > 1e-9) {
      this.regolith.deposit((made / Math.max(0.05, this.oreWaterFraction.value)) * (1 - this.oreWaterFraction.value));
      const clean = made * (1 - this.cleanupLoss.value);
      this.water.deposit(clean);
      this.feedstock.deposit(made - clean);
    }
    this.productionKgPerSol = made / ctx.dtSols;
    this.record(ctx, 'icemine.production', 'Water production', 'kg/sol', this.productionKgPerSol);
  }

  override get statusLine(): string {
    return this.capacityKgPerSol <= 0
      ? 'Not installed'
      : `${this.productionKgPerSol.toFixed(0)} kg/sol of ${this.capacityKgPerSol.toFixed(0)} kg/sol`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Capacity', this.capacityKgPerSol, 'kg/sol'],
      ['Production', this.productionKgPerSol, 'kg/sol'],
      ['System mass', this.systemMassKg / 1000, 't'],
    ];
  }
}

/**
 * Cryogenic propellant storage. Zero-boiloff needs continuous cryocooler power; shed power
 * means vented mass — coupling the power architecture to the return-flight schedule.
 */
export class PropellantDepot extends SimModule {
  private boiloffPerSol!: Param;
  private zboKwPerTonne!: Param;
  private ch4!: Store;
  private lox!: Store;

  override get displayName() {
    return 'Propellant depot';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.boiloffPerSol = p.getOrRegister('starship.boiloff_fraction_per_sol', 'Passive cryo boiloff on Mars surface', 0.0025, 'fraction/sol',
      '~0.25%/sol estimate for insulated steel tanks on Mars (research campaign)');
    this.zboKwPerTonne = p.getOrRegister('starship.zbo_kw_per_tonne', 'Zero-boiloff cryocooler power', 0.026, 'kW/t stored',
      '~40 kW cryocooler per full 1550 t ship (research campaign, speculative)');
    this.ch4 = ctx.stores.getOrCreate('depot_ch4', 'ch4', 0);
    this.lox = ctx.stores.getOrCreate('depot_lox', 'lox', 0);
  }

  get storedTonnes(): number {
    return (this.ch4.amountKg + this.lox.amountKg) / 1000;
  }

  override preTick(ctx: SimContext): void {
    const kw = this.storedTonnes * this.zboKwPerTonne.value;
    if (kw > 0) ctx.power.request(this, kw, LoadPriority.High);
  }

  override tick(ctx: SimContext): void {
    const grant = ctx.power.grantedFraction(this);
    if (grant < 0.999 && this.storedTonnes > 0) {
      const frac = this.boiloffPerSol.value * ctx.dtSols * (1 - grant);
      const lostCh4 = this.ch4.withdraw(this.ch4.amountKg * frac);
      const lostLox = this.lox.withdraw(this.lox.amountKg * frac);
      if (lostCh4 + lostLox > 50)
        this.log(ctx, 'warning', `Cryocoolers underpowered — ${(lostCh4 + lostLox).toFixed(0)} kg propellant boiled off`);
    }
    this.record(ctx, 'depot.ch4', 'CH₄ stored', 't', this.ch4.amountKg / 1000);
    this.record(ctx, 'depot.lox', 'LOX stored', 't', this.lox.amountKg / 1000);
    this.record(ctx, 'depot.total', 'Propellant stored', 't', this.storedTonnes);
  }

  override get statusLine(): string {
    return `${(this.ch4.amountKg / 1000).toFixed(0)} t CH₄ + ${(this.lox.amountKg / 1000).toFixed(0)} t LOX`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['CH₄', this.ch4.amountKg / 1000, 't'],
      ['LOX', this.lox.amountKg / 1000, 't'],
      ['ZBO power', this.storedTonnes * this.zboKwPerTonne.value, 'kW'],
    ];
  }
}
