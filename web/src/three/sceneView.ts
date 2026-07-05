import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import type { SimulationEngine } from '../sim/engine';
import { SimRandom } from '../sim/rng';
import { Habitat, CrewModule } from '../sim/modules/life-support';
import { SolarFarm, NuclearPlant } from '../sim/modules/power-modules';
import { Greenhouse, IceMine, IsruPropellantPlant } from '../sim/modules/production';
import { StarshipFleet, RobotFleet, LaunchCampaign } from '../sim/modules/logistics';
import { buildTerrain, terrainHeight } from './terrain';
import {
  makeCrate, makeDepotTank, makeGreenhouse, makeHabModule, makeIsruPlant, makeMinePit,
  makeMiningRig, makeMoverInstances, makeReactor, makeSolarInstances, makeStarship,
} from './structures';

// Site plan (meters, base origin at 0,0).
const LANDING = new THREE.Vector3(-210, 0, -40);
const HAB = new THREE.Vector3(0, 0, 0);
const GREENHOUSE_ROW = new THREE.Vector3(10, 0, -70);
const SOLAR_ORIGIN = new THREE.Vector3(90, 0, 30);
const ISRU_SITE = new THREE.Vector3(-20, 0, 95);
const DEPOT_ROW = new THREE.Vector3(-90, 0, 110);
const MINE_SITE = new THREE.Vector3(260, 0, 330);
const REACTOR_FIELD = new THREE.Vector3(-480, 0, 260);

const DAY_SKY = new THREE.Color(0.72, 0.5, 0.33);
const SUNSET_SKY = new THREE.Color(0.42, 0.37, 0.42);
const NIGHT_SKY = new THREE.Color(0.012, 0.008, 0.01);
const STORM_SKY = new THREE.Color(0.4, 0.26, 0.15);

interface Mover {
  from: THREE.Vector3;
  to: THREE.Vector3;
  t: number;
  duration: number;
}

interface ShipVis {
  group: THREE.Group;
  base: THREE.Vector3;
  t: number; // 0..1 landing progress
  departing: boolean;
}

function onGround(x: number, z: number): THREE.Vector3 {
  return new THREE.Vector3(x, terrainHeight(x, z), z);
}

/**
 * Three.js base view: renderer + physically-driven sky + structure reconciler. Reads engine
 * state each frame; never mutates the simulation.
 */
export class SceneView {
  readonly renderer: THREE.WebGLRenderer;
  readonly scene = new THREE.Scene();
  readonly camera: THREE.PerspectiveCamera;
  readonly controls: OrbitControls;

  private sun: THREE.DirectionalLight;
  private hemi: THREE.HemisphereLight;
  private dust: THREE.Points;
  private dustMat: THREE.PointsMaterial;
  private smoothedTau = 0.5;

  /**
   * Visual day/night cycle. OFF by default: at hyper-timelapse speeds the real diurnal
   * cycle strobes unpleasantly, so the *rendering* holds a fixed daytime sun while the
   * simulation physics underneath keep the true cycle (solar output, photoperiods, storms).
   * Slow-moving effects (dust storms) still tint the sky either way.
   */
  dayNightCycle = false;

  private engine: SimulationEngine | null = null;
  private world = new THREE.Group();

  private ships = new Map<string, ShipVis>();
  private habModules: THREE.Group[] = [];
  private greenhouses: Array<{ group: THREE.Group; glass: THREE.MeshStandardMaterial }> = [];
  private solar!: ReturnType<typeof makeSolarInstances>;
  private reactors: THREE.Group[] = [];
  private depotTanks: THREE.Group[] = [];
  private rigs: THREE.Group[] = [];
  private crates: THREE.Mesh[] = [];
  private isruPlant: THREE.Group | null = null;
  private minePit: THREE.Mesh | null = null;

  private crewInst: THREE.InstancedMesh;
  private robotInst: THREE.InstancedMesh;
  private crewMovers: Mover[] = [];
  private robotMovers: Mover[] = [];
  private visRng = new SimRandom(1234, 'scene');
  private tmpMat = new THREE.Matrix4();
  private tmpQuat = new THREE.Quaternion();
  private tmpScale = new THREE.Vector3(1, 1, 1);

