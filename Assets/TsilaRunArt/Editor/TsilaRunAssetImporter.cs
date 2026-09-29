// Generated asset builder. Editor-only; no third-party packages required.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;

namespace Method.TsilaRun.Editor
{
    public static class TsilaRunAssetImporter
    {
        const string BasePath = "Assets/TsilaRunArt";
        const string Generated = BasePath + "/Generated";
        [Serializable] public class Submesh { public int[] indices; }
        [Serializable] public class Bone { public string name; public int parent; public Vector3 position; }
        [Serializable] public class ColliderData { public Vector3 center; public Vector3 size; }
        [Serializable] public class Track { public int bone; public string path; public float[] times; public float[] values; }
        [Serializable] public class ClipData { public string name; public float duration; public bool loop; public Track[] tracks; }
        [Serializable] public class AssetData
        {
            public string name;
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uv;
            public Submesh[] submeshes;
            public Bone[] bones;
            public int[] joints;
            public float[] weights;
            public ColliderData[] colliders;
            public ClipData[] animations;
        }

        [MenuItem("Tools/Tsila Run/Import Asset Pack")]
        public static void ImportAll()
        {
            ImportForPrototype();
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Tsila Run", "Assets Method importés. Utilisez Create or Update Mobile Prototype pour les intégrer au jeu.", "OK");
        }

