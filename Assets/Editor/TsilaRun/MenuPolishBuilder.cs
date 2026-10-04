using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace TsilaRun.Editor
{
    public static class MenuPolishBuilder
    {
        const string UI = "Assets/TsilaRun/Art/UI";
        public static Material SandMaterial()
        {
            string path = MeshyPackIntegration.Generated + "/Materials/Island Sand.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("TsilaRun/Sand")); AssetDatabase.CreateAsset(material, path); }
            return material;
        }
        public static TMP_FontAsset FontAsset()
        {
            string path = UI + "/Fonts/Rajdhani SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null) return asset;
            if (Shader.Find("TextMeshPro/Distance Field") == null)
            {
                string package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).First();
                AssetDatabase.ImportPackage(package, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            asset = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(UI + "/Fonts/Rajdhani-Bold.ttf"),
                100, 12, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = "Rajdhani SDF";
            asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 /:!?'.+-()", out string missing);
            if (missing.Length > 0) throw new System.InvalidOperationException("Missing Rajdhani glyphs: " + missing);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, path);
            foreach (var atlas in asset.atlasTextures) { atlas.name = "Rajdhani atlas"; AssetDatabase.AddObjectToAsset(atlas, asset); }
            asset.material.name = "Rajdhani SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
            return asset;
        }

        public static Material TitleMaterial()
        {
            string path = UI + "/Title Glow.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FontAsset().material); material.name = "Title Glow";
                material.shader = Shader.Find("TextMeshPro/Distance Field");
                material.EnableKeyword("GLOW_ON"); material.SetColor("_GlowColor", new Color(.25f, 1f, .55f, .2f));
                material.SetFloat("_GlowOuter", .08f); material.SetFloat("_GlowPower", .5f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        [MenuItem("Tools/Tsila Run/Polish Menu and Gameplay")]
        public static void ApplyBatch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            FontAsset();
            MeshyPackIntegration.BuildPropsBatch();
            MeshyPackIntegration.RefreshLayoutBatch();
            CharacterMotionBuilder.Build();
            ModernUiUpdater.RefreshBatch();
            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            RenderSettings.fogColor = new Color32(25, 42, 54, 255);
            RenderSettings.ambientLight = new Color32(145, 162, 183, 255);
            Stage(game);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_PRESENTATION_POLISH_OK");
        }

        public static void ApplyAndCapture() { ApplyBatch(); PrototypeScreenshots.CaptureBatch(); }

        public static void Stage(RunnerGame game)
        {
            var presentation = game.GetComponent<RunnerPresentation>();
            if (presentation.stage != null) Object.DestroyImmediate(presentation.stage);
            var stage = new GameObject("Neon Character Stage"); presentation.stage = stage;
            var dark = Material("Stage Navy", new Color32(8, 15, 27, 255), Color.black);
            var neon = Material("Stage Neon", new Color32(69, 232, 145, 255), new Color(.05f, .85f, .32f) * 2f);
            MobilePrototypeBuilder.Shape(stage.transform, "Platform rim", PrimitiveType.Cylinder,
                new Vector3(0, -.085f, 0), new Vector3(2.05f, .035f, 2.05f), neon);
            MobilePrototypeBuilder.Shape(stage.transform, "Platform", PrimitiveType.Cylinder,
                new Vector3(0, -.045f, 0), new Vector3(1.98f, .04f, 1.98f), dark);
            for (int side = -1; side <= 1; side += 2)
                MobilePrototypeBuilder.Shape(stage.transform, "Neon rail", PrimitiveType.Cube,
                    new Vector3(side * 1.45f, 1.05f, 1.2f), new Vector3(.025f, 2.5f, .025f), neon);
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Night Gradient and Halo"; backdrop.transform.SetParent(stage.transform, false);
            backdrop.transform.localPosition = new Vector3(0, .8f, 4); backdrop.transform.localScale = new Vector3(15, 15, 1);
            Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            string bgPath = UI + "/MenuBackdrop.mat";
            var bg = AssetDatabase.LoadAssetAtPath<Material>(bgPath);
            if (bg == null) { bg = new Material(Shader.Find("TsilaRun/MenuBackdrop")); AssetDatabase.CreateAsset(bg, bgPath); }
            backdrop.GetComponent<Renderer>().sharedMaterial = bg;
            var rim = new GameObject("Violet Rim").AddComponent<Light>();
            rim.transform.SetParent(stage.transform, false); rim.type = LightType.Point;
            rim.transform.localPosition = new Vector3(-1.6f, 1.7f, .6f); rim.color = new Color(.48f, .24f, 1f);
            rim.range = 4f; rim.intensity = 2f; rim.shadows = LightShadows.None;
            var particles = new GameObject("Menu Sparks").AddComponent<ParticleSystem>();
            particles.transform.SetParent(stage.transform, false); particles.transform.localPosition = new Vector3(0, .05f, .8f);
            var main = particles.main; main.maxParticles = 18; main.startLifetime = 3f; main.startSpeed = .16f;
            main.startSize = .022f; main.startColor = new Color(.3f, 1f, .65f, .5f);
            var emission = particles.emission; emission.rateOverTime = 4;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(3, .05f, 1);
            var velocity = particles.velocityOverLifetime; velocity.enabled = true; velocity.y = .15f;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("TsilaRunParticles");
            var volume = stage.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10;
            string profilePath = UI + "/MenuBloom.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath);
                var bloom = profile.Add<Bloom>(); bloom.intensity.Override(.22f); bloom.threshold.Override(1.1f);
                bloom.scatter.Override(.45f); bloom.highQualityFiltering.Override(false);
                AssetDatabase.AddObjectToAsset(bloom, profile); EditorUtility.SetDirty(profile);
            }
            volume.sharedProfile = profile;
            var camera = Camera.main; camera.allowHDR = true; camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            stage.AddComponent<MenuStageMotion>();
            presentation.ShowMenu(true);
        }

        static Material Material(string name, Color color, Color emission)
        {
            string path = UI + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            if (name == "Stage Navy") material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .2f);
            material.SetColor("_EmissionColor", emission); material.EnableKeyword("_EMISSION");
            // URP validates _EMISSION from AnyEmissive. None silently strips the
            // keyword on reimport, turning the neon accents dark in the saved scene.
            material.globalIlluminationFlags = emission.maxColorComponent > 0f
                ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(material); return material;
        }

        public static void RepairStageMaterials()
        {
            Material("Stage Neon", new Color32(69, 232, 145, 255), new Color(.05f, .85f, .32f) * 2f);
        }
    }
}
