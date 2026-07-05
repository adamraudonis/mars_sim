/** Bulk commodities tracked by mass. Cabin atmosphere is NOT a store (see Habitat). */
export type Resource =
  | 'o2'
  | 'n2'
  | 'co2'
  | 'h2'
  | 'ch4'
  | 'lox'
  | 'waterPotable'
  | 'waterWaste'
  | 'waterFeedstock'
  | 'food'
  | 'biomass'
  | 'regolith'
  | 'sparesMass';

export const resourceDisplayName: Record<Resource, string> = {
  o2: 'Oxygen reserve',
  n2: 'Nitrogen',
  co2: 'CO₂ buffer',
  h2: 'Hydrogen',
  ch4: 'Methane (propellant)',
  lox: 'LOX (propellant)',
  waterPotable: 'Potable water',
  waterWaste: 'Wastewater',
  waterFeedstock: 'Raw water (ISRU)',
  food: 'Food',
  biomass: 'Biomass',
  regolith: 'Regolith',
  sparesMass: 'Spares',
};

/**
 * A mass buffer with capacity. All withdrawals/deposits are clamped and ledgered so the
 * engine can audit conservation exactly: deposited − withdrawn === amount.
 */
export class Store {
  readonly id: string;
  readonly resource: Resource;
  displayName: string;

  capacityKg: number;
  amountKg: number;

  totalDepositedKg: number;
  totalWithdrawnKg = 0;
  totalVentedKg = 0; // deposits rejected for lack of capacity (never entered the store)

  constructor(id: string, resource: Resource, capacityKg: number, initialKg = 0) {
    this.id = id;
    this.resource = resource;
    this.capacityKg = capacityKg;
    this.amountKg = Math.min(initialKg, capacityKg);
    this.totalDepositedKg = this.amountKg;
    this.displayName = resourceDisplayName[resource];
  }

  get freeKg(): number {
    return Math.max(0, this.capacityKg - this.amountKg);
  }

  get fraction(): number {
    return this.capacityKg > 0 ? this.amountKg / this.capacityKg : 0;
  }

  /** Deposit up to kg; returns amount actually stored. Excess is vented (tracked). */
  deposit(kg: number): number {
    if (kg <= 0) return 0;
    const stored = Math.min(kg, this.freeKg);
    this.amountKg += stored;
    this.totalDepositedKg += stored;
    this.totalVentedKg += kg - stored;
    return stored;
  }

  /** Withdraw up to kg; returns amount actually obtained. */
  withdraw(kg: number): number {
    if (kg <= 0) return 0;
    const got = Math.min(kg, this.amountKg);
    this.amountKg -= got;
    this.totalWithdrawnKg += got;
    return got;
  }

  addCapacity(kg: number): void {
    this.capacityKg += kg;
  }
}

/** Registry of stores keyed by id, with per-resource aggregates. */
export class StoreSet {
  private byId = new Map<string, Store>();
  readonly all: Store[] = [];

  create(id: string, r: Resource, capacityKg: number, initialKg = 0): Store {
    if (this.byId.has(id)) throw new Error(`duplicate store id '${id}'`);
    const s = new Store(id, r, capacityKg, initialKg);
    this.byId.set(id, s);
    this.all.push(s);
    return s;
  }

  get(id: string): Store | undefined {
    return this.byId.get(id);
  }

  getOrCreate(id: string, r: Resource, capacityKg: number, initialKg = 0): Store {
    return this.byId.get(id) ?? this.create(id, r, capacityKg, initialKg);
  }

  totalAmount(r: Resource): number {
    let sum = 0;
    for (const s of this.all) if (s.resource === r) sum += s.amountKg;
    return sum;
  }
}
