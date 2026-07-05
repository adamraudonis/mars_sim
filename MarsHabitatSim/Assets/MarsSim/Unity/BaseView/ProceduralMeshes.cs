using System.Collections.Generic;
using UnityEngine;

namespace MarsSim.UnityApp.BaseView
{
    /// <summary>
    /// Zero-asset geometry + material helpers for every structure in the base view.
    /// Primitives are Unity built-ins; composites are assembled from parented primitives so
    /// each structure stays a single draggable GameObject.
    /// </summary>
    public static class ProceduralMeshes
    {
        private static readonly Dictionary<string, Material> MaterialCache = new();

        public static Material Mat(string name, Color color, float metallic = 0f, float smoothness = 0.3f,
            Color? emission = null)
        {
            if (MaterialCache.TryGetValue(name, out var cached)) return cached;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            MaterialCache[name] = m;
            return m;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 localScale, Material mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // ---------------- Structures ----------------

        public static GameObject Starship(Transform parent, Vector3 pos, string name)
        {
            var root = new GameObject($"Starship {name}");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            var steel = Mat("steel", new Color(0.72f, 0.73f, 0.76f), 0.95f, 0.75f);
            var dark = Mat("tile", new Color(0.12f, 0.12f, 0.14f), 0.2f, 0.35f);

            // Hull: 9 m diameter, ~46 m barrel + nose. (Cylinder primitive is 2 units tall.)
            Primitive(PrimitiveType.Cylinder, "hull", root.transform, new Vector3(0, 23f, 0),
                new Vector3(9f, 23f, 9f), steel);
            // Nosecone approximated by stacked shrinking sections.
            Primitive(PrimitiveType.Cylinder, "nose1", root.transform, new Vector3(0, 48.5f, 0),
                new Vector3(7.4f, 2.6f, 7.4f), steel);
            Primitive(PrimitiveType.Cylinder, "nose2", root.transform, new Vector3(0, 52.4f, 0),
                new Vector3(5.2f, 1.9f, 5.2f), steel);
            Primitive(PrimitiveType.Sphere, "nose3", root.transform, new Vector3(0, 54.6f, 0),
                new Vector3(3.6f, 3.4f, 3.6f), steel);
            // Flaps (windward tile side).
            Primitive(PrimitiveType.Cube, "flapL", root.transform, new Vector3(-5.4f, 41f, 0),
                new Vector3(3.4f, 9f, 0.5f), dark);
            Primitive(PrimitiveType.Cube, "flapR", root.transform, new Vector3(5.4f, 41f, 0),
                new Vector3(3.4f, 9f, 0.5f), dark);
            Primitive(PrimitiveType.Cube, "aflapL", root.transform, new Vector3(-5.8f, 6f, 0),
                new Vector3(4.2f, 11f, 0.6f), dark);
            Primitive(PrimitiveType.Cube, "aflapR", root.transform, new Vector3(5.8f, 6f, 0),
                new Vector3(4.2f, 11f, 0.6f), dark);
            return root;
        }

        public static GameObject HabModule(Transform parent, Vector3 pos, int index)
        {
            var root = new GameObject($"Hab module {index}");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var shell = Mat("hab_shell", new Color(0.85f, 0.82f, 0.76f), 0.1f, 0.45f);
            var trim = Mat("hab_trim", new Color(0.35f, 0.38f, 0.42f), 0.4f, 0.4f);
            // Horizontal cylinder on cradles + end domes + airlock tunnel.
            Primitive(PrimitiveType.Cylinder, "shell", root.transform, new Vector3(0, 3.4f, 0),
                new Vector3(6.6f, 7f, 6.6f), shell, new Vector3(90, 0, 0));
            Primitive(PrimitiveType.Sphere, "domeA", root.transform, new Vector3(0, 3.4f, 7f),
                new Vector3(6.6f, 6.6f, 6.6f), shell);
            Primitive(PrimitiveType.Sphere, "domeB", root.transform, new Vector3(0, 3.4f, -7f),
                new Vector3(6.6f, 6.6f, 6.6f), shell);
            Primitive(PrimitiveType.Cube, "cradle", root.transform, new Vector3(0, 0.7f, 0),
                new Vector3(7.4f, 1.4f, 12f), trim);
            Primitive(PrimitiveType.Cylinder, "airlock", root.transform, new Vector3(4.6f, 1.8f, 0),
                new Vector3(2.4f, 2.6f, 2.4f), trim, new Vector3(0, 0, 90));
            return root;
        }

        public static GameObject GreenhouseTube(Transform parent, Vector3 pos, int index, out Material glowMat)
        {
            var root = new GameObject($"Greenhouse {index}");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var frame = Mat("gh_frame", new Color(0.55f, 0.57f, 0.6f), 0.3f, 0.4f);
            // Per-tube emissive material instance (glows when lights are on).
            glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = $"gh_glass_{index}",
                color = new Color(0.75f, 0.82f, 0.8f, 0.65f),
            };
            glowMat.SetFloat("_Smoothness", 0.7f);
            glowMat.EnableKeyword("_EMISSION");

            Primitive(PrimitiveType.Cylinder, "tube", root.transform, new Vector3(0, 2.2f, 0),
                new Vector3(5f, 11f, 4.4f), glowMat, new Vector3(90, 0, 0));
            Primitive(PrimitiveType.Cube, "footing", root.transform, new Vector3(0, 0.4f, 0),
                new Vector3(5.6f, 0.8f, 23f), frame);
            return root;
        }

