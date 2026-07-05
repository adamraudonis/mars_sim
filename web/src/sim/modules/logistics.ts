import { SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { LoadPriority } from '../power';
import { LaborPriority, type TaskType } from '../labor';
import type { Store } from '../store';
import type { CargoManifest, Flight } from '../scenario';
import { Habitat, CrewModule, Eclss } from './life-support';
import { SolarFarm, NuclearPlant, BatteryBank } from './power-modules';
import { Greenhouse, IceMine, IsruPropellantPlant } from './production';
import { MaintenanceSystem } from './maintenance';

export type ShipRole = 'cargo' | 'crewTransport' | 'tankerDepot';

export interface LandedShip {
  name: string;
  role: ShipRole;
  landedSol: number;
  contributesHabitatVolume: boolean;
  fueledForReturn: boolean;
  departed: boolean;
}

/**
 * Starships on the surface: pressurized volume (crew ships double as early habitat),
 * depot tank capacity, hotel power during buildup (the seed power that lets robots build
 * the farm that replaces it), and the return vehicle whose propellant demand drives the
 * ISRU sizing question.
 */
export class StarshipFleet extends SimModule {
  readonly ships: LandedShip[] = [];
  readonly returnWindowSols: number[] = [];

  private pressVolume!: Param;
  private habUsableFraction!: Param;
  private returnPropTonnes!: Param;
  private tankCapacityTonnes!: Param;
  private hotelPowerKw!: Param;

  private ch4!: Store;
  private lox!: Store;
  private hab?: Habitat;

  override get displayName() {
    return 'Starship fleet';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.pressVolume = p.getOrRegister('starship.pressurized_volume_m3', 'Ship pressurized volume', 1000, 'm3',
      'SpaceX: ~1000 m3 claimed for crew configuration (company claim)');
    this.habUsableFraction = p.getOrRegister('starship.habitat_usable_fraction', 'Usable fraction of ship volume as habitat', 0.6, '',
      'Outfitting, tankage domes, airlocks reduce net habitable volume');
    this.returnPropTonnes = p.getOrRegister('starship.return_propellant_tonnes', 'Propellant to reach Earth-return trajectory', 1200, 't',
      'Mars ascent+TEI ~6.9 km/s; full V3 tanks give ~9.3 km/s at 145 t burnout (research campaign)');
    this.tankCapacityTonnes = p.getOrRegister('starship.tank_capacity_tonnes', 'Ship propellant tank capacity', 1550, 't',
      'Starship V3 as flown: 1550 t (research campaign corrected)');
    this.hotelPowerKw = p.getOrRegister('starship.ship_hotel_power_kw', 'Power each landed ship contributes (deployable panels)', 30, 'kW',
      'Estimate: ship-mounted arrays/fuel cells for surface ops');
    p.getOrRegister('starship.of_ratio', 'Raptor O:F mixture ratio', 3.6, 'kg O2/kg CH4', 'Raptor methalox, SpaceX statements');

    this.ch4 = ctx.stores.getOrCreate('depot_ch4', 'ch4', 0);
    this.lox = ctx.stores.getOrCreate('depot_lox', 'lox', 0);
    this.hab = this.engine.find(Habitat);
  }

  land(ctx: SimContext, name: string, role: ShipRole, contributesVolume: boolean): LandedShip {
    const ship: LandedShip = {
      name, role, landedSol: ctx.clock.sol,
      contributesHabitatVolume: contributesVolume, fueledForReturn: false, departed: false,
    };
    this.ships.push(ship);

    if (contributesVolume && this.hab)
      this.hab.addVolume(ctx, this.pressVolume.value * this.habUsableFraction.value, true);

    const of = ctx.params.v('starship.of_ratio');
    const capKg = this.tankCapacityTonnes.value * 1000;
    this.ch4.addCapacity(capKg / (1 + of));
    this.lox.addCapacity((capKg * of) / (1 + of));

    this.log(ctx, 'milestone', `${name} landed (${role})`);
    return ship;
  }

  get returnPropellantFraction(): number {
    const of = this.engine.params.v('starship.of_ratio');
    const needKg = this.returnPropTonnes.value * 1000;
    const needCh4 = needKg / (1 + of);
    const needLox = (needKg * of) / (1 + of);
    return Math.min(1, Math.min(this.ch4.amountKg / needCh4, this.lox.amountKg / needLox));
  }

  override preTick(ctx: SimContext): void {
    const present = this.ships.filter((s) => !s.departed).length;
    if (present > 0) ctx.power.offer(present * this.hotelPowerKw.value);
  }

  override tick(ctx: SimContext): void {
    const returnShip = this.ships.find((s) => s.role === 'crewTransport' && !s.departed);
    if (returnShip && !returnShip.fueledForReturn && this.returnPropellantFraction >= 1) {
      returnShip.fueledForReturn = true;
      this.log(ctx, 'milestone', `${returnShip.name} fully fueled for Earth return`);
    }

    for (const window of this.returnWindowSols) {
      if (Math.abs(ctx.clock.sol - window) < ctx.dtSols && returnShip?.fueledForReturn && !returnShip.departed) {
        const of = this.engine.params.v('starship.of_ratio');
        const needKg = this.returnPropTonnes.value * 1000;
        this.ch4.withdraw(needKg / (1 + of));
        this.lox.withdraw((needKg * of) / (1 + of));
        returnShip.departed = true;
        this.log(ctx, 'milestone', `${returnShip.name} departed for Earth at the return window`);
      }
    }

    this.record(ctx, 'fleet.ships', 'Ships on surface', '', this.ships.filter((s) => !s.departed).length);
    this.record(ctx, 'fleet.return_prop', 'Return propellant readiness', '%', this.returnPropellantFraction * 100);
  }

  override get statusLine(): string {
    return `${this.ships.filter((s) => !s.departed).length} ships on surface, return propellant ${(this.returnPropellantFraction * 100).toFixed(0)}%`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Ships on surface', this.ships.filter((s) => !s.departed).length, ''],
      ['Return propellant', this.returnPropellantFraction * 100, '%'],
      ['Required', this.returnPropTonnes.value, 't'],
    ];
  }
}

