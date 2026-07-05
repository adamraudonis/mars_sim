using MarsSim.Core;
using UnityEngine;

namespace MarsSim.UnityApp.BaseView
{
    /// <summary>
    /// Physically-driven sky: the directional light follows the environment model's sun
    /// azimuth/elevation, intensity tracks computed surface irradiance, and sky/fog color
    /// respond to optical depth — the butterscotch daytime sky, blue-grey Martian sunsets,
    /// and brown-black dust storms all fall out of tau and elevation.
    /// </summary>
    public sealed class SkyController : MonoBehaviour
    {
        public Light Sun;

        private Camera _cam;
        private ParticleSystem _dust;
        private float _smoothedTau = 0.5f;

        private static readonly Color DaySky = new(0.76f, 0.55f, 0.38f);
        private static readonly Color SunsetSky = new(0.45f, 0.40f, 0.44f);  // Mars sunsets are cool blue-grey
        private static readonly Color NightSky = new(0.016f, 0.010f, 0.012f);
        private static readonly Color StormSky = new(0.42f, 0.28f, 0.16f);

        private void Start()
        {
            _cam = Camera.main;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            BuildStormParticles();
        }

        private void BuildStormParticles()
        {
            var go = new GameObject("DustStorm");
            go.transform.SetParent(transform, false);
            _dust = go.AddComponent<ParticleSystem>();
            var main = _dust.main;
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 5f);
            main.startColor = new Color(0.55f, 0.36f, 0.2f, 0.06f);
            main.startLifetime = 4f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = _dust.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(700, 120, 700);

            var vel = _dust.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(28f, 45f);
            vel.y = new ParticleSystem.MinMaxCurve(-1f, 2f);

            var em = _dust.emission;
            em.rateOverTime = 0;

            var psr = _dust.GetComponent<ParticleSystemRenderer>();
            psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            psr.material.SetColor("_BaseColor", new Color(0.55f, 0.36f, 0.2f, 0.05f));
            SetupTransparency(psr.material);
        }

        private static void SetupTransparency(Material m)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.renderQueue = 3000;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
        }

        private void LateUpdate()
        {
            var runner = SimRunner.Instance;
            if (runner?.Engine == null || Sun == null) return;
            var env = runner.Engine.Context.Env;

            float el = (float)env.SunElevationDeg;
            float az = (float)env.SunAzimuthDeg;
            Sun.transform.rotation = Quaternion.Euler(el, az + 180f, 0);

            _smoothedTau = Mathf.Lerp(_smoothedTau, (float)env.OpticalDepthTau, Time.deltaTime * 2f);
            float tau = _smoothedTau;

            // Intensity from actual computed irradiance (normalized to clear noon ~600 W/m2).
            float ghiNorm = Mathf.Clamp01((float)env.GlobalHorizontalWm2 / 600f);
            Sun.intensity = Mathf.Lerp(0.02f, 1.6f, ghiNorm);
            Sun.color = Color.Lerp(new Color(1f, 0.55f, 0.35f), new Color(1f, 0.93f, 0.85f),
                Mathf.Clamp01(el / 25f));
            Sun.shadowStrength = Mathf.Lerp(0.9f, 0.3f, Mathf.Clamp01((tau - 1f) / 4f));

            // Sky color: elevation blend + storm override.
            float dayness = Mathf.Clamp01((el + 4f) / 20f);
            var sky = Color.Lerp(NightSky, Color.Lerp(SunsetSky, DaySky, Mathf.Clamp01((el - 4f) / 22f)), dayness);
            float stormness = Mathf.Clamp01((tau - 1.5f) / 4f);
            sky = Color.Lerp(sky, StormSky * (0.15f + 0.85f * dayness), stormness);

            if (_cam != null) _cam.backgroundColor = sky;
            RenderSettings.ambientLight = sky * (0.55f + 0.25f * dayness);
            RenderSettings.fogColor = sky;
            RenderSettings.fogDensity = 0.00012f + 0.00030f * Mathf.Clamp01(tau / 6f) + 0.0009f * stormness;

            var em = _dust.emission;
            em.rateOverTime = stormness > 0.05f ? 900f * stormness : 0f;
            if (_cam != null) _dust.transform.position = _cam.transform.position;
        }
    }
}