        public static GameObject SolarTable(Transform parent, Vector3 pos, Material panelMat)
        {
            var root = new GameObject("Solar table");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var leg = Mat("solar_leg", new Color(0.4f, 0.42f, 0.46f), 0.5f, 0.35f);
            Primitive(PrimitiveType.Cube, "panel", root.transform, new Vector3(0, 2.1f, 0),
                new Vector3(19.4f, 0.12f, 9.4f), panelMat, new Vector3(18, 0, 0));
            Primitive(PrimitiveType.Cube, "legA", root.transform, new Vector3(-8f, 0.9f, 0),
                new Vector3(0.35f, 1.8f, 0.35f), leg);
            Primitive(PrimitiveType.Cube, "legB", root.transform, new Vector3(8f, 0.9f, 0),
                new Vector3(0.35f, 1.8f, 0.35f), leg);
            return root;
        }

        public static GameObject Reactor(Transform parent, Vector3 pos, int index)
        {
            var root = new GameObject($"Fission unit {index}");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var core = Mat("reactor_core", new Color(0.3f, 0.32f, 0.36f), 0.7f, 0.5f);
            var fin = Mat("radiator", new Color(0.88f, 0.89f, 0.92f), 0.35f, 0.55f,
                new Color(0.06f, 0.02f, 0.02f));
            Primitive(PrimitiveType.Cylinder, "vessel", root.transform, new Vector3(0, 2.6f, 0),
                new Vector3(2.6f, 2.6f, 2.6f), core);
            // Deployed radiator wings.
            Primitive(PrimitiveType.Cube, "radA", root.transform, new Vector3(-6.5f, 3.3f, 0),
                new Vector3(10f, 4.2f, 0.12f), fin);
            Primitive(PrimitiveType.Cube, "radB", root.transform, new Vector3(6.5f, 3.3f, 0),
                new Vector3(10f, 4.2f, 0.12f), fin);
            Primitive(PrimitiveType.Cube, "berm", root.transform, new Vector3(0, 0.8f, 5f),
                new Vector3(9f, 1.6f, 1.8f), Mat("berm", new Color(0.5f, 0.3f, 0.19f), 0f, 0.05f));
            return root;
        }

        public static GameObject IsruPlant(Transform parent, Vector3 pos)
        {
            var root = new GameObject("ISRU plant");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var box = Mat("isru_box", new Color(0.75f, 0.72f, 0.65f), 0.4f, 0.4f);
            var pipe = Mat("isru_pipe", new Color(0.5f, 0.52f, 0.56f), 0.6f, 0.5f);
            Primitive(PrimitiveType.Cube, "unitA", root.transform, new Vector3(0, 2f, 0),
                new Vector3(10f, 4f, 6f), box);
            Primitive(PrimitiveType.Cube, "unitB", root.transform, new Vector3(0, 2f, 8f),
                new Vector3(10f, 4f, 6f), box);
            Primitive(PrimitiveType.Cylinder, "stack", root.transform, new Vector3(4.2f, 5.4f, 0),
                new Vector3(0.8f, 2.4f, 0.8f), pipe);
            Primitive(PrimitiveType.Cylinder, "pipe", root.transform, new Vector3(0, 1f, 4f),
                new Vector3(0.5f, 4f, 0.5f), pipe, new Vector3(90, 0, 0));
            return root;
        }

