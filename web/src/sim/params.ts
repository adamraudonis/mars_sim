export type Confidence = 'high' | 'medium' | 'low' | 'speculative';

/**
 * A single tunable quantity with provenance. The simulation NEVER hard-codes numbers;
 * modules resolve parameters by id so every value is user-tunable and carries its citation
 * into the UI. Override layers: base (sourced) → scenario → distilled → user.
 */
export class Param {
  id: string;
  name: string;
  unit: string;
  domain: string;
  baseValue: number;
  rangeMin: number | null = null;
  rangeMax: number | null = null;
  source: string;
  sourceUrl: string | null = null;
  confidence: Confidence = 'medium';
  notes: string | null = null;

  scenarioOverride: number | null = null;
  distilledOverride: number | null = null;
  userOverride: number | null = null;

  /** True while only a scenario override exists and no module/database defined this id. */
  isPlaceholder = false;

  constructor(id: string, name: string, baseValue: number, unit: string, source: string) {
    this.id = id;
    this.name = name;
    this.baseValue = baseValue;
    this.unit = unit;
    this.source = source;
    this.domain = id.includes('.') ? id.slice(0, id.indexOf('.')) : 'misc';
  }

  get value(): number {
    return this.userOverride ?? this.distilledOverride ?? this.scenarioOverride ?? this.baseValue;
  }

  get isOverridden(): boolean {
    return (
      this.userOverride !== null || this.distilledOverride !== null || this.scenarioOverride !== null
    );
  }
}

export interface ParamDatabaseJson {
  version: string;
  domains: Record<
    string,
    {
      title?: string;
      modeling_notes?: string;
      parameters: Array<{
        id: string;
        name?: string;
        value: number;
        unit?: string;
        range_min?: number | null;
        range_max?: number | null;
        source?: string;
        source_url?: string | null;
        confidence?: string;
        notes?: string | null;
      }>;
    }
  >;
}

/** Loads the sourced parameter database and manages override layers. */
export class ParameterRegistry {
  private params = new Map<string, Param>();
  readonly all: Param[] = [];
  private changeListeners: Array<(p: Param) => void> = [];

  has(id: string): boolean {
    return this.params.has(id);
  }

  find(id: string): Param | undefined {
    return this.params.get(id);
  }

  onChanged(listener: (p: Param) => void): () => void {
    this.changeListeners.push(listener);
    return () => {
      this.changeListeners = this.changeListeners.filter((l) => l !== listener);
    };
  }

  private emit(p: Param): void {
    for (const l of this.changeListeners) l(p);
  }

  /** Resolve a parameter; if absent, register the code fallback (with provenance). */
  getOrRegister(
    id: string,
    name: string,
    fallbackValue: number,
    unit: string,
    source: string,
    confidence: Confidence = 'medium',
  ): Param {
    let p = this.params.get(id);
    if (p) {
      if (p.isPlaceholder) {
        p.name = name;
        p.unit = unit;
        p.baseValue = fallbackValue;
        p.source = source;
        p.confidence = confidence;
        p.isPlaceholder = false;
      }
      return p;
    }
    p = new Param(id, name, fallbackValue, unit, source);
    p.confidence = confidence;
    this.params.set(id, p);
    this.all.push(p);
    return p;
  }

  /** Value of an already-registered parameter. */
  v(id: string): number {
    const p = this.params.get(id);
    if (!p) throw new Error(`parameter '${id}' not registered`);
    return p.value;
  }

  setUserOverride(id: string, value: number | null): void {
    const p = this.params.get(id);
    if (!p) throw new Error(`parameter '${id}' not registered`);
    p.userOverride = value;
    this.emit(p);
  }

  setScenarioOverride(id: string, value: number): void {
    let p = this.params.get(id);
    if (!p) {
      // Modules register lazily in init; hold the override in a placeholder so it is
      // never silently dropped. getOrRegister upgrades it with real metadata later.
      p = new Param(id, id, value, '', '(scenario override)');
      p.isPlaceholder = true;
      this.params.set(id, p);
      this.all.push(p);
    }
    p.scenarioOverride = value;
    this.emit(p);
  }

  setDistilledOverride(id: string, value: number | null): void {
    const p = this.params.get(id);
    if (!p) throw new Error(`parameter '${id}' not registered`);
    p.distilledOverride = value;
    this.emit(p);
  }

  clearScenarioOverrides(): void {
    for (const p of this.all) p.scenarioOverride = null;
  }

  /** Load research/parameters_master.json (values become the sourced baseline). */
  loadDatabase(db: ParamDatabaseJson): number {
    let loaded = 0;
    for (const [domainKey, domain] of Object.entries(db.domains)) {
      for (const pj of domain.parameters) {
        if (!pj.id) continue;
        let p = this.params.get(pj.id);
        if (!p) {
          p = new Param(pj.id, pj.name ?? pj.id, pj.value, pj.unit ?? '', pj.source ?? '(unsourced)');
          this.params.set(pj.id, p);
          this.all.push(p);
        }
        p.domain = domainKey;
        p.isPlaceholder = false;
        p.name = pj.name ?? pj.id;
        p.baseValue = pj.value;
        p.unit = pj.unit ?? '';
        p.rangeMin = pj.range_min ?? null;
        p.rangeMax = pj.range_max ?? null;
        p.source = pj.source ?? '(unsourced)';
        p.sourceUrl = pj.source_url ?? null;
        p.notes = pj.notes ?? null;
        p.confidence = (['high', 'medium', 'low', 'speculative'].includes(pj.confidence ?? '')
          ? pj.confidence
          : 'medium') as Confidence;
        loaded++;
      }
    }
    return loaded;
  }
}