  constructor(canvas: HTMLCanvasElement) {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    this.camera = new THREE.PerspectiveCamera(55, 1, 0.5, 6000);
    // Default framing takes in the landing zone, hab cluster, and solar farm.
    this.camera.position.set(-420, 250, -380);

    this.controls = new OrbitControls(this.camera, canvas);
    this.controls.target.set(20, 4, 60);
    this.controls.maxPolarAngle = Math.PI / 2 - 0.02;
    this.controls.minDistance = 15;
    this.controls.maxDistance = 2500;
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.08;
    this.controls.autoRotateSpeed = 0.4;

    this.scene.fog = new THREE.FogExp2(0x3a2a1c, 0.00015);

    // Environment map so metallic surfaces (ship steel, tanks, panels) read correctly.
    const pmrem = new THREE.PMREMGenerator(this.renderer);
    this.scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
    this.scene.environmentIntensity = 0.5;

    this.sun = new THREE.DirectionalLight(0xffffff, 1.2);
    this.sun.castShadow = true;
    this.sun.shadow.mapSize.set(2048, 2048);
    this.sun.shadow.camera.left = -400;
    this.sun.shadow.camera.right = 400;
    this.sun.shadow.camera.top = 400;
    this.sun.shadow.camera.bottom = -400;
    this.sun.shadow.camera.far = 2500;
    this.scene.add(this.sun);
    this.scene.add(this.sun.target);

    this.hemi = new THREE.HemisphereLight(0xcf9a70, 0x5a3822, 0.5);
    this.scene.add(this.hemi);

    this.scene.add(buildTerrain());
    this.scene.add(this.world);

    // Dust storm particles around the camera.
    const dustGeo = new THREE.BufferGeometry();
    const n = 2500;
    const pos = new Float32Array(n * 3);
    for (let i = 0; i < n; i++) {
      pos[i * 3] = (Math.random() - 0.5) * 700;
      pos[i * 3 + 1] = Math.random() * 120;
      pos[i * 3 + 2] = (Math.random() - 0.5) * 700;
    }
    dustGeo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    this.dustMat = new THREE.PointsMaterial({
      color: 0x8c5c33, size: 2.2, transparent: true, opacity: 0, depthWrite: false,
    });
    this.dust = new THREE.Points(dustGeo, this.dustMat);
    this.dust.visible = false;
    this.scene.add(this.dust);

    this.solar = makeSolarInstances(1200);
    this.solar.mesh.castShadow = true;
    this.world.add(this.solar.mesh);

    this.crewInst = makeMoverInstances(24, false);
    this.robotInst = makeMoverInstances(60, true);
    this.world.add(this.crewInst);
    this.world.add(this.robotInst);
  }

  setEngine(engine: SimulationEngine): void {
    this.engine = engine;
    // Clear reconstructed visuals (fresh mission).
    for (const s of this.ships.values()) this.world.remove(s.group);
    this.ships.clear();
    for (const arr of [this.habModules, this.reactors, this.depotTanks, this.rigs, this.crates])
      for (const g of arr) this.world.remove(g as THREE.Object3D);
    for (const gh of this.greenhouses) this.world.remove(gh.group);
    this.habModules = [];
    this.greenhouses = [];
    this.reactors = [];
    this.depotTanks = [];
    this.rigs = [];
    this.crates = [];
    if (this.isruPlant) this.world.remove(this.isruPlant);
    if (this.minePit) this.world.remove(this.minePit);
    this.isruPlant = null;
    this.minePit = null;
    this.solar.mesh.count = 0;
    this.crewMovers = [];
    this.robotMovers = [];
  }

  resize(width: number, height: number): void {
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
  }

  setAutoOrbit(on: boolean): void {
    this.controls.autoRotate = on;
  }

  render(dtReal: number): void {
    if (this.engine) {
      this.syncSky(dtReal);
      this.syncStructures();
      this.syncMovers(dtReal);
      this.animateShips(dtReal);
    }
    this.controls.update();
    this.renderer.render(this.scene, this.camera);
  }

  // ---------------- Sky ----------------

