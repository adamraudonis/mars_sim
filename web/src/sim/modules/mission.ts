import { SimModule, type SimContext } from '../module';
import type { Param } from '../params';
import { CrewModule } from './life-support';

/**
 * Mission-level status and failure accounting. The power bus already triages loads by
 * priority (Critical shed last) with a battery reserve for life support — this module makes
 * the consequence explicit: it accumulates unserved Critical (life-support) power as both
 * energy (kWh) and brown-out time (sols), and declares MISSION FAILURE once sustained
 * life-support power loss passes a tunable budget. It also flags loss-of-crew.
 *
 * This is complementary to the crew health model (starved ECLSS → bad air → deaths): the
 * power budget usually trips first and gives an unambiguous, chartable failure signal.
 */
export class MissionControl extends SimModule {
  cumulativeUnmetCriticalKwh = 0;
  brownoutSols = 0; // total time critical power was unmet beyond the threshold
  failed = false;
  failReason = '';

  private brownoutThresholdKw!: Param;
  private failBudgetHours!: Param;
  private crew?: CrewModule;

  override get displayName() {
    return 'Mission control';
  }

  override init(ctx: SimContext): void {
    const p = ctx.params;
    this.brownoutThresholdKw = p.getOrRegister('mission.brownout_threshold_kw', 'Critical-power shortfall counted as a brown-out', 1, 'kW',
      'Ops policy: unmet Critical (life-support) load above this counts against the failure budget');
    this.failBudgetHours = p.getOrRegister('mission.critical_brownout_fail_hours', 'Cumulative life-support brown-out that fails the mission', 72, 'h',
      'Ops policy: total unmet-critical-power hours the base can survive before mission failure (tunable)');
    this.crew = this.engine.find(CrewModule);
  }

  get budgetFraction(): number {
    const budgetH = Math.max(1e-6, this.failBudgetHours.value);
    return Math.min(1, (this.brownoutSols * 24.6597) / budgetH);
  }

  override tick(ctx: SimContext): void {
    const unmetCrit = ctx.power.unmetCriticalKw;
    this.cumulativeUnmetCriticalKwh += unmetCrit * ctx.dtHours;
    if (unmetCrit > this.brownoutThresholdKw.value) {
      const wasZero = this.brownoutSols === 0;
      this.brownoutSols += ctx.dtSols;
      if (wasZero)
        this.log(ctx, 'warning', `Life-support power shortfall began (${unmetCrit.toFixed(0)} kW unmet critical)`);
    }

    const brownoutHours = this.brownoutSols * 24.6597;
    if (!this.failed) {
      if (brownoutHours >= this.failBudgetHours.value && this.crew && this.crew.count > 0) {
        this.failed = true;
        this.failReason = `Life support lost power for ${brownoutHours.toFixed(0)} h (budget ${this.failBudgetHours.value.toFixed(0)} h)`;
        this.log(ctx, 'critical', `MISSION FAILURE — ${this.failReason}`);
      } else if (this.crew && this.crew.fatalities > 0 && this.crew.count === 0) {
        this.failed = true;
        this.failReason = 'Loss of all crew';
        this.log(ctx, 'critical', 'MISSION FAILURE — loss of all crew');
      }
    }

    this.health = this.failed ? 'failed' : this.brownoutSols > 0 ? 'degraded' : 'nominal';

    this.record(ctx, 'power.unmet_critical', 'Unmet life-support power', 'kW', unmetCrit);
    this.record(ctx, 'mission.brownout_hours', 'Cumulative life-support brown-out', 'h', brownoutHours);
    this.record(ctx, 'mission.failed', 'Mission failed', '', this.failed ? 1 : 0);
  }

  override get statusLine(): string {
    if (this.failed) return `MISSION FAILURE — ${this.failReason}`;
    if (this.brownoutSols > 0)
      return `Life-support power margin at risk (${(this.brownoutSols * 24.6597).toFixed(0)} of ${this.failBudgetHours.value.toFixed(0)} h budget)`;
    return 'Nominal — life support fully powered';
  }

  override get keyFigures(): Array<[string, number, string]> {
    return [
      ['Life-support brown-out', this.brownoutSols * 24.6597, 'h'],
      ['Failure budget', this.failBudgetHours.value, 'h'],
      ['Unmet critical (cum.)', this.cumulativeUnmetCriticalKwh, 'kWh'],
      ['Mission failed', this.failed ? 1 : 0, ''],
    ];
  }
}
