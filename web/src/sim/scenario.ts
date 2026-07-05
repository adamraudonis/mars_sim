import { Fidelity } from './module';
import { Units } from './units';
import type { ShipRole } from './modules/logistics';

export interface SparesLot {
  equipmentClass: string;
  units: number;
  unitMassKg: number;
}

export interface CargoManifest {
  solarAreaM2: number;
  batteryKwh: number;
  nuclearUnits: number;
  isruCapacityKgPerSol: number;
  iceCapacityKgPerSol: number;
  greenhouseM2: number;
  habVolumeM3: number;
  eclssCrewCapacity: number;
  foodKg: number;
  waterKg: number;
  o2Kg: number;
  n2Kg: number;
  h2Kg: number;
  robots: number;
  spares: SparesLot[];
}

export interface ShipArrival {
  name: string;
  role: ShipRole;
  contributesHabitatVolume: boolean;
}

export interface Flight {
  sol: number;
  label: string;
  ships: ShipArrival[];
  crewArriving: number;
  cargo: CargoManifest;
}

export interface Scenario {
  name: string;
  description: string;
  latitudeDeg: number;
  elevationKm: number;
  epochUtcMs: number;
  timestepSeconds: number;
  seed: number;
  durationSols: number;
  fidelity: Record<string, Fidelity>;
  overrides: Record<string, number>;
  flights: Flight[];
  returnWindowSols: number[];
}

/** Parse a scenario JSON file (same schema as the Unity reference). */
export function parseScenario(j: any): Scenario {
  const fidelity: Record<string, Fidelity> = {};
  for (const [key, v] of Object.entries(j.fidelity ?? {})) {
    fidelity[key] = v === 'L0' ? Fidelity.L0 : v === 'L2' ? Fidelity.L2 : Fidelity.L1;
  }

  const flights: Flight[] = (j.flights ?? []).map((f: any) => ({
    sol: f.sol ?? 0,
    label: f.label ?? `Flight @ sol ${f.sol ?? 0}`,
    crewArriving: f.crew ?? 0,
    ships: (f.ships ?? []).map((s: any) => ({
      name: s.name ?? 'Ship',
      role: (s.role === 'CrewTransport' ? 'crewTransport' : s.role === 'TankerDepot' ? 'tankerDepot' : 'cargo') as ShipRole,
      contributesHabitatVolume: !!s.habitat,
    })),
    cargo: {
      solarAreaM2: f.cargo?.solar_area_m2 ?? 0,
      batteryKwh: f.cargo?.battery_kwh ?? 0,
      nuclearUnits: f.cargo?.nuclear_units ?? 0,
      isruCapacityKgPerSol: f.cargo?.isru_capacity_kg_sol ?? 0,
      iceCapacityKgPerSol: f.cargo?.ice_capacity_kg_sol ?? 0,
      greenhouseM2: f.cargo?.greenhouse_m2 ?? 0,
      habVolumeM3: f.cargo?.hab_volume_m3 ?? 0,
      eclssCrewCapacity: f.cargo?.eclss_crew_capacity ?? 0,
      foodKg: f.cargo?.food_kg ?? 0,
      waterKg: f.cargo?.water_kg ?? 0,
      o2Kg: f.cargo?.o2_kg ?? 0,
      n2Kg: f.cargo?.n2_kg ?? 0,
      h2Kg: f.cargo?.h2_kg ?? 0,
      robots: f.cargo?.robots ?? 0,
      spares: (f.cargo?.spares ?? []).map((sp: any) => ({
        equipmentClass: sp.class ?? 'general',
        units: sp.units ?? 0,
        unitMassKg: sp.unit_kg ?? 50,
      })),
    },
  }));
  flights.sort((a, b) => a.sol - b.sol);

  return {
    name: j.name ?? 'Unnamed',
    description: j.description ?? '',
    latitudeDeg: j.site?.latitude_deg ?? 40,
    elevationKm: j.site?.elevation_km ?? -3,
    epochUtcMs: j.epoch_utc ? Date.parse(`${j.epoch_utc}T00:00:00Z`) : Date.UTC(2031, 8, 1),
    timestepSeconds: j.timestep_seconds ?? Units.solSeconds / 24,
    seed: j.seed ?? 42,
    durationSols: j.duration_sols ?? 2000,
    fidelity,
    overrides: j.overrides ?? {},
    flights,
    returnWindowSols: j.return_windows_sols ?? [],
  };
}
