/**
 * The observable control logic of the simulation — every threshold-driven rule, in one
 * place (the "Simulink" view). Each law names the signal it watches, the governing
 * parameter threshold (editable in the Parameters tab), and the action taken. LogicPage
 * evaluates them live so you can see which rules are firing right now.
 */
export interface ControlLaw {
  when: string;
  then: string;
  signalSeriesId?: string; // live signal to read from the recording
  signalLabel?: string;
  signalUnit?: string;
  compare?: 'gt' | 'lt';
  thresholdParam?: string; // param id supplying the threshold value
  thresholdConst?: number;
  thresholdUnit?: string;
  priority?: string; // load/labor priority where relevant
}

export interface ControlSystem {
  system: string;
  summary: string;
  laws: ControlLaw[];
}

export const CONTROL_SYSTEMS: ControlSystem[] = [
  {
    system: 'Power bus — load triage',
    summary:
      'Every load carries a priority. Generation + battery are allocated top-down; the lowest priority is shed first, pro-rata within a tier.',
    laws: [
      { when: 'demand exceeds supply', then: 'shed loads bottom-up: Opportunistic → Low → Normal → High; Critical (life support) is served last of all', priority: 'ordering' },
      {
        when: 'battery state of charge drops below the critical reserve', then: 'only Critical + High loads may keep discharging; ISRU, ice mining and greenhouse lighting are cut until the sun/reactor recovers',
        signalSeriesId: 'power.battery_soc', signalLabel: 'battery SoC', signalUnit: '%', compare: 'lt',
        thresholdParam: 'power_solar.battery_reserve_soc_for_critical', thresholdUnit: 'fraction',
      },
      { when: 'generation exceeds demand', then: 'surplus charges the battery (round-trip efficiency applied); any excess beyond charge rate is curtailed' },
    ],
  },
  {
    system: 'Habitat atmosphere',
    summary: 'The cabin is a well-mixed gas volume. These reflexes hold it breathable.',
    laws: [
      {
        when: 'cabin ppCO₂ exceeds the emergency limit', then: 'EMERGENCY open-loop purge — vent cabin air and repressurise from O₂/N₂ reserves (Apollo-13 style), spending reserve mass to dump CO₂',
        signalSeriesId: 'hab.ppco2', signalLabel: 'ppCO₂', signalUnit: 'kPa', compare: 'gt',
        thresholdParam: 'eclss.emergency_purge_ppco2_kpa', thresholdUnit: 'kPa',
      },
      {
        when: 'cabin ppO₂ falls below 95% of setpoint', then: 'top up O₂ from the bottled reserve until the OGA catches up',
        signalSeriesId: 'hab.ppo2', signalLabel: 'ppO₂', signalUnit: 'kPa', compare: 'lt',
        thresholdParam: 'eclss.ppo2_setpoint', thresholdUnit: 'kPa',
      },
      {
        when: 'total pressure below setpoint', then: 'inject N₂ makeup from the reserve to hold cabin pressure',
        signalSeriesId: 'hab.pressure', signalLabel: 'cabin pressure', signalUnit: 'kPa', compare: 'lt',
        thresholdParam: 'eclss.cabin_total_pressure', thresholdUnit: 'kPa',
      },
    ],
  },
  {
    system: 'ECLSS — air & water regeneration',
    summary: 'Rate-based hardware, each function degraded by its own failures. Power is Critical priority.',
    laws: [
      { when: 'ppO₂ below setpoint OR O₂ reserve under target', then: 'run the OGA: electrolyse potable water → O₂ to cabin (+ H₂ for Sabatier), up to rated capacity', thresholdParam: 'eclss.o2_reserve_target_sols', thresholdUnit: 'sols', priority: 'Critical' },
      { when: 'cabin holds CO₂', then: 'CDRA scrubs it to the CO₂ buffer at up to the rated per-crew capacity', thresholdParam: 'eclss.co2_removal_capacity_kg_cm_day', thresholdUnit: 'kg/CM-day', priority: 'Critical' },
      { when: 'CO₂ buffer + H₂ available', then: 'Sabatier: CO₂ + 4H₂ → CH₄ + 2H₂O, throughput-capped to the reactor rate; water recycled to electrolysis', thresholdParam: 'eclss.sabatier_conversion' },
      { when: 'wastewater present', then: 'recover it to potable at the demonstrated closure fraction; brine is lost', signalSeriesId: 'store.water_waste', signalLabel: 'wastewater', signalUnit: 'kg', thresholdParam: 'eclss.water_recovery_fraction', thresholdUnit: 'fraction' },
    ],
  },
  {
    system: 'Crew health',
    summary: 'Sustained deprivation drains a health index; at zero the crew begin to die.',
    laws: [
      { when: 'ppO₂ below the hypoxia threshold (or O₂ delivery short)', then: 'severe health drain (minutes matter)', signalSeriesId: 'hab.ppo2', signalLabel: 'ppO₂', signalUnit: 'kPa', compare: 'lt', thresholdParam: 'human_factors.hypoxia_ppo2_kpa', thresholdUnit: 'kPa' },
      { when: 'ppCO₂ above the hypercapnia threshold', then: 'cognitive + health degradation', signalSeriesId: 'hab.ppco2', signalLabel: 'ppCO₂', signalUnit: 'kPa', compare: 'gt', thresholdParam: 'human_factors.hypercapnia_ppco2_kpa', thresholdUnit: 'kPa' },
      { when: 'water reserve under ~0.7 days of need', then: 'dehydration health drain', signalSeriesId: 'crew.water_days', signalLabel: 'water reserve', signalUnit: 'sols', compare: 'lt', thresholdConst: 1 },
      { when: 'food reserve running out', then: 'starvation drain (slower onset)', signalSeriesId: 'crew.food_days', signalLabel: 'food reserve', signalUnit: 'sols', compare: 'lt', thresholdConst: 10 },
    ],
  },
  {
    system: 'Solar array — dust management',
    summary: 'Dust accumulates and derates output; cleaning costs labor.',
    laws: [
      { when: 'panel dust obscuration exceeds the cleaning threshold', then: 'request cleaning labor (Normal); if dust is over twice the threshold, escalate to High priority so it doesn’t threaten the power supply', signalSeriesId: 'solar.dust', signalLabel: 'panel dust', signalUnit: '%', compare: 'gt', thresholdParam: 'power_solar.cleaning_threshold', thresholdUnit: 'fraction' },
    ],
  },
  {
    system: 'ISRU & ice mining',
    summary: 'Production is gated by power, labor and feedstock; storage back-pressures the mine.',
    laws: [
      { when: 'water tank near full', then: 'throttle the ice mine so product isn’t vented (no phantom power draw)', signalSeriesId: 'icemine.production', signalLabel: 'water mined', signalUnit: 'kg/sol' },
      { when: 'architecture = H₂-import', then: 'Sabatier feeds from Earth H₂ and O₂ comes from MOXIE-style SOXE — zero water mining, more plant energy', thresholdParam: 'isru_atmosphere.architecture', thresholdUnit: 'mode' },
      { when: 'plant commissioning incomplete', then: 'consume construction labor until commissioned before producing propellant' },
    ],
  },
  {
    system: 'Propellant depot (cryo)',
    summary: 'Zero-boiloff needs continuous cryocooler power (High priority).',
    laws: [
      { when: 'cryocooler power is shed', then: 'the unpowered fraction of the tank farm boils off at the passive rate — power loss directly costs propellant', thresholdParam: 'starship.boiloff_fraction_per_sol', thresholdUnit: 'fraction/sol', priority: 'High' },
    ],
  },
  {
    system: 'Starship return',
    summary: 'The whole ISRU campaign exists to fill these tanks before the window.',
    laws: [
      { when: 'return propellant reaches 100%', then: 'mark the crew ship fuelled for Earth return', signalSeriesId: 'fleet.return_prop', signalLabel: 'return readiness', signalUnit: '%', compare: 'gt', thresholdConst: 100 },
      { when: 'Earth-return window opens AND ship is fuelled', then: 'depart — withdraw the return propellant from the depot' },
    ],
  },
  {
    system: 'Maintenance & spares',
    summary: 'Stochastic failures per operating hour; repairs need spares + labor.',
    laws: [
      { when: 'a component fails and a class spare exists', then: 'consume one spare + maintenance labor to restore it', signalSeriesId: 'maint.queue', signalLabel: 'open repairs', signalUnit: 'jobs' },
      { when: 'no spare, but ≥2 units of a group have failed', then: 'cannibalise one dead unit for parts to fix another', signalSeriesId: 'maint.awaiting_spares', signalLabel: 'jobs without spares', signalUnit: 'jobs' },
      { when: 'units of a module are down', then: 'reduce that module’s capacity factor (and, per sub-function, its ECLSS air/water throughput)', thresholdParam: 'reliability.k_factor' },
    ],
  },
  {
    system: 'Robot fleet',
    summary: 'Robots supply labor but must charge.',
    laws: [
      { when: 'charging power was shed last step', then: 'reduce robot labor supply proportionally (energy ↔ labor coupling)', signalSeriesId: 'robots.charge_kw', signalLabel: 'charging draw', signalUnit: 'kW' },
    ],
  },
  {
    system: 'Mission control — failure budget',
    summary: 'Makes “unmet critical power = mission failure” explicit.',
    laws: [
      { when: 'Critical (life-support) power is unmet beyond a small margin', then: 'count it against the brown-out budget', signalSeriesId: 'power.unmet_critical', signalLabel: 'unmet critical', signalUnit: 'kW', compare: 'gt', thresholdParam: 'mission.brownout_threshold_kw', thresholdUnit: 'kW' },
      { when: 'cumulative life-support brown-out exceeds the budget', then: 'declare MISSION FAILURE', signalSeriesId: 'mission.brownout_hours', signalLabel: 'brown-out so far', signalUnit: 'h', compare: 'gt', thresholdParam: 'mission.critical_brownout_fail_hours', thresholdUnit: 'h' },
    ],
  },
  {
    system: 'Mars environment — dust storms',
    summary: 'Stochastic, seasonal — the main external shock to a solar base.',
    laws: [
      { when: 'in the dusty season (Ls 180–330)', then: 'each sol has a small chance of a regional or global dust storm that raises optical depth τ and darkens the sky for weeks', signalSeriesId: 'env.tau', signalLabel: 'optical depth τ', signalUnit: '', compare: 'gt', thresholdParam: 'mars_environment.global_storm_tau' },
    ],
  },
];
