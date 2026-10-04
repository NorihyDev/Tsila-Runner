using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TsilaRun.Tests
{
    public sealed class PunkyContentTests
    {
        const string Key = "TsilaRun.Tests.PunkyProgress";
        [TearDown] public void Cleanup() { PlayerPrefs.DeleteKey(Key); Time.timeScale = 1f; }

        [Test]
        public void PunkyCosts500AndPurchaseSurvivesReloadWithoutChargingTwice()
        {
            PlayerPrefs.SetString(Key, "{\"coins\":499,\"owned\":63,\"selected\":5}");
            var progress = new RunnerProgress(Key);
            Assert.IsFalse(progress.BuyOrEquip(6));
            Assert.AreEqual(499, progress.Wallet);
            progress.EarnCoin();
            Assert.IsTrue(progress.BuyOrEquip(6));
            Assert.AreEqual(0, progress.Wallet);
            progress = new RunnerProgress(Key);
            Assert.AreEqual(6, progress.Selected);
            for (int index = 0; index < 7; index++) Assert.IsTrue(progress.Owns(index));
            Assert.IsTrue(progress.BuyOrEquip(6));
            Assert.AreEqual(0, progress.Wallet);
            Assert.AreEqual(3, RunnerProgress.SkinModelIndex(6));
        }

        [Test]
        public void PunkyHasOriginalFaceTextureAndAllPlayableAnimations()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorrorContentBuilder.SkinFolder + "/Punky.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<RunnerAvatar>());
            var animator = prefab.GetComponent<Animator>();
            Assert.IsTrue(animator.avatar.isValid);
            foreach (string state in new[] { "Idle", "Run", "Jump", "Slide" })
                Assert.IsTrue(animator.runtimeAnimatorController.animationClips.Any(clip => clip.name == "Punky_" + state));
            var face = prefab.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(renderer => renderer.sharedMaterials)
                .First(material => material.name == "Punky_FaceDetails");
            Assert.AreEqual(MeshyPackIntegration.Source + "/Textures/Punky_BaseColor.png",
                AssetDatabase.GetAssetPath(face.GetTexture("_BaseMap")));
        }

        [Test]
        public void PunkyAnimationRemainsGroundedAcrossBothLods()
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HorrorContentBuilder.SkinFolder + "/Punky.prefab"));
            var mesh = new Mesh();
            try
            {
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (string state in new[] { "Run", "Jump", "Slide" })
                {
                    var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Punky_" + state);
                    for (int frame = 0; frame < 8; frame++)
                    {
                        clip.SampleAnimation(model, clip.length * frame / 8f);
                        foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            Assert.IsTrue(skin.bones.All(bone => bone != null));
                            skin.BakeMesh(mesh);
                            var vertices = mesh.vertices.Select(vertex => skin.transform.TransformPoint(vertex)).ToArray();
                            Assert.That(vertices.Min(vertex => vertex.y), Is.InRange(-.04f, .055f), state + " floor at frame " + frame);
                            Assert.That(vertices.Max(vertex => vertex.y), Is.InRange(.2f, 2.5f), state + " height at frame " + frame);
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(model); }
        }

        [Test]
        public void SceneOffersPunkyWithoutOverlappingBackButton()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            Assert.AreEqual("Punky", game.characterPrefabs[3].name);
            var shop = Object.FindAnyObjectByType<RunnerShop>();
            Assert.AreEqual(7, shop.skinButtons.Length);
            var last = (RectTransform)shop.skinButtons[6].transform;
            var back = (RectTransform)shop.close.transform;
            Assert.Greater(last.anchorMin.y, back.anchorMax.y);
        }
    }
}