  private syncSky(dtReal: number): void {
    const env = this.engine!.context.env;
    // With the cycle off, hold a fixed mid-morning sun (physics unaffected).
    const el = this.dayNightCycle ? env.sunElevationDeg : 38;
    const az = this.dayNightCycle ? env.sunAzimuthDeg : 205;

    const elR = (el * Math.PI) / 180;
    const azR = (az * Math.PI) / 180;
    const dist = 1800;
    this.sun.position.set(
      Math.cos(elR) * Math.sin(azR) * dist,
      Math.max(20, Math.sin(elR) * dist),
      -Math.cos(elR) * Math.cos(azR) * dist,
    );
    this.sun.target.position.set(0, 0, 0);

    this.smoothedTau += (env.opticalDepthTau - this.smoothedTau) * Math.min(1, dtReal * 2);
    const tau = this.smoothedTau;

    // Steady mode still dims with dust (slow, non-flashing); cycle mode uses real GHI.
    const ghiNorm = this.dayNightCycle
      ? Math.max(0, Math.min(1, env.globalHorizontalWm2 / 600))
      : 0.85 * Math.min(1, Math.exp(-0.35 * Math.max(0, tau - 0.4)));
    this.sun.intensity = 0.06 + 2.4 * ghiNorm;
    this.sun.color.setRGB(1, 0.55 + 0.38 * Math.min(1, el / 25), 0.35 + 0.5 * Math.min(1, el / 25));

    const dayness = Math.max(0, Math.min(1, (el + 4) / 20));
    const sky = NIGHT_SKY.clone().lerp(
      SUNSET_SKY.clone().lerp(DAY_SKY, Math.max(0, Math.min(1, (el - 4) / 22))),
      dayness,
    );
    const stormness = Math.max(0, Math.min(1, (tau - 1.5) / 4));
    sky.lerp(STORM_SKY.clone().multiplyScalar(0.15 + 0.85 * dayness), stormness);

    this.scene.background = sky;
    (this.scene.fog as THREE.FogExp2).color.copy(sky);
    (this.scene.fog as THREE.FogExp2).density =
      0.00012 + 0.0003 * Math.min(1, tau / 6) + 0.0009 * stormness;
    this.hemi.intensity = 0.15 + 0.55 * dayness * (1 - stormness * 0.5);

    this.dust.visible = stormness > 0.05;
    this.dustMat.opacity = 0.35 * stormness;
    if (this.dust.visible) {
      this.dust.position.copy(this.camera.position);
      this.dust.position.y = Math.max(10, this.camera.position.y - 30);
      const pos = this.dust.geometry.attributes.position as THREE.BufferAttribute;
      for (let i = 0; i < pos.count; i++) {
        let x = pos.getX(i) + dtReal * 42;
        if (x > 350) x -= 700;
        pos.setX(i, x);
      }
      pos.needsUpdate = true;
    }
  }

  // ---------------- Structures ----------------

