import { SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { LaborPriority } from '../labor';
import type { Store } from '../store';

/** One repairable component population inside a module (e.g. "OGA pumps ×4"). */
export interface ComponentGroup {
  id: string;
  owner: SimModule;
  equipmentClass: string; // spares pooled by class
  functionTag: string | null; // sub-function within the owner (null = whole module)
  total: number;
  working: number;
  mtbfHours: number;
  repairCrewHours: number;
  spareMassKg: number;
  weibullShape: number; // 1 = exponential; >1 = wear-out
  ageHours: number;
}

interface RepairJob {
  group: ComponentGroup;
  hoursRemaining: number;
  spareConsumed: boolean;
  queuedSol: number;
}

function capacityContribution(g: ComponentGroup): number {
  return g.total > 0 ? g.working / g.total : 1;
}

/** Γ(1 + 1/k) via Lanczos-lite (scale-from-mean for Weibull). */
function gammaMeanFactor(k: number): number {
  const x = 1 + 1 / k;
  const g = [1.000000000190015, 76.18009172947146, -86.50532032941677, 24.01409824083091,
    -1.231739572450155, 1.208650973866179e-3, -5.395239384953e-6];
  let sum = g[0];
  for (let i = 1; i < 7; i++) sum += g[i] / (x + i - 1);
  const t = x + 4.5;
  return Math.exp((x - 0.5) * Math.log(t) - t + Math.log(sum * 2.5066282746310005));
}

/**
 * Stochastic failure + repair + sparing engine (the Owens & de Weck problem). Failures
 * sample per operating hour (Poisson, so multi-unit fleets can shed several units a step);
 * repairs consume maintenance labor + class-pooled spares; cannibalization merges dead
 * units. A failed unit without a spare stays down — with no resupply for ~26 months,
 * spares mass is a survival parameter.
 */
export class MaintenanceSystem extends SimModule {
  readonly groups: ComponentGroup[] = [];
  private queue: RepairJob[] = [];
  private sparesByClass = new Map<string, number>();

  private kFactor!: Param;
  private preventivePerGroupSol!: Param;
  private cannibalize!: Param;
  private sparesMass!: Store;

  private requestedCorrectiveHours = 0;

  override get displayName() {
    return 'Maintenance & spares';
  }

  get queueLength(): number {
    return this.queue.length;
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.kFactor = p.getOrRegister('reliability.k_factor', 'Failure rate uncertainty multiplier', 2, '',
      'ISS experience: observed rates above predictions (Stromgren; Owens & de Weck)');
    this.preventivePerGroupSol = p.getOrRegister('reliability.preventive_hours_per_group_sol', 'Preventive maintenance per component group', 0.05, 'crew-eq h/sol',
      'ISS: ~2-10 h/wk preventive across systems, scaled per group');
    this.cannibalize = p.getOrRegister('reliability.allow_cannibalization', 'Cannibalize failed units for spares (0/1)', 1, 'bool',
      'Ops policy');
    this.sparesMass = ctx.stores.getOrCreate('spares', 'sparesMass', 1e9);
  }

  register(owner: SimModule, id: string, equipmentClass: string, count: number, mtbfHours: number,
    repairCrewHours: number, spareMassKg: number, weibullShape = 1, functionTag: string | null = null): ComponentGroup {
    const g: ComponentGroup = {
      id, owner, equipmentClass, functionTag, total: count, working: count,
      mtbfHours, repairCrewHours, spareMassKg, weibullShape, ageHours: 0,
    };
    this.groups.push(g);
    return g;
  }

  addSpares(equipmentClass: string, units: number, unitMassKg: number): void {
    this.sparesByClass.set(equipmentClass, (this.sparesByClass.get(equipmentClass) ?? 0) + units);
    this.sparesMass.deposit(units * unitMassKg);
  }

  spareCount(equipmentClass: string): number {
    return this.sparesByClass.get(equipmentClass) ?? 0;
  }

  /** Availability of one sub-function of a module (min over its tagged + module-wide groups). */
  functionCapacity(owner: SimModule, functionTag: string): number {
    let cap = 1;
    for (const g of this.groups) {
      if (g.owner !== owner) continue;
      if (g.functionTag === null || g.functionTag === functionTag)
        cap = Math.min(cap, capacityContribution(g));
    }
    return cap;
  }

  override preTick(ctx: SimContext): void {
    const preventive = this.groups.length * this.preventivePerGroupSol.value * ctx.dtSols;
    let corrective = 0;
    for (const j of this.queue) {
      // Jobs stuck waiting for a spare (no cannibalization pair) can't be worked.
      if (!j.spareConsumed && this.spareCount(j.group.equipmentClass) === 0) {
        const pairs = this.queue.filter((q) => q.group === j.group && !q.spareConsumed).length;
        if (pairs < 2) continue;
      }
      corrective += Math.min(j.hoursRemaining, ctx.dtSols * 8);
    }
    this.requestedCorrectiveHours = corrective;
    if (preventive + corrective > 0)
      ctx.labor.request(this, 'maintenance', preventive + corrective, LaborPriority.High);
  }

  override tick(ctx: SimContext): void {
    const laborGrant = ctx.labor.grantedFraction(this, 'maintenance');

    // --- Sample failures (per operating hour, Poisson) ---
    for (const g of this.groups) {
      if (g.working <= 0 || g.mtbfHours <= 0) continue;
      const duty = Math.max(0, Math.min(1, g.owner.failureDutyCycle));
      if (duty <= 0) continue;
      g.ageHours += ctx.dtHours * duty;
      let hazardPerHour = (duty * this.kFactor.value) / g.mtbfHours;
      if (g.weibullShape > 1.001) {
        const lambda = g.mtbfHours / gammaMeanFactor(g.weibullShape);
        hazardPerHour =
          duty * this.kFactor.value * (g.weibullShape / lambda) *
          Math.pow(Math.max(1e-6, g.ageHours) / lambda, g.weibullShape - 1);
      }
      const lambdaStep = hazardPerHour * ctx.dtHours * g.working;
      const failures = Math.min(g.working, this.rng.poisson(lambdaStep));
      for (let n = 0; n < failures; n++) {
        g.working--;
        this.queue.push({ group: g, hoursRemaining: g.repairCrewHours, spareConsumed: false, queuedSol: ctx.clock.sol });
        this.log(ctx, 'warning',
          `FAILURE: ${g.id} (${g.owner.displayName}) — ${g.working}/${g.total} units up, spare ${this.spareCount(g.equipmentClass) > 0 ? 'available' : 'NOT available'}`);
      }
    }

    // --- Work the repair queue (labor budget from what preTick actually requested) ---
    let hoursAvailable = laborGrant * this.requestedCorrectiveHours;
    for (const job of [...this.queue]) {
      if (hoursAvailable <= 0) break;

      if (!job.spareConsumed) {
        if (this.spareCount(job.group.equipmentClass) > 0) {
          this.sparesByClass.set(job.group.equipmentClass, this.spareCount(job.group.equipmentClass) - 1);
          this.sparesMass.withdraw(job.group.spareMassKg);
          job.spareConsumed = true;
        } else if (
          this.cannibalize.value > 0.5 &&
          job.group.total >= 2 &&
          this.queue.filter((q) => q.group === job.group && !q.spareConsumed).length >= 2
        ) {
          // Strip one failed unit for parts, repair the other.
          const other = this.queue.find((q) => q.group === job.group && q !== job && !q.spareConsumed)!;
          this.queue.splice(this.queue.indexOf(other), 1);
          job.group.total--;
          job.spareConsumed = true;
          this.log(ctx, 'info', `Cannibalized a failed ${job.group.id} unit for parts`);
        } else continue;
      }

      const work = Math.min(job.hoursRemaining, hoursAvailable);
      job.hoursRemaining -= work;
      hoursAvailable -= work;
      if (job.hoursRemaining <= 1e-6) {
        this.queue.splice(this.queue.indexOf(job), 1);
        job.group.working = Math.min(job.group.total, job.group.working + 1);
        // Renewal: a fresh unit dilutes the population age.
        if (job.group.total > 0) job.group.ageHours *= 1 - 1 / job.group.total;
      }
    }

    // --- Propagate availability to owners ---
    const byOwner = new Map<SimModule, ComponentGroup[]>();
    for (const g of this.groups) {
      const arr = byOwner.get(g.owner);
      if (arr) arr.push(g);
      else byOwner.set(g.owner, [g]);
    }
    for (const [owner, groups] of byOwner) {
      const tagged = groups.some((g) => g.functionTag !== null);
      let cap: number;
      if (tagged) {
        let sum = 0;
        for (const g of groups) sum += capacityContribution(g);
        cap = groups.length > 0 ? sum / groups.length : 1;
      } else {
        cap = 1;
        for (const g of groups) cap = Math.min(cap, capacityContribution(g));
      }
      owner.capacityFactor = cap;
      let worst = 1;
      for (const g of groups) worst = Math.min(worst, capacityContribution(g));
      owner.health = worst >= 0.999 ? 'nominal' : worst > 0.4 ? 'degraded' : 'failed';
    }

    this.record(ctx, 'maint.queue', 'Open repair jobs', '', this.queue.length);
    this.record(ctx, 'maint.spares_mass', 'Spares inventory', 'kg', this.sparesMass.amountKg);
    this.record(ctx, 'maint.awaiting_spares', 'Jobs stuck without spares', '',
      this.queue.filter((j) => !j.spareConsumed).length);
  }

  override get statusLine(): string {
    return `${this.queue.length} open jobs (${this.queue.filter((j) => !j.spareConsumed).length} awaiting spares)`;
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Component groups', this.groups.length, ''],
      ['Open repairs', this.queue.length, ''],
      ['Spares mass', this.sparesMass.amountKg / 1000, 't'],
    ];
  }
}
