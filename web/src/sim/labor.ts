import type { SimModule } from './module';

export type TaskType =
  | 'maintenance'
  | 'isruOps'
  | 'agriculture'
  | 'construction'
  | 'logistics'
  | 'science';

export enum LaborPriority {
  Critical = 0,
  High = 1,
  Normal = 2,
  Low = 3,
}

interface Demand {
  module: SimModule;
  task: TaskType;
  crewEqHours: number;
  priority: LaborPriority;
}

/**
 * Per-step labor market. Crew and robots supply work-hours; modules demand crew-equivalent
 * hours per task type. Robots substitute at a per-task effectiveness ratio. Allocation is
 * by priority, pro-rata within a band (uniform fraction found by bisection).
 */
export class LaborPool {
  private demands: Demand[] = [];
  private granted = new Map<string, number>(); // key: moduleId|task
  private robotEffectiveness = new Map<TaskType, number>();

  crewHoursAvailable = 0;
  robotHoursAvailable = 0;
  crewHoursUsed = 0;
  robotHoursUsed = 0;
  demandCrewEqHours = 0;
  servedCrewEqHours = 0;
  unmetCrewEqHours = 0;

  beginStep(): void {
    this.demands.length = 0;
    this.granted.clear();
    this.crewHoursAvailable = 0;
    this.robotHoursAvailable = 0;
    this.crewHoursUsed = 0;
    this.robotHoursUsed = 0;
  }

  supplyCrewHours(hours: number): void {
    if (hours > 0) this.crewHoursAvailable += hours;
  }

  supplyRobotHours(hours: number): void {
    if (hours > 0) this.robotHoursAvailable += hours;
  }

  setRobotEffectiveness(task: TaskType, ratio: number): void {
    this.robotEffectiveness.set(task, ratio);
  }

  getRobotEffectiveness(task: TaskType): number {
    return this.robotEffectiveness.get(task) ?? 0;
  }

  request(module: SimModule, task: TaskType, crewEqHours: number, priority: LaborPriority): void {
    if (crewEqHours <= 0) return;
    this.demands.push({ module, task, crewEqHours, priority });
  }

  grantedFraction(module: SimModule, task: TaskType): number {
    return this.granted.get(`${module.id}|${task}`) ?? 1;
  }

  resolve(): void {
    this.demandCrewEqHours = 0;
    for (const d of this.demands) this.demandCrewEqHours += d.crewEqHours;

    let crewLeft = this.crewHoursAvailable;
    let robotLeft = this.robotHoursAvailable;
    let served = 0;

    this.demands.sort((a, b) => a.priority - b.priority); // stable in JS

    let i = 0;
    while (i < this.demands.length) {
      let j = i;
      while (j < this.demands.length && this.demands[j].priority === this.demands[i].priority) j++;

      const f = this.bandFraction(i, j, crewLeft, robotLeft);
      for (let k = i; k < j; k++) {
        const d = this.demands[k];
        const target = d.crewEqHours * f;
        let got = 0;
        const eff = this.getRobotEffectiveness(d.task);
        if (eff > 0 && robotLeft > 0) {
          const robotHours = Math.min(target / eff, robotLeft);
          robotLeft -= robotHours;
          this.robotHoursUsed += robotHours;
          got += robotHours * eff;
        }
        if (got < target - 1e-12 && crewLeft > 0) {
          const crewHours = Math.min(target - got, crewLeft);
          crewLeft -= crewHours;
          this.crewHoursUsed += crewHours;
          got += crewHours;
        }
        const fr = d.crewEqHours > 0 ? Math.max(0, Math.min(1, got / d.crewEqHours)) : 1;
        const key = `${d.module.id}|${d.task}`;
        const prev = this.granted.get(key);
        this.granted.set(key, prev === undefined ? fr : Math.min(prev, fr));
        served += got;
      }
      i = j;
    }

    this.servedCrewEqHours = served;
    this.unmetCrewEqHours = Math.max(0, this.demandCrewEqHours - served);
  }

  /** Largest uniform fraction of every band demand the remaining supply can serve. */
  private bandFraction(i: number, j: number, crewLeft: number, robotLeft: number): number {
    const feasible = (f: number): boolean => {
      let crew = crewLeft;
      let robot = robotLeft;
      for (let k = i; k < j; k++) {
        const d = this.demands[k];
        let target = d.crewEqHours * f;
        const eff = this.getRobotEffectiveness(d.task);
        if (eff > 0 && robot > 0) {
          const rh = Math.min(target / eff, robot);
          robot -= rh;
          target -= rh * eff;
        }
        if (target > 1e-12) {
          const ch = Math.min(target, crew);
          crew -= ch;
          target -= ch;
        }
        if (target > 1e-9) return false;
      }
      return true;
    };

    if (feasible(1)) return 1;
    let lo = 0;
    let hi = 1;
    for (let iter = 0; iter < 24; iter++) {
      const mid = (lo + hi) / 2;
      if (feasible(mid)) lo = mid;
      else hi = mid;
    }
    return lo;
  }
}
