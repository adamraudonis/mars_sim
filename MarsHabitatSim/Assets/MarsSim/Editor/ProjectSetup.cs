using System.IO;
using MarsSim.UnityApp;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace MarsSim.EditorTools
{
    /// <summary>
    /// One-shot project configuration (idempotent): URP pipeline assets, linear color space,
    /// the UI Toolkit runtime theme + panel settings, and the bootstrap scene. Runs from the
    /// menu or headless via -executeMethod MarsSim.EditorTools.ProjectSetup.CreateAll.
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("MarsSim/Setup Project (URP, theme, scene)")]
        public static void CreateAll()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // ---------- URP ----------
            Directory.CreateDirectory("Assets/Settings");
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP_Renderer.asset");
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, "Assets/Settings/URP_Renderer.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP_Asset.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, "Assets/Settings/URP_Asset.asset");
            }
            pipeline.shadowDistance = 900f;
            pipeline.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // ---------- UI Toolkit runtime theme + panel settings ----------
            Directory.CreateDirectory("Assets/Resources");
            const string themePath = "Assets/Resources/MarsTheme.tss";
            if (!File.Exists(themePath))
            {
                File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath);
            }
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);

            const string psPath = "Assets/Resources/MarsPanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, psPath);
            }
            if (theme != null) ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            EditorUtility.SetDirty(ps);

            // ---------- Bootstrap scene ----------
            Directory.CreateDirectory("Assets/Scenes");
            const string scenePath = "Assets/Scenes/MarsBase.unity";
            if (!File.Exists(scenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("Bootstrap");
                go.AddComponent<SceneBootstrap>();
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            AssetDatabase.SaveAssets();
            Debug.Log("MarsSim project setup complete (URP + theme + scene).");
        }
    }
}
