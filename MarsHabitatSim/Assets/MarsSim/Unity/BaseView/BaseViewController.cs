using System.Collections.Generic;
using System.Linq;
using MarsSim.Core;
using MarsSim.Core.Modules;
using UnityEngine;

namespace MarsSim.UnityApp.BaseView
{
    /// <summary>
    /// Keeps the 3D scene in sync with engine state each frame: structures appear as the
    /// campaign deploys them, ships land and depart with animation, panels visibly dust over,
    /// greenhouses glow through the night, and crew/robots shuttle between worksites.
    /// Pure reconciliation — nothing here mutates the simulation.
    /// </summary>
    public sealed class BaseViewController : MonoBehaviour
    {
        // Site plan (meters, base origin at 0,0).
        private static readonly Vector3 LandingZone = new(-210, 0, -40);
        private static readonly Vector3 HabCluster = new(0, 0, 0);
        private static readonly Vector3 GreenhouseRow = new(10, 0, -70);
        private static readonly Vector3 SolarFieldOrigin = new(90, 0, 30);
        private static readonly Vector3 IsruSite = new(-20, 0, 95);
        private static readonly Vector3 DepotRow = new(-90, 0, 110);
        private static readonly Vector3 MineSite = new(260, 0, 330);
        private static readonly Vector3 ReactorField = new(-480, 0, 260);

        private readonly Dictionary<string, GameObject> _ships = new();
        private readonly List<GameObject> _habModules = new();
        private readonly List<(GameObject go, Material glow)> _greenhouses = new();
        private readonly List<GameObject> _solarTables = new();
        private readonly List<GameObject> _reactors = new();
        private readonly List<GameObject> _depotTanks = new();
        private readonly List<GameObject> _rigs = new();
        private readonly List<GameObject> _crates = new();
        private GameObject _isruPlant;
        private GameObject _minePit;
        private Material _panelMat;

        private readonly List<Mover> _people = new();
        private readonly List<Mover> _robots = new();

        private sealed class Mover
        {
            public GameObject Go;
            public Vector3 From, To;
            public float T, Duration;
        }

        private sealed class ShipAnim { public float T; public bool Departing; }
        private readonly Dictionary<GameObject, ShipAnim> _shipAnims = new();

        private void Start()
        {
            _panelMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "solar_panel",
                color = new Color(0.08f, 0.09f, 0.16f),
            };
            _panelMat.SetFloat("_Metallic", 0.6f);
            _panelMat.SetFloat("_Smoothness", 0.8f);

            if (SimRunner.Instance != null)
                SimRunner.Instance.EngineRebuilt += Rebuild;
        }

