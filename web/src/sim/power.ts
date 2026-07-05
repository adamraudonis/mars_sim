import type { SimModule } from './module';

/** Load-shed order: lower value = shed last. */
export enum LoadPriority {
  Critical = 0, // life support, habitat thermal
  High = 1, // comms, robot charging, cryocoolers
  Normal = 2, // ISRU production, ice mining
  Low = 3, // greenhouse lighting, science
  Opportunistic = 4,
}

/**
 * Aggregated battery bank state. Round-trip efficiency applied on charge; a hard SoC floor
 * protects cycle life, and a *critical reserve* SoC below which only Critical/High loads
 * may discharge — what keeps a night-running ISRU plant from starving life support.
 */
export class BatteryState {
  capacityKwh = 0;
  energyKwh = 0;
  maxChargeRateKw = Infinity;
  maxDischargeRateKw = Infinity;
  chargeEfficiency = 0.95;
  minSocFraction = 0.15;
  criticalReserveSoc = 0.5;

  get soc(): number {
    return this.capacityKwh > 0 ? this.energyKwh / this.capacityKwh : 0;
  }

  get usableKwh(): number {
    return Math.max(0, this.energyKwh - this.minSocFraction * this.capacityKwh);
  }

  maxDischargeKw(dtHours: number): number {
    return dtHours <= 0 ? 0 : Math.min(this.maxDischargeRateKw, this.usableKwh / dtHours);
  }

  discharge(kw: number, dtHours: number): number {
    const actual = Math.min(kw, this.maxDischargeKw(dtHours));
    this.energyKwh -= actual * dtHours;
    return actual;
  }

  charge(kw: number, dtHours: number): number {
    if (dtHours <= 0 || this.capacityKwh <= 0) return 0;
    const headroom = Math.max(0, this.capacityKwh - this.energyKwh);
    const maxGridKw = Math.min(this.maxChargeRateKw, headroom / (dtHours * this.chargeEfficiency));
    const actual = Math.min(kw, maxGridKw);
    this.energyKwh += actual * dtHours * this.chargeEfficiency;
    return actual;
  }
}

interface Demand {
  module: SimModule;
  kw: number;
  priority: LoadPriority;
}

/**
 * Instantaneous electrical bus, resolved every step: generators offer kW, consumers demand
 * kW at a priority, the battery bridges. Demands served strictly by priority (pro-rata
 * within a band); each consumer gets a granted fraction it must respect this step.
 */
export class PowerBus {
  readonly battery = new BatteryState();

  private demands: Demand[] = [];
  private granted = new Map<SimModule, number>();
  private offeredKw = 0;

  generationOfferedKw = 0;
  generationUsedKw = 0;
  demandKw = 0;
  servedKw = 0;
  unmetKw = 0;
  curtailedKw = 0;
  batteryFlowKw = 0; // + discharging, − charging

  beginStep(): void {
    this.demands.length = 0;
    this.granted.clear();
    this.offeredKw = 0;
  }

  offer(kw: number): void {
    if (kw > 0) this.offeredKw += kw;
  }

  request(module: SimModule, kw: number, priority: LoadPriority): void {
    if (kw <= 0) return;
    this.demands.push({ module, kw, priority });
  }

  /** Fraction of its request a module actually received this step [0,1]. */
  grantedFraction(module: SimModule): number {
    return this.granted.get(module) ?? 1;
  }

  resolve(dtHours: number): void {
    this.generationOfferedKw = this.offeredKw;

    let totalDemand = 0;
    for (const d of this.demands) totalDemand += d.kw;
    this.demandKw = totalDemand;

    const battFullKw = this.battery.maxDischargeKw(dtHours);
    const aboveReserveKwh = Math.max(
      0,
      this.battery.energyKwh - this.battery.criticalReserveSoc * this.battery.capacityKwh,
    );
    const battLowPrioKw =
      dtHours > 0
        ? Math.min(this.battery.maxDischargeRateKw, Math.min(battFullKw, aboveReserveKwh / dtHours))
        : 0;

    // Stable sort (JS Array.sort is stable): priority, insertion order preserved.
    this.demands.sort((a, b) => a.priority - b.priority);

    let servedTotal = 0;
    let genLeft = this.offeredKw;
    let battUsedKw = 0;
    let i = 0;
    while (i < this.demands.length) {
      let j = i;
      let groupKw = 0;
      while (j < this.demands.length && this.demands[j].priority === this.demands[i].priority) {
        groupKw += this.demands[j].kw;
        j++;
      }
      const battCapKw = this.demands[i].priority <= LoadPriority.High ? battFullKw : battLowPrioKw;
      const available = genLeft + Math.max(0, battCapKw - battUsedKw);
      const fraction = groupKw <= available ? 1 : groupKw > 0 ? available / groupKw : 0;
      let servedGroup = 0;
      for (let k = i; k < j; k++) {
        const f = Math.max(0, Math.min(1, fraction));
        const d = this.demands[k];
        const prev = this.granted.get(d.module);
        this.granted.set(d.module, prev === undefined ? f : Math.min(prev, f));
        servedGroup += d.kw * f;
      }
      const fromGen = Math.min(servedGroup, genLeft);
      genLeft -= fromGen;
      battUsedKw += servedGroup - fromGen;
      servedTotal += servedGroup;
      i = j;
    }

    this.servedKw = servedTotal;
    this.unmetKw = Math.max(0, totalDemand - servedTotal);

    if (battUsedKw > 1e-12) {
      const discharged = this.battery.discharge(battUsedKw, dtHours);
      this.batteryFlowKw = discharged;
      this.generationUsedKw = this.offeredKw - genLeft;
      this.curtailedKw = 0;
    } else {
      const absorbed = this.battery.charge(genLeft, dtHours);
      this.batteryFlowKw = -absorbed;
      this.generationUsedKw = this.offeredKw - genLeft + absorbed;
      this.curtailedKw = Math.max(0, genLeft - absorbed);
    }
  }
}