/**
 * Humanoid robot workforce (Optimus-class, speculative in the DB). Robots supply
 * robot-hours with per-task effectiveness; charging power couples to next step's labor
 * supply; the fleet takes shop time itself. count = 0 recovers the humans-only baseline.
 */
export class RobotFleet extends SimModule {
  count = 0;

  private lastChargeGrant = 1;
  private chargeKw = 0;

  private workHoursPerSol!: Param;
  private availability!: Param;
  private chargeKwhPerWorkHour!: Param;
  private unitMassKg!: Param;
  private effByTask = new Map<TaskType, Param>();
  private maintHoursPerRobotSol!: Param;

  override get displayName() {
    return 'Robot fleet';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.workHoursPerSol = p.getOrRegister('robots.work_hours_per_sol', 'Robot work hours per sol', 16, 'h/sol',
      'Duty cycle limited by charging + thermal (company claims, speculative)');
    this.availability = p.getOrRegister('robots.availability', 'Fleet availability', 0.8, '',
      'Estimate: dust, wear, software — speculative');
    this.chargeKwhPerWorkHour = p.getOrRegister('robots.charge_kwh_per_work_hour', 'Charging energy per work hour', 0.35, 'kWh/h',
      'Optimus ~2.3 kWh battery / ~6-8 h work (company claim)');
    this.unitMassKg = p.getOrRegister('robots.unit_mass_kg', 'Robot unit mass', 60, 'kg',
      'Optimus Gen 2 ~57-73 kg (company claim)');
    const eff: Array<[TaskType, string, number, string]> = [
      ['isruOps', 'robots.effectiveness_isru_ops', 0.6, 'monitoring rounds, valve/panel work'],
      ['agriculture', 'robots.effectiveness_agriculture', 0.4, 'delicate manipulation'],
      ['construction', 'robots.effectiveness_construction', 0.5, 'semi-structured assembly'],
      ['logistics', 'robots.effectiveness_logistics', 0.6, 'warehouse-analog structured work'],
      ['maintenance', 'robots.effectiveness_maintenance', 0.25, 'diagnosis-heavy work stays human-led'],
    ];
    for (const [task, id, val, note] of eff)
      this.effByTask.set(task, p.getOrRegister(id, `Robot effectiveness: ${task}`, val, 'crew-eq', note));
    this.maintHoursPerRobotSol = p.getOrRegister('robots.maintenance_hours_per_robot_sol', 'Upkeep labor per robot', 0.05, 'crew-eq h/sol',
      'Estimate: ~1 shop-hour per robot per 20 sols');
  }

  get fleetMassKg(): number {
    return this.count * this.unitMassKg.value;
  }

