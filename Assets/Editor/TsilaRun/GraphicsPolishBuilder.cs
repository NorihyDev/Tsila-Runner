using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TsilaRun.Editor
{
    public static class GraphicsPolishBuilder
    {
        [MenuItem("Tools/Tsila Run/Polish Graphics")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBatch();
        }

        public static void ApplyBatch()
        {
            foreach (string path in new[] { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" })
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                pipeline.renderScale = 1f;
                pipeline.msaaSampleCount = 2;
                pipeline.supportsHDR = true;
                var serialized = new SerializedObject(pipeline);
                serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
                serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
                serialized.FindProperty("m_AnyShadowsSupported").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline.mainLightShadowmapResolution = 2048;
                pipeline.shadowDistance = 35f;
                pipeline.shadowCascadeCount = 2;
                EditorUtility.SetDirty(pipeline);
            }

            // Road surfaces need to receive the actors' shadows in every pooled tile.
            // Update existing assets without regenerating their scenery or geometry.
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { MobilePrototypeBuilder.Root, MeshyPackIntegration.Generated }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ConfigureRenderers(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            foreach (var root in scene.GetRootGameObjects()) ConfigureRenderers(root);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .72f, .84f);
            RenderSettings.ambientEquatorColor = new Color(.38f, .43f, .48f);
            RenderSettings.ambientGroundColor = new Color(.25f, .23f, .20f);
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(1f, .94f, .84f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = .65f;
                light.shadowBias = .035f;
                light.shadowNormalBias = .35f;
            }
            var camera = Camera.main;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderShadows = true;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA handles edges without FXAA blur.

            const string profilePath = "Assets/TsilaRun/Art/UI/CourseColor.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            if (!profile.TryGet<ColorAdjustments>(out var color))
            {
                color = profile.Add<ColorAdjustments>();
                AssetDatabase.AddObjectToAsset(color, profile);
            }
            color.contrast.Override(6f);
            color.saturation.Override(4f);
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>();
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }
            bloom.threshold.Override(1.25f);
            bloom.intensity.Override(.12f);
            bloom.scatter.Override(.35f);
            bloom.highQualityFiltering.Override(false);
            var volumeRoot = GameObject.Find("Course Graphics");
            if (volumeRoot == null) volumeRoot = new GameObject("Course Graphics");
            var volume = volumeRoot.GetComponent<Volume>() ?? volumeRoot.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -5f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(color);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_GRAPHICS_POLISH_OK native resolution, MSAA 2x, grounded shadows, subtle grading");
        }

        static void ConfigureRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                renderer.receiveShadows = true;
                // Only characters cast dynamic shadows; scenery remains affordable on mobile.
                if (renderer is SkinnedMeshRenderer) renderer.shadowCastingMode = ShadowCastingMode.On;
            }
        }
    }
}
