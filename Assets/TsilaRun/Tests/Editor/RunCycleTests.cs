using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TsilaRun.Tests
{
    public sealed class RunCycleTests
    {
        [TestCase("Tsila")]
        [TestCase("Officer")]
        [TestCase("Mianja")]
        public void KneesBendBackWithHeelRecoveryAndOppositeArms(string name)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
            try
            {
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
                var bones = model.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                float largestFlex = 0, smallestFlex = 180, highestKnee = 0, rearPush = 0, recoveryClearance = 0;
                for (int frame = 0; frame < 120; frame++)
                {
                    animator.Play("Run", 0, frame / 120f); animator.Update(0);
                    foreach (string side in new[] { "L", "R" })
                    {
                        string other = side == "L" ? "R" : "L";
                        Vector3 thigh = bones["lower_leg." + side].position - bones["upper_leg." + side].position;
                        Vector3 shin = bones["foot." + side].position - bones["lower_leg." + side].position;
                        thigh.x = shin.x = 0;
                        float flex = Vector3.SignedAngle(thigh, shin, Vector3.right);
                        Assert.That(flex, Is.InRange(15f, 122f), name + " knee " + side + " frame " + frame);
                        largestFlex = Mathf.Max(largestFlex, flex); smallestFlex = Mathf.Min(smallestFlex, flex);
                        float drive = -Vector3.SignedAngle(Vector3.down, thigh, Vector3.right);
                        highestKnee = Mathf.Max(highestKnee, drive); rearPush = Mathf.Max(rearPush, -drive);
                        if (drive > 50)
                        {
                            Vector3 arm = bones["lower_arm." + other].position - bones["upper_arm." + other].position;
                            Assert.Greater(arm.z, .06f, "The opposite arm drives forward with the raised knee.");
                        }
                        // A driven knee remains forward as the foot descends to land.
                        // Measure heel clearance during the folded recovery, before landing.
                        if (drive > 30 && flex > 95)
                            recoveryClearance = Mathf.Max(recoveryClearance,
                                bones["foot." + side].position.y - bones["foot." + other].position.y);
                    }
                }
                Assert.Greater(largestFlex, 105, "Recover the heel behind the body.");
                Assert.Less(smallestFlex, 25, "Extend the leg for support and push-off.");
                Assert.Greater(highestKnee, 58, "Drive the thigh forward rather than shuffle.");
                Assert.Greater(rearPush, 30, "Push the leg behind the hips.");
                Assert.Greater(recoveryClearance, .15f, "The recovered heel clears the planted foot.");
                TestContext.WriteLine(name + ": knee flexion " + smallestFlex.ToString("F1") + "–" + largestFlex.ToString("F1") +
                    " degrees; forward thigh " + highestKnee.ToString("F1") + "; heel clearance " + recoveryClearance.ToString("F3") + " m.");
            }
            finally { Object.DestroyImmediate(model); }
        }

        [TestCase("Tsila")]
        [TestCase("Officer")]
        [TestCase("Mianja")]
        public void RunKeepsBothLodsAboveRoadAndLoopsWithoutPoseSnap(string name)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
            try
            {
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
                var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name.EndsWith("_Run"));
                var transforms = model.GetComponentsInChildren<Transform>();
                clip.SampleAnimation(model, .00001f);
                var firstPosition = transforms.Select(t => t.localPosition).ToArray();
                var firstRotation = transforms.Select(t => t.localRotation).ToArray();
                clip.SampleAnimation(model, clip.length - .00001f);
                for (int index = 0; index < transforms.Length; index++)
                {
                    Assert.Less(Vector3.Distance(firstPosition[index], transforms[index].localPosition), .001f, "Loop translation seam.");
                    Assert.Less(Quaternion.Angle(firstRotation[index], transforms[index].localRotation), .1f, "Loop rotation seam.");
                }
                for (int frame = 0; frame < 120; frame++)
                {
                    animator.Play("Run", 0, frame / 120f); animator.Update(0);
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var bounds = BlenderPackIntegration.SkinnedBounds(model, skin);
                        Assert.GreaterOrEqual(bounds.min.y, -.015f, name + " floor at " + frame);
                        Assert.Less(bounds.min.y, .04f, name + " floating at " + frame);
                        Assert.Less(bounds.max.y, 2.05f, name + " excessive body bounce at " + frame);
                    }
                }
            }
            finally { Object.DestroyImmediate(model); }
        }
    }
}