  override preTick(ctx: SimContext): void {
    if (this.count === 0) return;

    for (const [task, param] of this.effByTask) ctx.labor.setRobotEffectiveness(task, param.value);
    ctx.labor.setRobotEffectiveness('science', 0.2);

    // Work hours scale with the previous step's charging grant (energy-labor coupling).
    const hours =
      this.count * this.availability.value * this.capacityFactor * this.workHoursPerSol.value * ctx.dtSols *
      Math.max(0, Math.min(1, 0.2 + 0.8 * this.lastChargeGrant));
    ctx.labor.supplyRobotHours(hours);

    this.chargeKw =
      (this.count * this.availability.value * this.capacityFactor * this.workHoursPerSol.value * ctx.dtSols *
        this.chargeKwhPerWorkHour.value) / ctx.dtHours;
    ctx.power.request(this, this.chargeKw, LoadPriority.High);

    ctx.labor.request(this, 'maintenance', this.count * this.maintHoursPerRobotSol.value * ctx.dtSols, LaborPriority.High);
  }

  override tick(ctx: SimContext): void {
    if (this.count === 0) return;
    const grant = ctx.power.grantedFraction(this);
    this.lastChargeGrant = grant;
    if (grant < 0.5 && ctx.clock.stepCount % 50 === 0)
      this.log(ctx, 'warning', 'Robot charging power-starved — fleet output reduced');

    this.record(ctx, 'robots.count', 'Robots', '', this.count);
    this.record(ctx, 'robots.hours', 'Robot hours supplied', 'h/sol',
      this.count * this.availability.value * this.capacityFactor * this.workHoursPerSol.value * grant);
    this.record(ctx, 'robots.charge_kw', 'Robot charging', 'kW', this.chargeKw * grant);
  }

  override get statusLine(): string {
    return this.count === 0
      ? 'None deployed'
      : `${this.count} robots, availability ${(this.availability.value * this.capacityFactor * 100).toFixed(0)}%`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Robots', this.count, ''],
      ['Fleet mass', this.fleetMassKg / 1000, 't'],
      ['Charging draw', this.chargeKw, 'kW'],
    ];
  }
}

interface DeploymentTask {
  description: string;
  totalHours: number;
  hoursDone: number;
  onComplete: (ctx: SimContext) => void;
}

/**
 * Executes the scenario's flight schedule. Stores fill on landing; hardware only comes
 * online through a construction-labor deployment queue — a manifest that outpaces the
 * workforce piles up visibly. Registers failure-prone component groups per hardware lot.
 */
export class LaunchCampaign extends SimModule {
  private pending: Flight[] = [];
  private deployQueue: DeploymentTask[] = [];
  private static readonly CONCURRENT = 4;
  private static readonly MAX_HOURS_PER_TASK_PER_SOL = 100;

  private deploySolarPer100m2!: Param;
  private deployPerNuclearUnit!: Param;
  private deployGreenhousePer10m2!: Param;
  private deployHabPer100m3!: Param;
  private deployEclssPerCrew!: Param;
  private unloadPerShip!: Param;

  override get displayName() {
    return 'Launch campaign';
  }

  get flightsRemaining(): number {
    return this.pending.length;
  }
  get deploymentsPending(): number {
    return this.deployQueue.length;
  }

  loadFlights(flights: Flight[]): void {
    this.pending = [...flights].sort((a, b) => a.sol - b.sol);
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.deploySolarPer100m2 = p.getOrRegister('mission_architecture.deploy_hours_solar_per_100m2', 'Deploy labor: solar per 100 m²', 8, 'crew-eq h',
      'NASA Mars surface array studies: deployment-days for MW-class farms');
    this.deployPerNuclearUnit = p.getOrRegister('power_nuclear.deploy_crew_hours', 'Deploy labor: reactor unit', 40, 'crew-eq h',
      'Estimate: robotic emplacement + cabling');
    this.deployGreenhousePer10m2 = p.getOrRegister('mission_architecture.deploy_hours_greenhouse_per_10m2', 'Deploy labor: greenhouse per 10 m²', 12, 'crew-eq h',
      'Outfitting-heavy installation estimate');
    this.deployHabPer100m3 = p.getOrRegister('mission_architecture.deploy_hours_hab_per_100m3', 'Deploy labor: habitat per 100 m³', 60, 'crew-eq h',
      'Estimate: connect, leak-check, outfit');
    this.deployEclssPerCrew = p.getOrRegister('mission_architecture.deploy_hours_eclss_per_crew', 'Deploy labor: ECLSS per crew capacity', 10, 'crew-eq h',
      'Rack installation + checkout estimate');
    this.unloadPerShip = p.getOrRegister('mission_architecture.unload_hours_per_ship', 'Unload labor per ship', 30, 'crew-eq h',
      'Estimate: 100+ t cargo, crane ops, hauling');
  }

