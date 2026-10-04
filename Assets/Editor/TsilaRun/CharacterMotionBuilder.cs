using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TsilaRun.Editor
{
    // Clips operate on the supplied skeleton, with baked floor clearance at every frame.
    public static class CharacterMotionBuilder
    {
        sealed class Track
        {
            public readonly AnimationCurve[] curves = Enumerable.Range(0, 10).Select(_ => new AnimationCurve()).ToArray();
            public void Add(float time, Transform bone)
            {
                Vector3 p = bone.localPosition, s = bone.localScale; Quaternion q = bone.localRotation;
                float[] values = { p.x, p.y, p.z, q.x, q.y, q.z, q.w, s.x, s.y, s.z };
                for (int i = 0; i < values.Length; i++) curves[i].AddKey(time, values[i]);
            }
        }
        public static void Build()
        {
            foreach (string name in new[] { "Tsila", "Officer" })
                BuildFor(name, MeshyPackIntegration.Source + "/" + name + ".fbx");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath("Mianja")) != null)
                BuildFor("Mianja", BlenderPackIntegration.Source + "/RunningPerson.fbx");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Tsila Run/Rebuild Natural Run Cycles")]
        public static void BuildRunCycles()
        {
            foreach (string name in new[] { "Tsila", "Officer", "Mianja" })
                BuildFor(name, name == "Mianja" ? BlenderPackIntegration.Source + "/RunningPerson.fbx" : MeshyPackIntegration.Source + "/" + name + ".fbx", new[] { "Run" });
            Debug.Log("TSILA_NATURAL_RUN_OK");
        }

        public static void BuildFor(string name, string sourcePath)
        { BuildFor(name, sourcePath, new[] { "Run", "Slide", "Jump" }); }

        static void BuildFor(string name, string sourcePath, string[] states)
        {
            {
                var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
                try
                {
                    var bones = model.GetComponentsInChildren<Transform>().Where(t => t.name == "root" || t.name == "pelvis" || t.name == "hips" || t.name == "spine" || t.name == "chest" || t.name == "neck" || t.name == "head" || t.name.Contains(".")).ToDictionary(t => t.name);
                    var imported = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
                    var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().First();
                    foreach (string state in states)
                    {
                        var clip = new AnimationClip { name = name + "_" + state, frameRate = 60 };
                        var tracks = bones.Values.ToDictionary(t => t, _ => new Track());
                        // Rebuild the run from the neutral pose: the source run bends knees backwards.
                        var source = imported.Single(c => c.name == (state == "Slide" || state == "Run" || state == "Jump" ? "Idle" : state));
                        var root = bones["root"];
                        const int frames = 60;
                        float duration = state == "Slide" ? RunnerRules.SlideSeconds : state == "Run" ? .68f : RunnerRules.JumpSeconds;
                        float rollScale = 1f;
                        for (int f = 0; f <= frames; f++)
                        {
                            float progress = f / (float)frames;
                            source.SampleAnimation(model, state == "Jump" ? progress * source.length : 0);
                            if (state == "Slide")
                            {
                                // A compact tuck rotates around its own centre, then holds low if clearance is blocked.
                                Rotate(bones["spine"], 45); Rotate(bones["chest"], 42); Rotate(bones["neck"], -22);
                                foreach (string side in new[] { "L", "R" })
                                {
                                    Rotate(bones["upper_leg." + side], -115); Rotate(bones["lower_leg." + side], 145);
                                    Rotate(bones["upper_arm." + side], -80); Rotate(bones["lower_arm." + side], -80);
                                }
                                root.localScale = Vector3.one * rollScale;
                                Rotate(root, progress * 360f);
                            }
                            else if (state == "Run")
                            {
                                PoseRun(bones, progress);
                            }
                            else PoseJump(bones, progress);
                            var bounds = BlenderPackIntegration.SkinnedBounds(model, skin);
                            Vector3 offset = new Vector3(state == "Slide" ? -bounds.center.x : 0, .006f - bounds.min.y, state == "Slide" ? -bounds.center.z : 0);
                            root.position += model.transform.TransformVector(offset);
                            foreach (var track in tracks) track.Value.Add(progress * duration, track.Key);
                        }
                        string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
                        foreach (var track in tracks)
                        {
                            string path = AnimationUtility.CalculateTransformPath(track.Key, model.transform);
                            for (int i = 0; i < properties.Length; i++)
                            {
                                for (int k = 0; k < track.Value.curves[i].length; k++)
                                {
                                    AnimationUtility.SetKeyLeftTangentMode(track.Value.curves[i], k, AnimationUtility.TangentMode.Linear);
                                    AnimationUtility.SetKeyRightTangentMode(track.Value.curves[i], k, AnimationUtility.TangentMode.Linear);
                                }
                                clip.SetCurve(path, typeof(Transform), properties[i], track.Value.curves[i]);
                            }
                        }
                        clip.EnsureQuaternionContinuity();
                        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = state == "Run";
                        settings.keepOriginalOrientation = settings.keepOriginalPositionXZ = settings.keepOriginalPositionY = true;
                        AnimationUtility.SetAnimationClipSettings(clip, settings);
                        var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(MeshyPackIntegration.Generated + "/" + clip.name + ".anim");
                        EditorUtility.CopySerialized(clip, saved); EditorUtility.SetDirty(saved); Object.DestroyImmediate(clip);
                    }
                }
                finally { Object.DestroyImmediate(model); }
            }
            AssetDatabase.SaveAssets();
        }

        // One leg: landing, loaded support, push-off, heel recovery, knee drive, landing.
        // Positive knee flexion folds the heel behind the thigh, never through the kneecap.
        static readonly AnimationCurve RunThigh = Cycle(
            new Vector2(0, -24), new Vector2(.16f, 0), new Vector2(.34f, 30),
            new Vector2(.44f, 38), new Vector2(.56f, 12), new Vector2(.70f, -45),
            new Vector2(.82f, -63), new Vector2(1, -24));
        static readonly AnimationCurve RunKnee = Cycle(
            new Vector2(0, 24), new Vector2(.16f, 35), new Vector2(.34f, 20),
            new Vector2(.44f, 55), new Vector2(.56f, 115), new Vector2(.70f, 105),
            new Vector2(.82f, 75), new Vector2(1, 24));
        static readonly AnimationCurve RunAnkle = Cycle(
            new Vector2(0, -6), new Vector2(.16f, -35), new Vector2(.34f, -24),
            new Vector2(.44f, -8), new Vector2(.56f, 12), new Vector2(.70f, -12),
            new Vector2(.82f, -6), new Vector2(1, -6));

        static AnimationCurve Cycle(params Vector2[] poses)
        {
            var curve = new AnimationCurve(poses.Select(p => new Keyframe(p.x, p.y)).ToArray());
            for (int key = 0; key < curve.length; key++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.ClampedAuto);
            }
            // The last key is the first pose of the next stride; match its velocity at the seam.
            float tangent = (poses[1].y - poses[poses.Length - 2].y) / (poses[1].x + 1f - poses[poses.Length - 2].x);
            var first = curve[0]; first.inTangent = first.outTangent = tangent; curve.MoveKey(0, first);
            var last = curve[curve.length - 1]; last.inTangent = last.outTangent = tangent; curve.MoveKey(curve.length - 1, last);
            return curve;
        }

        static void PoseRun(Dictionary<string, Transform> bones, float progress)
        {
            float phase = progress * Mathf.PI * 2f;
            Rotate(bones.TryGetValue("pelvis", out var pelvis) ? pelvis : bones["hips"], 6);
            Rotate(bones["spine"], 4);
            Rotate(bones["neck"], -7);
            var chest = bones["chest"];
            Vector3 up = chest.parent.InverseTransformDirection(Vector3.up);
            chest.localRotation = Quaternion.AngleAxis(Mathf.Sin(phase) * 4f, up) * chest.localRotation;
            foreach (string side in new[] { "L", "R" })
            {
                float legPhase = Mathf.Repeat(progress + (side == "L" ? 0 : .5f), 1f);
                float otherPhase = Mathf.Repeat(legPhase + .5f, 1f);
                Rotate(bones["upper_leg." + side], RunThigh.Evaluate(legPhase) - 6);
                Rotate(bones["lower_leg." + side], RunKnee.Evaluate(legPhase));
                Rotate(bones["foot." + side], RunAnkle.Evaluate(legPhase));
                Rotate(bones["upper_arm." + side], RunThigh.Evaluate(otherPhase) * .65f + 5);
                Rotate(bones["lower_arm." + side], -85f + Mathf.Sin(legPhase * Mathf.PI * 2f) * 8f);
            }
        }

        static void PoseJump(Dictionary<string, Transform> bones, float progress)
        {
            float arc = Mathf.Sin(progress * Mathf.PI);
            float landing = Mathf.SmoothStep(0f, 1f, Mathf.Abs(progress - .5f) * 2f);
            Rotate(bones.TryGetValue("pelvis", out var pelvis) ? pelvis : bones["hips"], Mathf.Lerp(8f, -4f, arc));
            Rotate(bones["spine"], Mathf.Lerp(5f, 14f, arc));
            Rotate(bones["chest"], Mathf.Lerp(2f, 10f, arc));
            Rotate(bones["neck"], Mathf.Lerp(-4f, -12f, arc));

            foreach (string side in new[] { "L", "R" })
            {
                float tuck = side == "L" ? Mathf.Lerp(18f, 48f, arc) : Mathf.Lerp(-10f, 22f, arc);
                float knee = Mathf.Lerp(28f, side == "L" ? 82f : 58f, arc) + landing * 10f;
                Rotate(bones["upper_leg." + side], tuck);
                Rotate(bones["lower_leg." + side], knee);
                Rotate(bones["foot." + side], Mathf.Lerp(-18f, 8f, arc));
                Rotate(bones["upper_arm." + side], side == "L" ? Mathf.Lerp(-20f, -55f, arc) : Mathf.Lerp(25f, 55f, arc));
                Rotate(bones["lower_arm." + side], -80f);
            }
        }

        static void Rotate(Transform bone, float angle)
        {
            Vector3 axis = bone.parent != null ? bone.parent.InverseTransformDirection(Vector3.right) : Vector3.right;
            bone.localRotation = Quaternion.AngleAxis(angle, axis) * bone.localRotation;
        }
    }
}
