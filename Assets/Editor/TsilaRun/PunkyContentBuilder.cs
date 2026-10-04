using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TsilaRun.Editor
{
    public static class PunkyContentBuilder
    {
        [MenuItem("Tools/Tsila Run/Add Punky Skin")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBatch();
        }

        public static void ApplyBatch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string suffix in new[] { "BaseColor", "Normal" })
            {
                string path = MeshyPackIntegration.Source + "/Textures/Punky_" + suffix + ".png";
                if (!File.Exists(path)) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = suffix == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = suffix != "Normal";
                importer.maxTextureSize = suffix == "BaseColor" ? 2048 : 1024;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            MeshyPackIntegration.ConfigureModel("Punky", true);
            MeshyPackIntegration.ConfigureModel("Punky_LOD1", true);
            // The head retains Punky's original UVs and photograph; the outfit uses Tsila's atlas.
            var body = AssetDatabase.LoadAssetAtPath<Material>(MeshyPackIntegration.Generated + "/Materials/Punky_Body.mat");
            var donor = AssetDatabase.LoadAssetAtPath<Material>(MeshyPackIntegration.Generated + "/Materials/Tsila_Body.mat");
            body.CopyPropertiesFromMaterial(donor);
            EditorUtility.SetDirty(body);
            MeshyPackIntegration.BuildCharacter("Punky");
            CharacterMotionBuilder.BuildFor("Punky", MeshyPackIntegration.Source + "/Punky.fbx");
            var parent = new GameObject("Temporary Punky Parent");
            try
            {
                var avatar = MethodVisualBuilder.Character(parent.transform, "Punky");
                avatar.name = "Punky";
                PrefabUtility.SaveAsPrefabAsset(avatar.gameObject, HorrorContentBuilder.SkinFolder + "/Punky.prefab");
            }
            finally { Object.DestroyImmediate(parent); }
            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            game.characterPrefabs = new[] { "Tsila", "Lucef", "Mianja", "Punky" }
                .Select(name => AssetDatabase.LoadAssetAtPath<GameObject>(HorrorContentBuilder.SkinFolder + "/" + name + ".prefab")).ToArray();
            if (game.characterPrefabs.Any(prefab => prefab == null)) throw new InvalidOperationException("Missing playable character prefab");
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ModernUiUpdater.RefreshBatch();
            Debug.Log("TSILA_PUNKY_SKIN_OK price=500 original face retained, Tsila outfit and natural motion");
        }

        public static void CaptureReview()
        {
            // Finish the graphics pass previously waiting for the Editor to close.
            GraphicsPolishBuilder.ApplyBatch();
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            game.player.gameObject.SetActive(false);
            game.world.gameObject.SetActive(false);
            game.chase.officer.transform.parent.gameObject.SetActive(false);
            var hud = Object.FindAnyObjectByType<RunnerHud>();
            hud.gameObject.SetActive(false);
            var preview = Object.FindAnyObjectByType<EditorPreview>();
            if (preview != null) preview.gameObject.SetActive(false);
            game.GetComponent<RunnerPresentation>().stage.SetActive(false);
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HorrorContentBuilder.SkinFolder + "/Punky.prefab"));
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var animator = model.GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            model.GetComponent<LODGroup>().ForceLOD(0);
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().First();
            var pose = new GameObject("Punky Review Pose");
            pose.transform.SetParent(skin.transform, false);
            var filter = pose.AddComponent<MeshFilter>();
            filter.sharedMesh = new Mesh();
            pose.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
            foreach (var part in model.GetComponentsInChildren<SkinnedMeshRenderer>()) part.enabled = false;
            var camera = Camera.main;
            camera.transform.position = new Vector3(2.2f, 1.6f, -4.5f);
            camera.transform.LookAt(new Vector3(0f, .9f, 0f));
            camera.fieldOfView = 32f;
            Directory.CreateDirectory("Logs/PunkyReview");
            var target = new RenderTexture(540, 720, 24);
            target.Create(); camera.targetTexture = target;
            try
            {
                foreach (string state in new[] { "Idle", "Run", "Jump", "Slide" })
                {
                    var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Punky_" + state);
                    clip.SampleAnimation(model, clip.length * (state == "Slide" ? .25f : .35f));
                    skin.BakeMesh(filter.sharedMesh);
                    camera.Render();
                    RenderTexture.active = target;
                    var pixels = new Texture2D(540, 720, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, 540, 720), 0, 0); pixels.Apply();
                    File.WriteAllBytes("Logs/PunkyReview/" + state + ".png", pixels.EncodeToPNG());
                    Object.DestroyImmediate(pixels);
                }
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = null;
                target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(filter.sharedMesh); Object.DestroyImmediate(model);
            }
            Debug.Log("TSILA_PUNKY_REVIEW_OK");
        }

        public static void RestoreAfterTests()
        {
            // The legacy SceneGeneration test replaces the production scene and road.
            MeshyPackIntegration.RefreshLayoutBatch();
            MethodMusicBuilder.ApplyBatch();
            CaptureReview();
            PrototypeScreenshots.CaptureQuick();
            Debug.Log("TSILA_PUNKY_FINAL_SCENE_OK landscape, skins, music and graphics restored");
        }
    }
}