  override preTick(ctx: SimContext): void {
    let hours = 0;
    for (const t of this.deployQueue.slice(0, LaunchCampaign.CONCURRENT))
      hours += Math.min(t.totalHours - t.hoursDone, ctx.dtSols * LaunchCampaign.MAX_HOURS_PER_TASK_PER_SOL);
    if (hours > 0) ctx.labor.request(this, 'construction', hours, LaborPriority.Normal);
  }

  override tick(ctx: SimContext): void {
    // Deployment work first (on the task set preTick requested labor for).
    if (this.deployQueue.length > 0) {
      const grant = ctx.labor.grantedFraction(this, 'construction');
      for (const t of this.deployQueue.slice(0, LaunchCampaign.CONCURRENT)) {
        const step =
          Math.min(t.totalHours - t.hoursDone, ctx.dtSols * LaunchCampaign.MAX_HOURS_PER_TASK_PER_SOL) * grant;
        t.hoursDone += step;
        if (t.hoursDone >= t.totalHours - 1e-6) {
          this.deployQueue.splice(this.deployQueue.indexOf(t), 1);
          t.onComplete(ctx);
          this.log(ctx, 'milestone', `Deployed: ${t.description}`);
        }
      }
    }

    // Landings last: new tasks first request labor next preTick.
    while (this.pending.length > 0 && this.pending[0].sol <= ctx.clock.sol)
      this.processFlight(ctx, this.pending.shift()!);

    this.record(ctx, 'campaign.flights_remaining', 'Flights remaining', '', this.pending.length);
    this.record(ctx, 'campaign.deploy_queue', 'Deployments pending', '', this.deployQueue.length);
  }