  private syncStructures(): void {
    const engine = this.engine!;
    const fleet = engine.find(StarshipFleet);

    // Ships.
    if (fleet) {
      let pad = 0;
      for (const ship of fleet.ships) {
        const padPos = onGround(LANDING.x + (pad % 3) * 70, LANDING.z + Math.floor(pad / 3) * 80);
        pad++;
        let vis = this.ships.get(ship.name);
        if (!vis && !ship.departed) {
          const group = makeStarship();
          group.position.copy(padPos).y += 320;
          this.world.add(group);
          vis = { group, base: padPos, t: 0, departing: false };
          this.ships.set(ship.name, vis);
        }
        if (vis && ship.departed && !vis.departing) {
          vis.departing = true;
          vis.t = 0;
        }
      }
    }

    // Hab modules: one per ~500 m³ of non-ship volume.
    const hab = engine.find(Habitat);
    const shipVol =
      (fleet?.ships.filter((s) => !s.departed && s.contributesHabitatVolume).length ?? 0) *
      engine.params.v('starship.pressurized_volume_m3') *
      engine.params.v('starship.habitat_usable_fraction');
    const habCount = Math.ceil(Math.max(0, (hab?.pressurizedVolumeM3 ?? 0) - shipVol) / 500);
    while (this.habModules.length < habCount) {
      const i = this.habModules.length;
      const g = makeHabModule();
      g.position.copy(onGround(HAB.x + (i % 3) * 18 - 18, HAB.z + Math.floor(i / 3) * 26));
      this.world.add(g);
      this.habModules.push(g);
    }

    // Greenhouses: one tube per 50 m²; glow when lights draw power.
    const gh = engine.find(Greenhouse);
    const ghCount = Math.ceil((gh?.growingAreaM2 ?? 0) / 50);
    while (this.greenhouses.length < ghCount) {
      const i = this.greenhouses.length;
      const made = makeGreenhouse();
      made.group.position.copy(onGround(GREENHOUSE_ROW.x + (i % 6) * 9, GREENHOUSE_ROW.z - Math.floor(i / 6) * 28));
      made.group.rotation.y = Math.PI / 2;
      this.world.add(made.group);
      this.greenhouses.push(made);
    }
    const lightsKw = engine.history.get('greenhouse.power')?.latest ?? 0;
    // Grow-light glow reads strongest in darkness; in daylight it's a faint tint.
    const visualEl = this.dayNightCycle ? engine.context.env.sunElevationDeg : 38;
    const darkness = Math.max(0, Math.min(1, (8 - visualEl) / 16));
    const glowLevel = lightsKw > 1 ? 0.12 + 0.55 * darkness : 0;
    const glow = new THREE.Color(0.55 * glowLevel, 0.2 * glowLevel, 0.45 * glowLevel);
    for (const g of this.greenhouses) g.glass.emissive.copy(glow);

    // Solar tables (instanced): one per ~186 m²; tint darkens with dust.
    const farm = engine.find(SolarFarm);
    const tableCount = Math.min(1200, Math.ceil((farm?.arrayAreaM2 ?? 0) / 186));
    if (tableCount !== this.solar.mesh.count) {
      for (let i = this.solar.mesh.count; i < tableCount; i++) {
        const p = onGround(SOLAR_ORIGIN.x + (i % 14) * 22, SOLAR_ORIGIN.z + Math.floor(i / 14) * 14);
        this.tmpQuat.setFromEuler(new THREE.Euler(-(18 * Math.PI) / 180, 0, 0));
        this.tmpMat.compose(new THREE.Vector3(p.x, p.y + 2.1, p.z), this.tmpQuat, this.tmpScale);
        this.solar.mesh.setMatrixAt(i, this.tmpMat);
      }
      this.solar.mesh.count = tableCount;
      this.solar.mesh.instanceMatrix.needsUpdate = true;
    }
    if (farm) {
      const dust = farm.dustFraction;
      this.solar.material.color.setRGB(0.08 + 0.37 * dust, 0.09 + 0.23 * dust, 0.16 + 0.04 * dust);
    }

    // Reactors.
    const nuc = engine.find(NuclearPlant);
    while (this.reactors.length < (nuc?.units ?? 0)) {
      const i = this.reactors.length;
      const g = makeReactor();
      g.position.copy(onGround(REACTOR_FIELD.x + (i % 6) * 26, REACTOR_FIELD.z + Math.floor(i / 6) * 26));
      this.world.add(g);
      this.reactors.push(g);
    }

    // ISRU plant + depot tanks.
    const isru = engine.find(IsruPropellantPlant);
    if (!this.isruPlant && isru && isru.capacityKgPerSol > 0) {
      this.isruPlant = makeIsruPlant();
      this.isruPlant.position.copy(onGround(ISRU_SITE.x, ISRU_SITE.z));
      this.world.add(this.isruPlant);
    }
    const depotT =
      ((engine.stores.get('depot_ch4')?.amountKg ?? 0) + (engine.stores.get('depot_lox')?.amountKg ?? 0)) / 1000;
    const tanks = Math.ceil(depotT / 250);
    while (this.depotTanks.length < tanks) {
      const i = this.depotTanks.length;
      const g = makeDepotTank();
      g.position.copy(onGround(DEPOT_ROW.x - (i % 5) * 14, DEPOT_ROW.z + Math.floor(i / 5) * 12));
      this.world.add(g);
      this.depotTanks.push(g);
    }

    // Mining rigs + pit.
    const mine = engine.find(IceMine);
    const rigCount = Math.ceil((mine?.capacityKgPerSol ?? 0) / 1500);
    if (rigCount > 0 && !this.minePit) {
      this.minePit = makeMinePit();
      this.minePit.position.copy(onGround(MINE_SITE.x, MINE_SITE.z));
      this.world.add(this.minePit);
    }
    while (this.rigs.length < rigCount) {
      const i = this.rigs.length;
      const g = makeMiningRig();
      g.position.copy(onGround(MINE_SITE.x + (i % 3) * 16 - 16, MINE_SITE.z + Math.floor(i / 3) * 16 - 8));
      this.world.add(g);
      this.rigs.push(g);
    }

    // Cargo crates = deployment backlog.
    const campaign = engine.find(LaunchCampaign);
    const crateCount = Math.min(24, (campaign?.deploymentsPending ?? 0) * 2);
    while (this.crates.length < crateCount) {
      const i = this.crates.length;
      const c = makeCrate();
      c.position.copy(onGround(LANDING.x + 40 + (i % 4) * 5, LANDING.z - 30 + Math.floor(i / 4) * 5));
      this.world.add(c);
      this.crates.push(c);
    }
    while (this.crates.length > crateCount) {
      const c = this.crates.pop()!;
      this.world.remove(c);
    }
  }