        public static GameObject DepotTank(Transform parent, Vector3 pos, int index)
        {
            var root = new GameObject($"Cryo tank {index}");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var tank = Mat("cryo_tank", new Color(0.9f, 0.91f, 0.94f), 0.75f, 0.7f);
            Primitive(PrimitiveType.Capsule, "tank", root.transform, new Vector3(0, 2.6f, 0),
                new Vector3(4.6f, 5.5f, 4.6f), tank, new Vector3(0, 0, 90));
            Primitive(PrimitiveType.Cube, "saddleA", root.transform, new Vector3(-3f, 1f, 0),
                new Vector3(1f, 2f, 5f), Mat("saddle", new Color(0.4f, 0.42f, 0.46f), 0.5f, 0.35f));
            Primitive(PrimitiveType.Cube, "saddleB", root.transform, new Vector3(3f, 1f, 0),
                new Vector3(1f, 2f, 5f), Mat("saddle", new Color(0.4f, 0.42f, 0.46f), 0.5f, 0.35f));
            return root;
        }

        public static GameObject MiningRig(Transform parent, Vector3 pos, int index)
        {
            var root = new GameObject($"Mining rig {index}");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var body = Mat("rig_body", new Color(0.8f, 0.6f, 0.15f), 0.3f, 0.35f);
            var track = Mat("rig_track", new Color(0.15f, 0.15f, 0.16f), 0.2f, 0.2f);
            Primitive(PrimitiveType.Cube, "body", root.transform, new Vector3(0, 1.5f, 0),
                new Vector3(3.4f, 1.4f, 5.4f), body);
            Primitive(PrimitiveType.Cube, "trackL", root.transform, new Vector3(-1.9f, 0.7f, 0),
                new Vector3(0.8f, 1.4f, 6f), track);
            Primitive(PrimitiveType.Cube, "trackR", root.transform, new Vector3(1.9f, 0.7f, 0),
                new Vector3(0.8f, 1.4f, 6f), track);
            Primitive(PrimitiveType.Cylinder, "drum", root.transform, new Vector3(0, 1.1f, 3.4f),
                new Vector3(1.6f, 1.8f, 1.6f), track, new Vector3(0, 0, 90));
            return root;
        }

        public static GameObject Person(Transform parent, Vector3 pos, bool robot)
        {
            var root = new GameObject(robot ? "Robot" : "Crew");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var mat = robot
                ? Mat("robot_body", new Color(0.2f, 0.22f, 0.26f), 0.6f, 0.6f, new Color(0f, 0.25f, 0.4f))
                : Mat("suit", new Color(0.93f, 0.93f, 0.9f), 0.05f, 0.4f);
            Primitive(PrimitiveType.Capsule, "body", root.transform, new Vector3(0, 0.95f, 0),
                new Vector3(0.62f, 0.95f, 0.62f), mat);
            if (!robot)
                Primitive(PrimitiveType.Sphere, "visor", root.transform, new Vector3(0, 1.62f, 0.12f),
                    new Vector3(0.42f, 0.42f, 0.42f), Mat("visor", new Color(0.9f, 0.7f, 0.3f), 0.9f, 0.9f));
            return root;
        }

        public static GameObject CargoCrate(Transform parent, Vector3 pos)
        {
            var root = new GameObject("Cargo");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            Primitive(PrimitiveType.Cube, "crate", root.transform, new Vector3(0, 1.2f, 0),
                new Vector3(3.2f, 2.4f, 3.2f), Mat("crate", new Color(0.65f, 0.6f, 0.5f), 0.1f, 0.3f));
            return root;
        }
    }
}
