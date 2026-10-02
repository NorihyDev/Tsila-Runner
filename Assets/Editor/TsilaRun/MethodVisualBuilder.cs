using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsilaRun.Editor
{
    public static class MethodVisualBuilder
    {
        public const string Art = "Assets/TsilaRunArt/Generated/";
        public static GameObject Model(Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name));
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlenderPackIntegration.PrefabPath(name));
            bool blenderPack = prefab != null;
            if (!blenderPack) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + name + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing Method asset: " + name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // The generated gameplay prefab owns these visuals, while original art stays intact.
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(parent, false);
            if (!blenderPack && name == "RoadSection") model.transform.localPosition = new Vector3(0f, 0f, -12f);
            if (!blenderPack && name == "Coin") model.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            foreach (var collider in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var body in model.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(body);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            return model;
        }
        public static RunnerAvatar Character(Transform parent, string name)
        {
            var model = Model(parent, name);
            var avatar = model.AddComponent<RunnerAvatar>();
            var rig = model.AddComponent<RunnerCharacterRig>();
            rig.root = model.transform;
            rig.defaultScale = Vector3.one;
            avatar.animator = model.GetComponent<Animator>();
            avatar.animator.applyRootMotion = false;
            avatar.animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            avatar.suitRenderers = model.GetComponentsInChildren<Renderer>();
            foreach (var bone in model.GetComponentsInChildren<Transform>())
            {
                switch (bone.name)
                {
                    case "upper_arm.L": case "LeftUpperArm": avatar.leftArm = bone; break;
                    case "upper_arm.R": case "RightUpperArm": avatar.rightArm = bone; break;
                    case "upper_leg.L": case "LeftUpperLeg": avatar.leftLeg = bone; break;
                    case "upper_leg.R": case "RightUpperLeg": avatar.rightLeg = bone; break;
                    case "lower_leg.L": case "LeftLowerLeg": avatar.leftKnee = bone; break;
                    case "lower_leg.R": case "RightLowerLeg": avatar.rightKnee = bone; break;
                }
            }
            return avatar;
        }
        public static RunnerPresentation Stage(RunnerGame game, Material dark, Material neon, Material violet)
        {
            var presentation = game.gameObject.AddComponent<RunnerPresentation>();
            presentation.game = game;
            var stage = new GameObject("Method Character Stage");
            presentation.stage = stage;
            MobilePrototypeBuilder.Shape(stage.transform, "Platform rim", PrimitiveType.Cylinder,
                new Vector3(0f, -0.11f, 0f), new Vector3(2.3f, 0.08f, 2.3f), neon);
            MobilePrototypeBuilder.Shape(stage.transform, "Platform", PrimitiveType.Cylinder,
                new Vector3(0f, -0.055f, 0f), new Vector3(2.18f, 0.055f, 2.18f), dark);
            for (int i = 0; i < 7; i++)
            {
                float x = (i - 3) * 0.8f;
                MobilePrototypeBuilder.Shape(stage.transform, "Backdrop fin", PrimitiveType.Cube,
                    new Vector3(x, 0.8f, 2f), new Vector3(0.025f, 3.2f - Mathf.Abs(x) * 0.3f, 0.05f), i % 2 == 0 ? neon : violet);
            }
            stage.SetActive(false);
            return presentation;
        }
    }
}
