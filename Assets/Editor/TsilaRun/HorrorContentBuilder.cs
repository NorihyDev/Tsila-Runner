using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TsilaRun.Editor
{
    public static class HorrorContentBuilder
    {
        public const string SkinFolder = MobilePrototypeBuilder.Root + "/Skins";
        public static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath("HorrorGirl")) != null;

        [MenuItem("Tools/Tsila Run/Apply Horror Characters and Skins")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBatch();
        }

        public static void ApplyBatch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string name in new[] { "HorrorGirl", "HorrorSkunx" })
            {
                foreach (string suffix in new[] { "BaseColor", "Normal" })
                {
                    var texture = (TextureImporter)AssetImporter.GetAtPath(MeshyPackIntegration.Source + "/Textures/" + name + "_" + suffix + ".png");
                    texture.textureType = suffix == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    texture.sRGBTexture = suffix != "Normal"; texture.maxTextureSize = 1024;
                    texture.mipmapEnabled = true; texture.textureCompression = TextureImporterCompression.Compressed;
                    texture.SaveAndReimport();
                }
                MeshyPackIntegration.ConfigureModel(name, true);
                MeshyPackIntegration.ConfigureModel(name + "_LOD1", true);
                MeshyPackIntegration.BuildCharacter(name);
                GroundNpcAnimation(name);
            }
            BuildMianja();
            CharacterMotionBuilder.BuildFor("Mianja", BlenderPackIntegration.Source + "/RunningPerson.fbx");
            BuildSkins();
            MeshyPackIntegration.RefreshLayoutBatch();
            ModernUiUpdater.RefreshBatch();
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_HORROR_CONTENT_OK");
        }

        static void BuildMianja()
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BlenderPackIntegration.PrefabPath("RunningPerson")));
            try
            {
                model.name = "Mianja";
                string path = MeshyPackIntegration.Generated + "/Mianja.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                var machine = controller.layers[0].stateMachine;
                foreach (var entry in machine.states) machine.RemoveState(entry.state);
                var originals = model.GetComponent<Animator>().runtimeAnimatorController.animationClips;
                foreach (string state in new[] { "Idle", "Run", "Jump", "Slide" })
                {
                    var clip = Object.Instantiate(originals.Single(c => c.name == state || c.name.EndsWith("_" + state)));
                    clip.name = "Mianja_" + state;
                    string clipPath = MeshyPackIntegration.Generated + "/" + clip.name + ".anim";
                    var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (saved == null) { AssetDatabase.CreateAsset(clip, clipPath); saved = clip; }
                    else { EditorUtility.CopySerialized(clip, saved); Object.DestroyImmediate(clip); }
                    var entry = machine.AddState(state); entry.motion = saved;
                    if (state == "Idle") machine.defaultState = entry;
                }
                model.GetComponent<Animator>().runtimeAnimatorController = controller;
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(model, MeshyPackIntegration.PrefabPath("Mianja"));
            }
            finally { Object.DestroyImmediate(model); }
        }

        public static void GroundNpcAnimation(string name)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
            var animator = model.GetComponent<Animator>();
            var source = animator.runtimeAnimatorController.animationClips.Single(c => c.name.EndsWith("_Run"));
            var clip = new AnimationClip { name = name + "_Run", frameRate = 60 };
            try
            {
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var motionRoot = skins[0].rootBone;
                var transforms = model.GetComponentsInChildren<Transform>().Where(t => t != model.transform).ToArray();
                var curves = transforms.ToDictionary(t => t, t => Enumerable.Range(0, 10).Select(_ => new AnimationCurve()).ToArray());
                for (int frame = 0; frame <= 60; frame++)
                {
                    float time = source.length * frame / 60f;
                    source.SampleAnimation(model, time);
                    var bounds = BlenderPackIntegration.SkinnedBounds(model, skins[0]);
                    foreach (var skin in skins.Skip(1)) bounds.Encapsulate(BlenderPackIntegration.SkinnedBounds(model, skin));
                    motionRoot.position += model.transform.TransformVector(new Vector3(-bounds.center.x, .006f - bounds.min.y, -bounds.center.z));
                    foreach (var transform in transforms)
                    {
                        Vector3 p = transform.localPosition, s = transform.localScale; Quaternion q = transform.localRotation;
                        float[] values = { p.x, p.y, p.z, q.x, q.y, q.z, q.w, s.x, s.y, s.z };
                        for (int index = 0; index < values.Length; index++) curves[transform][index].AddKey(time, values[index]);
                    }
                }
                string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
                foreach (var pair in curves)
                    for (int index = 0; index < properties.Length; index++)
                    {
                        for (int key = 0; key < pair.Value[index].length; key++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(pair.Value[index], key, AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(pair.Value[index], key, AnimationUtility.TangentMode.Linear);
                        }
                        clip.SetCurve(AnimationUtility.CalculateTransformPath(pair.Key, model.transform), typeof(Transform), properties[index], pair.Value[index]);
                    }
                clip.EnsureQuaternionContinuity();
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
                settings.keepOriginalOrientation = settings.keepOriginalPositionY = settings.keepOriginalPositionXZ = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.CopySerialized(clip, source); EditorUtility.SetDirty(source);
                // An idle pose is needed when the chase stops after catching the player.
                var idle = animator.runtimeAnimatorController.animationClips.Single(c => c.name.EndsWith("_Idle"));
                foreach (var binding in AnimationUtility.GetCurveBindings(idle)) AnimationUtility.SetEditorCurve(idle, binding, null);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    AnimationUtility.SetEditorCurve(idle, binding, AnimationCurve.Constant(0f, 1f, AnimationUtility.GetEditorCurve(clip, binding).Evaluate(0f)));
                idle.EnsureQuaternionContinuity(); EditorUtility.SetDirty(idle);
                Debug.Log("HORROR_GROUNDED_RUN " + name + " duration=" + source.length);
            }
            finally { Object.DestroyImmediate(clip); Object.DestroyImmediate(model); }
        }

        static void BuildSkins()
        {
            Directory.CreateDirectory(SkinFolder);
            foreach (var entry in new[] { (name: "Tsila", model: "Tsila"), (name: "Lucef", model: "Officer"), (name: "Mianja", model: "Mianja") })
            {
                var parent = new GameObject("Temporary Skin Parent");
                try
                {
                    var avatar = MethodVisualBuilder.Character(parent.transform, entry.model);
                    avatar.name = entry.name;
                    PrefabUtility.SaveAsPrefabAsset(avatar.gameObject, SkinFolder + "/" + entry.name + ".prefab");
                }
                finally { Object.DestroyImmediate(parent); }
            }
        }

        public static string SceneCharacter(string original)
        {
            if (!Available) return original;
            return original == "Officer" ? "HorrorGirl" : original == "RunningPerson" ? "HorrorSkunx" : original;
        }

        public static void ApplyToScene(RunnerGame game)
        {
            if (!Available) return;
            game.characterPrefabs = new[] { "Tsila", "Lucef", "Mianja" }.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>(SkinFolder + "/" + name + ".prefab")).ToArray();
            if (game.characterPrefabs.Any(p => p == null)) throw new InvalidOperationException("Missing playable skin prefab");
            var officer = game.chase.officer;
            var officerRoot = officer.transform.parent.gameObject;
            // The scene owns its actor. Changing the gameplay prefab must not
            // silently instantiate another visual alongside a scene override.
            if (PrefabUtility.IsPartOfPrefabInstance(officerRoot))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(officerRoot), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var extra in officerRoot.GetComponentsInChildren<RunnerAvatar>(true).Where(avatar => avatar != officer))
                Object.DestroyImmediate(extra.gameObject);
            if (!officer.name.StartsWith("HorrorGirl"))
            {
                var parent = officer.transform.parent;
                Object.DestroyImmediate(officer.gameObject);
                officer = MethodVisualBuilder.Character(parent, "HorrorGirl");
            }
            officer.alwaysRun = true; officer.game = game; officer.gameObject.SetActive(false);
            game.chase.officer = officer;
            foreach (string name in new[] { "Officer", "RunningPerson" })
            {
                string path = MobilePrototypeBuilder.Root + "/Prefabs/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                    MethodVisualBuilder.Character(root.transform, SceneCharacter(name)).alwaysRun = true;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            EditorUtility.SetDirty(game); EditorUtility.SetDirty(game.chase);
        }

        public static void CaptureReview()
        {
            PrototypeScreenshots.CaptureBatch();
            foreach (string name in new[] { "Lucef", "Mianja", "HorrorGirl", "HorrorSkunx" })
            {
                EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
                var game = Object.FindAnyObjectByType<RunnerGame>();
                game.player.gameObject.SetActive(false); game.world.gameObject.SetActive(false);
                game.chase.officer.transform.parent.gameObject.SetActive(false);
                game.GetComponent<RunnerPresentation>().stage.SetActive(false);
                Object.FindAnyObjectByType<RunnerHud>().gameObject.SetActive(false);
                var preview = Object.FindAnyObjectByType<EditorPreview>(); if (preview != null) preview.gameObject.SetActive(false);
                var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(name == "Lucef" || name == "Mianja" ? SkinFolder + "/" + name + ".prefab" : MeshyPackIntegration.PrefabPath(name)));
                var animator = model.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Play("Run", 0, .2f); animator.Update(0f);
                var group = model.GetComponent<LODGroup>(); group.ForceLOD(0);
                foreach (var skin in group.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>())
                {
                    var mesh = new Mesh(); skin.BakeMesh(mesh); skin.enabled = false;
                    var baked = new GameObject("Review Pose"); baked.transform.SetParent(skin.transform, false);
                    baked.AddComponent<MeshFilter>().sharedMesh = mesh;
                    baked.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                }
                MobilePrototypeBuilder.Shape(null, "Review Sand", PrimitiveType.Cube, new Vector3(0,-.06f,0), new Vector3(8,.1f,8), MenuPolishBuilder.SandMaterial());
                var camera = Camera.main; camera.transform.position = new Vector3(2.2f, 1.5f, 3.2f);
                camera.transform.LookAt(new Vector3(0,.9f,0)); camera.fieldOfView = 35; camera.aspect = 2f/3f;
                var target = new RenderTexture(640,960,24); target.Create(); camera.targetTexture = target;
                camera.Render(); RenderTexture.active = target;
                var pixels = new Texture2D(640,960,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,640,960),0,0); pixels.Apply();
                File.WriteAllBytes("Screenshots/Horror-"+name+".png", pixels.EncodeToPNG());
                camera.targetTexture = null; RenderTexture.active = null;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
                Debug.Log("TSILA_CHARACTER_CAPTURE_OK "+name);
            }
        }

        public static void RefreshAndCaptureReview()
        {
            MeshyPackIntegration.RefreshLayoutBatch();
            ModernUiUpdater.RefreshBatch();
            CaptureReview();
        }
    }
}
