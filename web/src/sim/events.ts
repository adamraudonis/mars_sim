export type EventSeverity = 'info' | 'milestone' | 'warning' | 'critical';

export interface SimEvent {
  sol: number;
  severity: EventSeverity;
  source: string;
  message: string;
}

/** Discrete event stream: landings, failures, storms, milestones, alarms. */
export class EventLog {
  readonly events: SimEvent[] = [];
  private listeners: Array<(e: SimEvent) => void> = [];

  log(sol: number, severity: EventSeverity, source: string, message: string): void {
    const e = { sol, severity, source, message };
    this.events.push(e);
    for (const l of this.listeners) l(e);
  }

  onEvent(listener: (e: SimEvent) => void): () => void {
    this.listeners.push(listener);
    return () => {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  get count(): number {
    return this.events.length;
  }
}