        public static void ImportForPrototype()
        {
            if (!Directory.Exists(BasePath + "/Data"))
                throw new DirectoryNotFoundException("Copy TsilaRunArt into the project's Assets folder first.");
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" :
                (GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit"));
            if (shader == null) throw new InvalidOperationException("No supported lit shader found. Use Built-in, URP, or HDRP and retry.");
            var atlas = LoadTexture("MethodAtlas_Mobile_1K.png", true);
            var emission = LoadTexture("MethodEmission_Mobile_1K.png", true);
            var logo = LoadTexture("MethodLogo.png", true);
            Material main = BuildMaterial("METHOD_Atlas", shader, atlas, emission);
            Material mark = BuildMaterial("METHOD_Logo", shader, logo, null);
            var paths = Directory.GetFiles(BasePath + "/Data", "*.json");
            Array.Sort(paths, StringComparer.Ordinal);
            int count = 0;
            foreach (string file in paths)
            {
                var data = JsonUtility.FromJson<AssetData>(File.ReadAllText(file));
                if (data == null || data.vertices == null || data.vertices.Length == 0)
                    throw new InvalidDataException("Invalid mesh data in " + file);
                BuildAsset(data, main, mark); count++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tsila Run: created/updated " + count + " prefabs under " + Generated + ". Characters use Generic rigs. Review in Play Mode before shipping.");
        }

        static Texture2D LoadTexture(string filename, bool srgb)
        {
            string path = BasePath + "/Textures/" + filename;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing texture " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material BuildMaterial(string name, Shader shader, Texture2D albedo, Texture2D emission)
        {
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_BaseColorMap")) mat.SetTexture("_BaseColorMap", albedo);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.12f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.22f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.22f);
            if (emission != null)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emission);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white * 0.65f);
                if (mat.HasProperty("_EmissiveColorMap")) mat.SetTexture("_EmissiveColorMap", emission);
                if (mat.HasProperty("_EmissiveColor")) mat.SetColor("_EmissiveColor", Color.white * 0.65f);
            }
            return SaveAsset(mat, Generated + "/" + name + ".mat");
        }
        static T SaveAsset<T>(T value, string path) where T : UnityEngine.Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
            EditorUtility.CopySerialized(value, existing);
            UnityEngine.Object.DestroyImmediate(value);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        static void BuildAsset(AssetData data, Material main, Material logo)
        {
            var root = new GameObject(data.name);
            try
            {
                var mesh = new Mesh { name = data.name, indexFormat = data.vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.vertices = data.vertices;
                mesh.normals = data.normals;
                mesh.uv = data.uv;
                mesh.subMeshCount = data.submeshes.Length;
                for (int s = 0; s < data.submeshes.Length; s++)
                {
                    int[] triangles = (int[])data.submeshes[s].indices.Clone();
                    // Source is right-handed CCW; Unity front faces use clockwise winding.
                    for (int i = 0; i < triangles.Length; i += 3)
                    { int temp = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = temp; }
                    mesh.SetTriangles(triangles, s);
                }
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                bool rigged = data.bones != null && data.bones.Length > 0;
                Transform[] bones = null;
                if (rigged)
                {
                    bones = new Transform[data.bones.Length];
                    Matrix4x4[] binds = new Matrix4x4[bones.Length];
                    for (int i = 0; i < bones.Length; i++)
                    {
                        Bone b = data.bones[i];
                        bones[i] = new GameObject(b.name).transform;
                        bones[i].SetParent(b.parent < 0 ? root.transform : bones[b.parent], false);
                        bones[i].localPosition = b.position - (b.parent < 0 ? Vector3.zero : data.bones[b.parent].position);
                        binds[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
                    }
                    BoneWeight[] weights = new BoneWeight[data.vertices.Length];
                    for (int i = 0; i < weights.Length; i++)
                    {
                        int k = i * 4;
                        weights[i] = new BoneWeight
                        {
                            boneIndex0 = data.joints[k], boneIndex1 = data.joints[k + 1],
                            boneIndex2 = data.joints[k + 2], boneIndex3 = data.joints[k + 3],
                            weight0 = data.weights[k], weight1 = data.weights[k + 1],
                            weight2 = data.weights[k + 2], weight3 = data.weights[k + 3]
                        };
                    }
                    mesh.bindposes = binds;
                    mesh.boneWeights = weights;
                }
                mesh = SaveAsset(mesh, Generated + "/" + data.name + "_Mesh.asset");
                var materials = new Material[] { main, logo };
                if (rigged)
                {
                    var renderer = root.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh = mesh;
                    renderer.sharedMaterials = materials;
                    renderer.bones = bones;
                    renderer.rootBone = bones[0];
                    // Conservative bounds cover the included body animation poses.
                    renderer.localBounds = new Bounds(new Vector3(0, 0.9f, 0), new Vector3(2.5f, 3.0f, 2.5f));
                    var animator = root.AddComponent<Animator>();
                    var avatar = AvatarBuilder.BuildGenericAvatar(root, "Root");
                    avatar.name = data.name + "_GenericAvatar";
                    animator.avatar = SaveAsset(avatar, Generated + "/" + avatar.name + ".asset");
                    animator.applyRootMotion = false;
                    animator.runtimeAnimatorController = BuildController(data, bones, root.transform);
                }
                else
                {
                    root.AddComponent<MeshFilter>().sharedMesh = mesh;
                    root.AddComponent<MeshRenderer>().sharedMaterials = materials;
                    foreach (ColliderData c in data.colliders)
                    {
                        var collider = root.AddComponent<BoxCollider>();
                        collider.center = c.center; collider.size = c.size;
                        if (data.name == "Coin") collider.isTrigger = true;
                    }
                    if (data.name == "Coin")
                    {
                        var body = root.AddComponent<Rigidbody>();
                        body.isKinematic = true; body.useGravity = false;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, Generated + "/" + data.name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static AnimatorController BuildController(AssetData data, Transform[] bones, Transform root)
        {
            string path = Generated + "/" + data.name + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = controller.layers[0].stateMachine;
            foreach (var state in sm.states) sm.RemoveState(state.state);
            foreach (var source in data.animations)
            {
                var clip = new AnimationClip { name = source.name, frameRate = 30, legacy = false };
                foreach (var track in source.tracks)
                {
                    int width = track.path == "rotation" ? 4 : 3;
                    string property = track.path == "rotation" ? "m_LocalRotation." : "m_LocalPosition.";
                    string target = AnimationUtility.CalculateTransformPath(bones[track.bone], root);
                    for (int axis = 0; axis < width; axis++)
                    {
                        var keys = new Keyframe[track.times.Length];
                        for (int k = 0; k < keys.Length; k++) keys[k] = new Keyframe(track.times[k], track.values[k * width + axis]);
                        var curve = new AnimationCurve(keys);
                        // The supplied slide returns to standing after .75s. Gameplay may hold
                        // the crouch longer under a beam, so enter the middle pose and hold it.
                        if (source.name == "Slide")
                        {
                            float middle = source.duration * 0.5f;
                            float held = curve.Evaluate(middle);
                            var slideKeys = new List<Keyframe>();
                            foreach (var key in keys)
                                if (key.time < middle) slideKeys.Add(new Keyframe(key.time / middle * 0.12f, key.value));
                            slideKeys.Add(new Keyframe(0.12f, held));
                            slideKeys.Add(new Keyframe(1.05f, held));
                            curve = new AnimationCurve(slideKeys.ToArray());
                        }
                        for (int k = 0; k < curve.length; k++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                        }
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(target, typeof(Transform), property + "xyzw"[axis]), curve);
                    }
                }
                clip.EnsureQuaternionContinuity();
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = source.loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                clip = SaveAsset(clip, Generated + "/" + data.name + "_" + source.name + ".anim");
                var state = sm.AddState(source.name);
                state.motion = clip;
                // Every state writes rest defaults so switching from Slide cannot retain its hip offset.
                state.writeDefaultValues = true;
                if (source.name == "Idle") sm.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
