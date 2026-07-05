import { SimulationEngine } from './engine';
import type { ParameterRegistry } from './params';
import type { Scenario } from './scenario';
import { MarsEnvironment } from './modules/environment';
import { Habitat, CrewModule, Eclss } from './modules/life-support';
import { SolarFarm, NuclearPlant, BatteryBank } from './modules/power-modules';
import { Greenhouse, IsruPropellantPlant, IceMine, PropellantDepot } from './modules/production';
import { StarshipFleet, RobotFleet, LaunchCampaign } from './modules/logistics';
import { MaintenanceSystem } from './modules/maintenance';

/**
 * Assembles a ready-to-run engine from a scenario + parameter registry. The module set is
 * the full composable stack; anything absent from the manifest stays at zero capacity and
 * costs nothing, so one builder serves every architecture.
 */
export function buildSimulation(scenario: Scenario, params: ParameterRegistry): SimulationEngine {
  const engine = new SimulationEngine(params, scenario.epochUtcMs, scenario.timestepSeconds, scenario.seed);

  params.clearScenarioOverrides();
  for (const [id, value] of Object.entries(scenario.overrides)) params.setScenarioOverride(id, value);

  const env = engine.add(new MarsEnvironment(), 'environment');
  env.latitudeDeg = scenario.latitudeDeg;
  env.elevationKm = scenario.elevationKm;

  engine.add(new Habitat(), 'habitat');
  engine.add(new CrewModule(), 'crew');
  engine.add(new Eclss(), 'eclss');
  engine.add(new SolarFarm(), 'solar');
  engine.add(new NuclearPlant(), 'nuclear');
  engine.add(new BatteryBank(), 'battery');
  engine.add(new Greenhouse(), 'greenhouse');
  engine.add(new IsruPropellantPlant(), 'isru_plant');
  engine.add(new IceMine(), 'ice_mine');
  engine.add(new PropellantDepot(), 'depot');
  const fleet = engine.add(new StarshipFleet(), 'fleet');
  engine.add(new RobotFleet(), 'robots');
  engine.add(new MaintenanceSystem(), 'maintenance');
  const campaign = engine.add(new LaunchCampaign(), 'campaign');

  campaign.loadFlights(scenario.flights);
  fleet.returnWindowSols.push(...scenario.returnWindowSols);

  for (const [id, fidelity] of Object.entries(scenario.fidelity)) {
    const m = engine.findById(id);
    if (m) m.fidelity = fidelity;
  }

  engine.initialize();
  return engine;
}
