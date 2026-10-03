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

        public static void BuildFor(string name, string sourcePath)
        {
            {
                var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
                try
                {
                    var bones = model.GetComponentsInChildren<Transform>().Where(t => t.name == "root" || t.name == "pelvis" || t.name == "spine" || t.name == "chest" || t.name == "neck" || t.name == "head" || t.name.Contains(".")).ToDictionary(t => t.name);
                    var imported = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
                    var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().First();
                    foreach (string state in new[] { "Run", "Slide", "Jump" })
                    {
                        var clip = new AnimationClip { name = name + "_" + state, frameRate = 60 };
                        var tracks = bones.Values.ToDictionary(t => t, _ => new Track());
                        var source = imported.Single(c => c.name == (state == "Slide" ? "Idle" : state));
                        var root = bones["root"];
                        const int frames = 60;
                        float duration = state == "Slide" ? RunnerRules.SlideSeconds : state == "Run" ? .68f : RunnerRules.JumpSeconds;
                        float rollScale = 1f;
                        for (int f = 0; f <= frames; f++)
                        {
                            float progress = f / (float)frames;
                            source.SampleAnimation(model, state == "Slide" ? 0 : progress * source.length);
                            float phase = progress * Mathf.PI * 2f;
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
                                Rotate(bones["chest"], 7);
                                var chest = bones["chest"];
                                chest.localRotation = Quaternion.AngleAxis(Mathf.Sin(phase) * 5f, Vector3.up) * chest.localRotation;
                                Rotate(bones["foot.L"], Mathf.Sin(phase) * 12);
                                Rotate(bones["foot.R"], -Mathf.Sin(phase) * 12);
                            }
                            else
                            {
                                Rotate(bones["upper_leg.L"], -25f * Mathf.Sin(progress * Mathf.PI));
                                Rotate(bones["upper_leg.R"], -25f * Mathf.Sin(progress * Mathf.PI));
                            }
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
        static void Rotate(Transform bone, float angle)
        {
            Vector3 axis = bone.parent != null ? bone.parent.InverseTransformDirection(Vector3.right) : Vector3.right;
            bone.localRotation = Quaternion.AngleAxis(angle, axis) * bone.localRotation;
        }
    }
}
