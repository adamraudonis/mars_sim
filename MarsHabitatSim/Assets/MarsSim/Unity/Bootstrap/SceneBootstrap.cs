using MarsSim.UnityApp.BaseView;
using MarsSim.UnityApp.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarsSim.UnityApp
{
    /// <summary>
    /// Constructs the entire application at Play time — camera rig, sun, terrain, base view,
    /// UI — so the scene asset stays a one-object stub and everything is reviewable code.
    /// </summary>
    public sealed class SceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // One bootstrap only (domain reloads, duplicated scene objects).
            if (FindObjectsByType<SceneBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            // Kill any default scene furniture; we own the world.
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (cam.GetComponentInParent<SceneBootstrap>() == null) Destroy(cam.gameObject);
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.GetComponentInParent<SceneBootstrap>() == null) Destroy(light.gameObject);

            var simGo = new GameObject("Simulation");
            var runner = simGo.AddComponent<SimRunner>();

            // Camera rig.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camera.fieldOfView = 55;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 6000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<CameraOrbit>();
            camGo.AddComponent<AudioListener>();

            // Sun (driven by SkyController from the environment model).
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            RenderSettings.sun = sun;

            var worldGo = new GameObject("World");
            worldGo.AddComponent<TerrainBuilder>();
            worldGo.AddComponent<SkyController>().Sun = sun;
            worldGo.AddComponent<BaseViewController>();

            var uiGo = new GameObject("UI");
            uiGo.AddComponent<HudController>();
        }
    }
}
