using System.IO;
using UnityEditor;
using UnityEngine;

namespace MarsSim.EditorTools
{
    /// <summary>
    /// Automated visual verification: enters Play mode, runs the timelapse, captures
    /// screenshots at set moments, then exits the editor. Launch (requires a GPU session,
    /// i.e. no -nographics):
    ///   Unity -projectPath &lt;proj&gt; -executeMethod MarsSim.EditorTools.PlayModeVerifier.Run
    /// Screenshots land in &lt;repo&gt;/screenshots/.
    /// </summary>
    public static class PlayModeVerifier
    {
        private const string Flag = "MarsSim_VerifyRun";
        private static double _playStart;
        private static int _shot;

        public static void Run()
        {
            SessionState.SetBool(Flag, true);
            ProjectSetup.CreateAll();
            EditorApplication.update += WaitForCompile;
        }

        private static void WaitForCompile()
        {
            if (EditorApplication.isCompiling) return;
            EditorApplication.update -= WaitForCompile;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MarsBase.unity");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            if (!SessionState.GetBool(Flag, false)) return;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    _playStart = EditorApplication.timeSinceStartup;
                    _shot = 0;
                    EditorApplication.update += CaptureLoop;
                }
            };
        }

        private static void CaptureLoop()
        {
            if (!EditorApplication.isPlaying) return;
            double t = EditorApplication.timeSinceStartup - _playStart;

            var runner = MarsSim.UnityApp.SimRunner.Instance;
            if (runner != null && t > 2)
                runner.SolsPerSecond = t < 26 ? 10 : 50; // sprint to the crewed era after shot 2

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "screenshots"));
            Directory.CreateDirectory(dir);

            // Shots: early base, mid buildup, crewed era, cinematic (panels hidden) —
            // each held until local daytime so the base is actually visible.
            double[] times = { 6, 25, 55, 65 };
            bool daytime = runner?.Engine != null && runner.Engine.Context.Env.SunElevationDeg > 12;
            if (_shot < times.Length && t > times[_shot] && daytime)
            {
                if (_shot == 3)
                    Object.FindFirstObjectByType<MarsSim.UnityApp.UI.HudController>()?.TogglePanels();
                string suffix = _shot == 3 ? "_cinematic" : "";
                string file = Path.Combine(dir, $"verify_{_shot + 1}_sol{runner?.Engine?.Clock.SolNumber ?? 0}{suffix}.png");
                ScreenCapture.CaptureScreenshot(file, 1);
                Debug.Log($"Captured {file}");
                _shot++;
            }

            if (t > 78 || _shot >= times.Length && t > times[^1] + 3)
            {
                SessionState.SetBool(Flag, false);
                Debug.Log($"Verify run done at sol {runner?.Engine?.Clock.SolNumber}, events={runner?.Engine?.Events.Count}");
                EditorApplication.Exit(0);
            }
        }
    }
}