        private void Rebuild()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _ships.Clear(); _habModules.Clear(); _greenhouses.Clear(); _solarTables.Clear();
            _reactors.Clear(); _depotTanks.Clear(); _rigs.Clear(); _crates.Clear();
            _people.Clear(); _robots.Clear(); _shipAnims.Clear();
            _isruPlant = null; _minePit = null;
        }

        private void Update()
        {
            var engine = SimRunner.Instance?.Engine;
            if (engine == null) return;

            SyncShips(engine);
            SyncCounted(engine);
            SyncPeople(engine);
            AnimateShips();
        }

        private static Vector3 OnGround(Vector3 pos)
            => new(pos.x, TerrainBuilder.HeightAt(pos.x, pos.z), pos.z);

        // ---------------- Ships ----------------

        private void SyncShips(SimulationEngine engine)
        {
            var fleet = engine.Find<StarshipFleet>();
            if (fleet == null) return;

            int pad = 0;
            foreach (var ship in fleet.Ships)
            {
                Vector3 padPos = OnGround(LandingZone + new Vector3((pad % 3) * 70, 0, (pad / 3) * 80));
                pad++;

                if (!_ships.TryGetValue(ship.Name, out var go) && !ship.Departed)
                {
                    go = ProceduralMeshes.Starship(transform, padPos + Vector3.up * 320f, ship.Name);
                    _ships[ship.Name] = go;
                    _shipAnims[go] = new ShipAnim { T = 0, Departing = false };
                }

                if (go != null && ship.Departed && _shipAnims.TryGetValue(go, out var anim) && !anim.Departing)
                {
                    anim.T = 0;
                    anim.Departing = true;
                }
            }
        }

        private void AnimateShips()
        {
            foreach (var kv in _shipAnims.ToList())
            {
                var go = kv.Key;
                var anim = kv.Value;
                if (go == null) { _shipAnims.Remove(go); continue; }
                if (anim.T >= 1 && !anim.Departing) continue;

                anim.T = Mathf.Min(1, anim.T + Time.deltaTime / 2.5f);
                var basePos = OnGround(new Vector3(go.transform.position.x, 0, go.transform.position.z));
                float height = Mathf.Pow(anim.Departing ? anim.T : 1 - anim.T, 2.2f) * 320f;
                go.transform.position = basePos + Vector3.up * height;

                if (anim.Departing && anim.T >= 1)
                {
                    _ships.Remove(_ships.FirstOrDefault(p => p.Value == go).Key ?? "");
                    _shipAnims.Remove(go);
                    Destroy(go);
                }
            }
        }

        // ---------------- Counted structures ----------------

        private void SyncCounted(SimulationEngine engine)
        {
            // Habitat modules: one cylinder per ~500 m3 of non-ship volume.
            var hab = engine.Find<Habitat>();
            var fleet = engine.Find<StarshipFleet>();
            double shipVol = (fleet?.Ships.Count(s => !s.Departed && s.ContributesHabitatVolume) ?? 0)
                             * engine.Params.V("starship.pressurized_volume_m3")
                             * engine.Params.V("starship.habitat_usable_fraction");
            int habCount = Mathf.CeilToInt((float)(((hab?.PressurizedVolumeM3 ?? 0) - shipVol) / 500.0));
            while (_habModules.Count < habCount)
            {
                int i = _habModules.Count;
                var pos = OnGround(HabCluster + new Vector3((i % 3) * 18 - 18, 0, (i / 3) * 26));
                _habModules.Add(ProceduralMeshes.HabModule(transform, pos, i + 1));
            }

            // Greenhouses: one tube per 50 m².
            var gh = engine.Find<Greenhouse>();
            int ghCount = Mathf.CeilToInt((float)((gh?.GrowingAreaM2 ?? 0) / 50.0));
            while (_greenhouses.Count < ghCount)
            {
                int i = _greenhouses.Count;
                var pos = OnGround(GreenhouseRow + new Vector3((i % 6) * 9, 0, -(i / 6) * 28));
                var go = ProceduralMeshes.GreenhouseTube(transform, pos, i + 1, out var glow);
                _greenhouses.Add((go, glow));
            }
            // Glow when lights draw power.
            float lightsKw = engine.History.Get("greenhouse.power")?.Latest ?? 0;
            var emission = lightsKw > 1 ? new Color(0.85f, 0.45f, 0.75f) * 1.6f : Color.black;
            foreach (var (_, glow) in _greenhouses) glow.SetColor("_EmissionColor", emission);

            // Solar tables: one per ~186 m² (19.4 x 9.6 panel).
            var farm = engine.Find<SolarFarm>();
            int tableCount = Mathf.CeilToInt((float)((farm?.ArrayAreaM2 ?? 0) / 186.0));
            while (_solarTables.Count < tableCount)
            {
                int i = _solarTables.Count;
                var pos = OnGround(SolarFieldOrigin + new Vector3((i % 14) * 22, 0, (i / 14) * 14));
                _solarTables.Add(ProceduralMeshes.SolarTable(transform, pos, _panelMat));
            }
            if (farm != null)
            {
                float dust = (float)farm.DustFraction;
                _panelMat.color = Color.Lerp(new Color(0.08f, 0.09f, 0.16f), new Color(0.45f, 0.32f, 0.2f), dust);
            }

            // Reactors.
            var nuc = engine.Find<NuclearPlant>();
            while (_reactors.Count < (nuc?.Units ?? 0))
            {
                int i = _reactors.Count;
                var pos = OnGround(ReactorField + new Vector3((i % 6) * 26, 0, (i / 6) * 26));
                _reactors.Add(ProceduralMeshes.Reactor(transform, pos, i + 1));
            }

            // ISRU plant + depot tanks.
            var isru = engine.Find<IsruPropellantPlant>();
            if (_isruPlant == null && isru is { CapacityKgPerSol: > 0 })
                _isruPlant = ProceduralMeshes.IsruPlant(transform, OnGround(IsruSite));

            double depotT = (engine.Stores.Get("depot_ch4")?.AmountKg ?? 0) / 1000.0
                            + (engine.Stores.Get("depot_lox")?.AmountKg ?? 0) / 1000.0;
            int tanks = Mathf.CeilToInt((float)(depotT / 250.0));
            while (_depotTanks.Count < tanks)
            {
                int i = _depotTanks.Count;
                var pos = OnGround(DepotRow + new Vector3(-(i % 5) * 14, 0, (i / 5) * 12));
                _depotTanks.Add(ProceduralMeshes.DepotTank(transform, pos, i + 1));
            }

            // Mining rigs + pit.
            var mine = engine.Find<IceMine>();
            int rigCount = Mathf.CeilToInt((float)((mine?.CapacityKgPerSol ?? 0) / 1500.0));
            if (rigCount > 0 && _minePit == null)
            {
                _minePit = ProceduralMeshes.Primitive(PrimitiveType.Cylinder, "Mine pit", transform,
                    OnGround(MineSite) + Vector3.down * 1.2f, new Vector3(60, 1.5f, 60),
                    ProceduralMeshes.Mat("pit", new Color(0.32f, 0.2f, 0.14f), 0f, 0.05f));
            }
            while (_rigs.Count < rigCount)
            {
                int i = _rigs.Count;
                var pos = OnGround(MineSite + new Vector3((i % 3) * 16 - 16, 0, (i / 3) * 16 - 8));
                _rigs.Add(ProceduralMeshes.MiningRig(transform, pos, i + 1));
            }

            // Cargo crates represent the deployment backlog.
            var campaign = engine.Find<LaunchCampaign>();
            int crateCount = Mathf.Min(24, (campaign?.DeploymentsPending ?? 0) * 2);
            while (_crates.Count < crateCount)
            {
                int i = _crates.Count;
                var pos = OnGround(LandingZone + new Vector3(40 + (i % 4) * 5, 0, -30 + (i / 4) * 5));
                _crates.Add(ProceduralMeshes.CargoCrate(transform, pos));
            }
            while (_crates.Count > crateCount && _crates.Count > 0)
            {
                Destroy(_crates[^1]);
                _crates.RemoveAt(_crates.Count - 1);
            }
        }

        // ---------------- People & robots ----------------

        private void SyncPeople(SimulationEngine engine)
        {
            int crewCount = Mathf.Min(engine.Find<Crew>()?.Count ?? 0, 24);
            int robotCount = Mathf.Min(engine.Find<RobotFleet>()?.Count ?? 0, 40);

            SyncMovers(_people, crewCount, false);
            SyncMovers(_robots, robotCount, true);

            bool night = engine.Context.Env.SunElevationDeg < -2;
            foreach (var m in _people) StepMover(m, night ? HabCluster : RandomSite(), night);
            foreach (var m in _robots) StepMover(m, RandomSite(), false);
        }

        private void SyncMovers(List<Mover> list, int target, bool robot)
        {
            while (list.Count < target)
            {
                var start = OnGround(HabCluster + new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f)));
                list.Add(new Mover
                {
                    Go = ProceduralMeshes.Person(transform, start, robot),
                    From = start, To = start, T = 1, Duration = 1,
                });
            }
            while (list.Count > target && list.Count > 0)
            {
                Destroy(list[^1].Go);
                list.RemoveAt(list.Count - 1);
            }
        }

        private Vector3 RandomSite()
        {
            var sites = new List<Vector3> { HabCluster, IsruSite, SolarFieldOrigin, LandingZone };
            if (_greenhouses.Count > 0) sites.Add(GreenhouseRow);
            if (_rigs.Count > 0) sites.Add(MineSite);
            var s = sites[Random.Range(0, sites.Count)];
            return s + new Vector3(Random.Range(-18f, 18f), 0, Random.Range(-18f, 18f));
        }

        private void StepMover(Mover m, Vector3 nextTarget, bool stayPut)
        {
            m.T += Time.deltaTime / m.Duration;
            if (m.T >= 1)
            {
                m.From = m.To;
                m.To = stayPut ? m.From : OnGround(nextTarget);
                m.T = 0;
                m.Duration = Random.Range(6f, 16f);
            }
            var pos = Vector3.Lerp(m.From, m.To, Mathf.SmoothStep(0, 1, m.T));
            pos.y = TerrainBuilder.HeightAt(pos.x, pos.z);
            m.Go.transform.position = pos;
        }
    }
}