  private processFlight(ctx: SimContext, f: Flight): void {
    this.log(ctx, 'milestone', `${f.label}: ${Math.max(f.ships.length, 1)} ship(s) landing`);

    const fleet = this.engine.find(StarshipFleet);
    for (const ship of f.ships) fleet?.land(ctx, ship.name, ship.role, ship.contributesHabitatVolume);

    const c = f.cargo;
    const maint = this.engine.find(MaintenanceSystem);

    this.depositWithCapacity(ctx, 'food', 'food', c.foodKg);
    this.depositWithCapacity(ctx, 'water_potable', 'waterPotable', c.waterKg);
    this.depositWithCapacity(ctx, 'o2_reserve', 'o2', c.o2Kg);
    this.depositWithCapacity(ctx, 'n2_reserve', 'n2', c.n2Kg);
    this.depositWithCapacity(ctx, 'h2_import', 'h2', c.h2Kg);
    for (const lot of c.spares) maint?.addSpares(lot.equipmentClass, lot.units, lot.unitMassKg);

    if (c.robots > 0) {
      const robots = this.engine.find(RobotFleet);
      if (robots) {
        robots.count += c.robots;
        maint?.register(robots, `robots_wave_${ctx.clock.solNumber}`, 'robotics', c.robots, 8000, 4, 15);
      }
    }

    if (f.crewArriving > 0) this.engine.find(CrewModule)?.arrive(ctx, f.crewArriving);

    const shipCount = Math.max(1, f.ships.length);
    this.enqueue(`Unload ${f.label}`, this.unloadPerShip.value * shipCount, () => {});

    if (c.solarAreaM2 > 0)
      this.enqueue(`Solar array ${c.solarAreaM2.toFixed(0)} m²`,
        (c.solarAreaM2 / 100) * this.deploySolarPer100m2.value, (cx) => {
          const farm = this.engine.find(SolarFarm);
          if (farm) farm.arrayAreaM2 += c.solarAreaM2;
          maint?.register(farm!, `solar_strings_${cx.clock.solNumber}`, 'power_electronics',
            Math.max(1, Math.floor(c.solarAreaM2 / 200)), 150000, 2, 20);
        });

    if (c.batteryKwh > 0)
      this.enqueue(`Battery bank ${c.batteryKwh.toFixed(0)} kWh`, c.batteryKwh / 100, () => {
        const bat = this.engine.find(BatteryBank);
        if (bat) bat.nameplateKwh += c.batteryKwh;
      });

    if (c.nuclearUnits > 0)
      this.enqueue(`${c.nuclearUnits} fission unit(s)`,
        c.nuclearUnits * this.deployPerNuclearUnit.value, (cx) => {
          const nuc = this.engine.find(NuclearPlant);
          if (nuc) nuc.units += c.nuclearUnits;
          maint?.register(nuc!, `reactor_bop_${cx.clock.solNumber}`, 'power_electronics', c.nuclearUnits, 87600, 8, 100);
        });

    if (c.isruCapacityKgPerSol > 0)
      this.enqueue(`ISRU plant ${(c.isruCapacityKgPerSol / 1000).toFixed(1)} t/sol`, 1, (cx) => {
        const plant = this.engine.find(IsruPropellantPlant);
        if (plant) plant.capacityKgPerSol += c.isruCapacityKgPerSol;
        const units = Math.max(1, Math.floor(c.isruCapacityKgPerSol / 500));
        maint?.register(plant!, `isru_compressors_${cx.clock.solNumber}`, 'isru_mech', units, 6000, 6, 80, 1.5);
        maint?.register(plant!, `isru_soxe_${cx.clock.solNumber}`, 'isru_stack', units, 12000, 4, 40);
      });

    if (c.iceCapacityKgPerSol > 0)
      this.enqueue(`Ice mining rigs ${(c.iceCapacityKgPerSol / 1000).toFixed(1)} t/sol`,
        c.iceCapacityKgPerSol / 100, (cx) => {
          const mine = this.engine.find(IceMine);
          if (mine) mine.capacityKgPerSol += c.iceCapacityKgPerSol;
          // Rigs ship with water tank farm: ~60 sols of production buffer.
          cx.stores.getOrCreate('water_potable', 'waterPotable', 0).addCapacity(c.iceCapacityKgPerSol * 60);
          const rigs = Math.max(1, Math.floor(c.iceCapacityKgPerSol / 1000));
          maint?.register(mine!, `excavators_${cx.clock.solNumber}`, 'mining_mech', rigs, 3000, 5, 120, 1.6);
        });

    if (c.greenhouseM2 > 0)
      this.enqueue(`Greenhouse ${c.greenhouseM2.toFixed(0)} m²`,
        (c.greenhouseM2 / 10) * this.deployGreenhousePer10m2.value, (cx) => {
          const gh = this.engine.find(Greenhouse);
          if (gh) gh.growingAreaM2 += c.greenhouseM2;
          maint?.register(gh!, `gh_led_pumps_${cx.clock.solNumber}`, 'greenhouse_mech',
            Math.max(1, Math.floor(c.greenhouseM2 / 50)), 20000, 3, 25);
        });

    if (c.habVolumeM3 > 0)
      this.enqueue(`Habitat module ${c.habVolumeM3.toFixed(0)} m³`,
        (c.habVolumeM3 / 100) * this.deployHabPer100m3.value,
        (cx) => this.engine.find(Habitat)?.addVolume(cx, c.habVolumeM3));

    if (c.eclssCrewCapacity > 0)
      this.enqueue(`ECLSS racks for ${c.eclssCrewCapacity} crew`,
        c.eclssCrewCapacity * this.deployEclssPerCrew.value, (cx) => {
          const eclss = this.engine.find(Eclss);
          if (eclss) eclss.designCrew += c.eclssCrewCapacity;
          // ECLSS racks ship with wastewater tankage + O2 accumulator headroom.
          cx.stores.getOrCreate('water_waste', 'waterWaste', 0).addCapacity(c.eclssCrewCapacity * 3.8 * 30);
          cx.stores.getOrCreate('o2_reserve', 'o2', 0).addCapacity(c.eclssCrewCapacity * 250);
          const racks = Math.max(1, Math.floor(c.eclssCrewCapacity / 4));
          maint?.register(eclss!, `oga_${cx.clock.solNumber}`, 'eclss_oru', racks, 8760, 4, 50, 1, 'oga');
          maint?.register(eclss!, `co2_removal_${cx.clock.solNumber}`, 'eclss_oru', racks, 6000, 3, 40, 1, 'co2');
          maint?.register(eclss!, `wrs_${cx.clock.solNumber}`, 'eclss_oru', racks, 7000, 5, 60, 1.3, 'wrs');
        });
  }

  private depositWithCapacity(ctx: SimContext, storeId: string, resource: Parameters<SimContext['stores']['getOrCreate']>[1], kg: number): void {
    if (kg <= 0) return;
    const store = ctx.stores.getOrCreate(storeId, resource, 0);
    store.addCapacity(kg * 1.1);
    store.deposit(kg);
  }

  private enqueue(description: string, hours: number, onComplete: (ctx: SimContext) => void): void {
    this.deployQueue.push({ description, totalHours: Math.max(1, hours), hoursDone: 0, onComplete });
  }

  override get statusLine(): string {
    return `${this.pending.length} flights scheduled, ${this.deployQueue.length} deployments in progress`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Flights remaining', this.pending.length, ''],
      ['Deploy queue', this.deployQueue.length, ''],
    ];
  }
}

export type { CargoManifest };
