using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TsilaRun.Editor
{
    public static class BlenderPackIntegration
    {
        public const string Root = "Assets/TsilaRun/BlenderPack";
        public const string Source = Root + "/Source";
        public const string Generated = Root + "/Generated";
        public static readonly string[] Characters = { "Tsila", "Officer_Lucef", "RunningPerson" };
        static readonly string[] Props = { "RoadSection", "Coin", "Barrier", "Overhead", "Tree", "Building", "FloatingPlatform" };
        public static string PrefabPath(string name) => Generated + "/" + (name == "Officer" ? "Officer_Lucef" : name) + ".prefab";

        [MenuItem("Tools/Tsila Run/Apply Blender Character Pack")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBatch();
        }

        public static void ApplyBatch()
        {
            BackupGameplay();
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory(Generated + "/Materials");
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(Source + "/Textures", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                bool normal = file.Contains("_Normal");
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = file.Contains("Face") ? 2048 : 1024;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            foreach (string name in Characters)
            {
                ConfigureModel(name, true);
                ConfigureModel(name + "_LOD1", true);
                BuildCharacter(name);
            }
            foreach (string name in Props) { ConfigureModel(name, false); BuildProp(name); }
            PatchGameplayPrefabs();
            PatchScene();
            AssetDatabase.SaveAssets();
            ValidateAssets();
            Debug.Log("TSILA_BLENDER_INTEGRATION_OK");
        }

        static void BackupGameplay()
        {
            string folder = "Logs/BlenderPackBackup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(folder);
            foreach (string path in Directory.GetFiles(MobilePrototypeBuilder.Root + "/Prefabs"))
                File.Copy(path, folder + "/" + Path.GetFileName(path), true);
            File.Copy(MobilePrototypeBuilder.ScenePath, folder + "/TsilaRun.unity", true);
            Debug.Log("Preserved pre-integration scene and prefabs in " + folder);
        }

        static void ConfigureModel(string name, bool character)
        {
            string path = Source + "/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1;
            importer.useFileUnits = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.animationType = character ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.avatarSetup = character ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.NoAvatar;
            importer.optimizeGameObjects = false;
            importer.importAnimation = character && !name.EndsWith("_LOD1", StringComparison.Ordinal);
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            if (importer.importAnimation)
            {
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    string state = new[] { "Idle", "Run", "Jump", "Slide" }.FirstOrDefault(s => clip.name.EndsWith(s, StringComparison.OrdinalIgnoreCase));
                    if (state == null) throw new InvalidDataException("Unrecognized clip: " + clip.name);
                    clip.name = state;
                    clip.loopTime = state == "Idle" || state == "Run";
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var material in model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
            {
                if (material == null) throw new InvalidDataException("Missing source material: " + name);
                var mapped = ConvertMaterial(name.Replace("_LOD1", ""), material, character);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), mapped);
            }
            importer.SaveAndReimport();
        }

        static Material ConvertMaterial(string character, Material source, bool isCharacter)
        {
            string key = source.name;
            string path = Generated + "/Materials/" + key + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
            string texture = null;
            if (isCharacter)
            {
                if (key.Contains("Body")) texture = character + "_Body_BaseColor.001.png";
                else if (key.Contains("Face")) texture = character + "_Face_V2.png";
                else if (key.Contains("Hair") && character != "RunningPerson") texture = character + "_Hair_V2.png";
                color = texture != null ? Color.white : key.Contains("Cap") ? new Color(.07f, .08f, .10f) : new Color(.035f, .027f, .022f);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/" + texture));
            mat.SetFloat("_Metallic", key.Contains("Gold") ? .6f : 0);
            mat.SetFloat("_Smoothness", key.Contains("Hair") ? .18f : key.Contains("Face") ? .38f : .22f);
            mat.enableInstancing = true;
            if (isCharacter && key.Contains("Cap"))
            {
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/RunningPerson_Cap_Normal.png"));
                mat.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject InstantiateModel(string name)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "/" + name + ".fbx"));
            model.name = name;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            return model;
        }

        static void BuildCharacter(string name)
        {
            var high = InstantiateModel(name);
            var low = InstantiateModel(name + "_LOD1");
            try
            {
                var animator = high.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidDataException("Invalid imported Generic avatar: " + name);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.runtimeAnimatorController = BuildController(name, high);
                var highSkin = high.GetComponentInChildren<SkinnedMeshRenderer>();
                var lowSkin = low.GetComponentInChildren<SkinnedMeshRenderer>();
                var bones = high.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                lowSkin.bones = lowSkin.bones.Select(b => bones[b.name]).ToArray();
                lowSkin.rootBone = bones[lowSkin.rootBone.name];
                lowSkin.transform.SetParent(high.transform, true);
                lowSkin.name = name + "_GameplayLOD";
                foreach (var skin in new[] { highSkin, lowSkin })
                {
                    skin.quality = SkinQuality.Bone4;
                    skin.updateWhenOffscreen = false;
                    var envelope = new Bounds();
                    bool first = true;
                    foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                    {
                        Vector3 point = skin.transform.InverseTransformPoint(high.transform.TransformPoint(new Vector3(x * 1.5f, .8f + y * 1.5f, z * 2f)));
                        if (first) { envelope = new Bounds(point, Vector3.zero); first = false; } else envelope.Encapsulate(point);
                    }
                    skin.localBounds = envelope;
                }
                var group = high.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(.42f, new Renderer[] { highSkin }), new LOD(.008f, new Renderer[] { lowSkin }) });
                group.localReferencePoint = Vector3.up * .9f;
                group.size = 1.85f;
                group.fadeMode = LODFadeMode.None;
                PrefabUtility.SaveAsPrefabAsset(high, PrefabPath(name));
                Debug.Log("BLENDER_CHARACTER " + name + " clips=" + animator.runtimeAnimatorController.animationClips.Length + " bounds=" + highSkin.bounds);
            }
            finally { Object.DestroyImmediate(high); Object.DestroyImmediate(low); }
        }

        static RuntimeAnimatorController BuildController(string name, GameObject model)
        {
            string path = Generated + "/" + name + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            var imported = AssetDatabase.LoadAllAssetsAtPath(Source + "/" + name + ".fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
            foreach (string state in new[] { "Idle", "Run", "Jump", "Slide" })
            {
                var source = imported.Single(c => c.name == state);
                AnimationClip clip = state == "Slide" ? CreateSlide(model) : Object.Instantiate(source);
                clip.name = state;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = state == "Idle" || state == "Run";
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                string clipPath = Generated + "/" + name + "_" + state + ".anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (existing == null) AssetDatabase.CreateAsset(clip, clipPath);
                else { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); clip = existing; EditorUtility.SetDirty(clip); }
                var item = machine.AddState(state); item.motion = clip; item.writeDefaultValues = true;
                if (state == "Idle") machine.defaultState = item;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimationClip CreateSlide(GameObject model)
        {
            var transforms = model.GetComponentsInChildren<Transform>();
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var rotations = transforms.Select(t => t.localRotation).ToArray();
            var clip = new AnimationClip { frameRate = 30 };
            try
            {
                foreach (string side in new[] { "L", "R" })
                {
                    var upper = transforms.Single(t => t.name == "upper_arm." + side);
                    var lower = transforms.Single(t => t.name == "lower_arm." + side);
                    upper.rotation = Quaternion.FromToRotation((lower.position - upper.position).normalized, Vector3.down) * upper.rotation;
                }
                var root = transforms.Single(t => t.name == "root");
                root.rotation = Quaternion.Euler(-90, 0, 0) * root.rotation;
                root.position += Vector3.up * .35f;
                foreach (var bone in transforms.Where(t => t != model.transform))
                {
                    string path = AnimationUtility.CalculateTransformPath(bone, model.transform);
                    for (int axis = 0; axis < 3; axis++)
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + "xyz"[axis]), AnimationCurve.Constant(0, 1.05f, bone.localPosition[axis]));
                    for (int axis = 0; axis < 4; axis++)
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis]), AnimationCurve.Constant(0, 1.05f, bone.localRotation[axis]));
                }
                clip.EnsureQuaternionContinuity();
            }
            finally
            {
                for (int i = 0; i < transforms.Length; i++) { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; }
            }
            return clip;
        }

        public static Bounds VisualBounds(GameObject root)
        {
            bool first = true; var result = new Bounds();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    var point = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if (first) { result = new Bounds(point, Vector3.zero); first = false; } else result.Encapsulate(point);
                }
            return result;
        }

        public static Bounds SkinnedBounds(GameObject root, SkinnedMeshRenderer skin)
        {
            var mesh = new Mesh();
            try
            {
                skin.BakeMesh(mesh);
                bool first = true; var bounds = new Bounds();
                foreach (var vertex in mesh.vertices)
                {
                    var point = root.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
                return bounds;
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        static void BuildProp(string name)
        {
            var root = new GameObject(name);
            var model = InstantiateModel(name); model.transform.SetParent(root.transform, false);
            try
            {
                var bounds = VisualBounds(root);
                Vector3 target = bounds.size;
                if (name == "RoadSection") target = new Vector3(7.2f, bounds.size.y, 24);
                if (name == "Coin") target = new Vector3(.6f, .6f, .12f);
                if (name == "Barrier") target = new Vector3(1.7f, .85f, .9f);
                if (name == "Overhead") target = new Vector3(1.9f, 2.8f, .9f);
                model.transform.localScale = Vector3.Scale(model.transform.localScale, new Vector3(target.x / bounds.size.x, target.y / bounds.size.y, target.z / bounds.size.z));
                bounds = VisualBounds(root);
                model.transform.localPosition -= new Vector3(bounds.center.x, name == "Coin" ? bounds.center.y : bounds.min.y, bounds.center.z);
                if (name == "Coin") model.transform.localPosition += Vector3.up * .9f;
                // The runner simulation uses a flat y=0 ground plane.
                if (name == "RoadSection") model.transform.localPosition -= Vector3.up * RoadSurfaceHeight(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(name));
                Debug.Log("BLENDER_PROP " + name + " " + VisualBounds(root));
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void PatchGameplayPrefabs()
        {
            string folder = MobilePrototypeBuilder.Root + "/Prefabs/";
            foreach (string name in new[] { "Tsila", "Officer", "RunningPerson", "Barrier", "Overhead", "Coin", "RoadSection" })
            {
                string path = folder + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (name == "Tsila")
                    {
                        var player = root.GetComponent<RunnerPlayer>();
                        ClearChildren(player.visual);
                        MethodVisualBuilder.Character(player.visual, "Tsila").player = player;
                        player.animatedSlide = true;
                    }
                    else if (name == "Officer" || name == "RunningPerson")
                    {
                        ClearChildren(root.transform);
                        MethodVisualBuilder.Character(root.transform, name).alwaysRun = true;
                    }
                    else if (name == "RoadSection")
                    {
                        var old = root.transform.Find("RoadSection");
                        if (old != null) Object.DestroyImmediate(old.gameObject);
                        MethodVisualBuilder.Model(root.transform, name);
                        var island = root.GetComponent<RunnerRoadSection>().scenery[0].transform;
                        foreach (Transform child in island.Cast<Transform>().ToArray())
                            if (new[] { "Trunk", "Tree", "Block House", "Roof", "Blender Tree", "Blender Building", "Blender Platform" }.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
                        foreach (int side in new[] { -1, 1 })
                        {
                            PlaceScenery(island, "Tree", "Blender Tree", new Vector3(side * 6, 0, 4));
                            PlaceScenery(island, "Building", "Blender Building", new Vector3(side * 10, 0, -5));
                            PlaceScenery(island, "FloatingPlatform", "Blender Platform", new Vector3(side * 8, -.3f, 10));
                        }
                    }
                    else { ClearChildren(root.transform); MethodVisualBuilder.Model(root.transform, name); }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        static float RoadSurfaceHeight(GameObject root)
        {
            float area = 0, height = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var vertices = filter.sharedMesh.vertices; var triangles = filter.sharedMesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i]]));
                    Vector3 b = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i + 1]]));
                    Vector3 c = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i + 2]]));
                    if (Mathf.Abs(a.y - b.y) > .001f || Mathf.Abs(a.y - c.y) > .001f) continue;
                    float candidate = Vector3.Cross(b - a, c - a).magnitude;
                    if (candidate > area + .01f) { area = candidate; height = a.y; }
                    else if (Mathf.Abs(candidate - area) < .01f) height = Mathf.Max(height, a.y);
                }
            }
            if (area < 10) throw new InvalidDataException("Road deck surface was not found.");
            return height;
        }

        static void PlaceScenery(Transform parent, string asset, string name, Vector3 position)
        { var model = MethodVisualBuilder.Model(parent, asset); model.name = name; model.transform.localPosition = position; }
        static void ClearChildren(Transform parent)
        { foreach (Transform child in parent.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject); }

        static void PatchScene()
        {
            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            var avatar = game.player.GetComponentInChildren<RunnerAvatar>(true);
            avatar.game = game; avatar.player = game.player;
            var officer = scene.GetRootGameObjects().First(o => o.name == "Officer").GetComponentInChildren<RunnerAvatar>(true);
            game.chase.officer = officer; officer.game = game; officer.alwaysRun = true;
            officer.gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            PrefabUtility.RecordPrefabInstancePropertyModifications(officer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ValidateAssets()
        {
            foreach (string name in Characters)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                var animator = prefab.GetComponent<Animator>();
                if (animator == null || !animator.avatar.isValid || animator.runtimeAnimatorController.animationClips.Length != 4)
                    throw new InvalidDataException("Invalid controller/rig: " + name);
                var levels = prefab.GetComponent<LODGroup>().GetLODs();
                if (levels.Length != 2) throw new InvalidDataException("Missing LOD: " + name);
                foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.bones.Length != 20 || renderer.sharedMaterials.Any(m => m == null || m.shader.name != "Universal Render Pipeline/Lit"))
                        throw new InvalidDataException("Missing bones/materials: " + name);
                }
                Debug.Log("BLENDER_VALIDATED " + name);
            }
        }
    }
}