  private animateShips(dtReal: number): void {
    for (const [name, vis] of this.ships) {
      if (vis.t >= 1 && !vis.departing) continue;
      vis.t = Math.min(1, vis.t + dtReal / 2.5);
      const h = Math.pow(vis.departing ? vis.t : 1 - vis.t, 2.2) * 320;
      vis.group.position.set(vis.base.x, vis.base.y + h, vis.base.z);
      if (vis.departing && vis.t >= 1) {
        this.world.remove(vis.group);
        this.ships.delete(name);
      }
    }
  }

  // ---------------- Movers ----------------

  private syncMovers(dtReal: number): void {
    const engine = this.engine!;
    const crewCount = Math.min(engine.find(CrewModule)?.count ?? 0, 24);
    const robotCount = Math.min(engine.find(RobotFleet)?.count ?? 0, 60);
    const night = this.dayNightCycle && engine.context.env.sunElevationDeg < -2;

    this.syncMoverList(this.crewMovers, crewCount);
    this.syncMoverList(this.robotMovers, robotCount);
    this.stepMovers(this.crewMovers, this.crewInst, dtReal, night);
    this.stepMovers(this.robotMovers, this.robotInst, dtReal, false);
  }

  private randomSite(): THREE.Vector3 {
    const sites = [HAB, ISRU_SITE, SOLAR_ORIGIN, LANDING];
    if (this.greenhouses.length > 0) sites.push(GREENHOUSE_ROW);
    if (this.rigs.length > 0) sites.push(MINE_SITE);
    const s = sites[this.visRng.nextInt(sites.length)];
    return onGround(s.x + this.visRng.range(-18, 18), s.z + this.visRng.range(-18, 18));
  }

  private syncMoverList(list: Mover[], target: number): void {
    while (list.length < target) {
      const start = onGround(HAB.x + this.visRng.range(-20, 20), HAB.z + this.visRng.range(-20, 20));
      list.push({ from: start.clone(), to: start.clone(), t: 1, duration: 1 });
    }
    while (list.length > target) list.pop();
  }

  private stepMovers(list: Mover[], inst: THREE.InstancedMesh, dtReal: number, stayHome: boolean): void {
    for (let i = 0; i < list.length; i++) {
      const m = list[i];
      m.t += dtReal / m.duration;
      if (m.t >= 1) {
        m.from.copy(m.to);
        m.to = stayHome
          ? onGround(HAB.x + this.visRng.range(-20, 20), HAB.z + this.visRng.range(-20, 20))
          : this.randomSite();
        m.t = 0;
        m.duration = this.visRng.range(6, 16);
      }
      const s = m.t * m.t * (3 - 2 * m.t);
      const x = m.from.x + (m.to.x - m.from.x) * s;
      const z = m.from.z + (m.to.z - m.from.z) * s;
      this.tmpMat.makeTranslation(x, terrainHeight(x, z) + 0.95, z);
      inst.setMatrixAt(i, this.tmpMat);
    }
    inst.count = list.length;
    inst.instanceMatrix.needsUpdate = true;
  }
}
