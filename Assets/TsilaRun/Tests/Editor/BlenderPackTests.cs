using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TsilaRun.Tests
{
    public sealed class BlenderPackTests
    {
        [TestCase("Tsila", 3)]
        [TestCase("Officer_Lucef", 3)]
        [TestCase("RunningPerson", 4)]
        public void CharactersHaveTexturedSkinsAndSharedSkeletonLods(string name, int materials)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlenderPackIntegration.PrefabPath(name));
            Assert.IsNotNull(prefab);
            var instance = Object.Instantiate(prefab);
            try
            {
                var animator = instance.GetComponent<Animator>();
                Assert.IsTrue(animator.avatar.isValid);
                Assert.IsFalse(animator.applyRootMotion);
                var clips = animator.runtimeAnimatorController.animationClips;
                CollectionAssert.AreEquivalent(new[] { "Idle", "Run", "Jump", "Slide" }, clips.Select(c => c.name.Substring(name.Length + 1)).Distinct());
                var levels = instance.GetComponent<LODGroup>().GetLODs();
                Assert.AreEqual(2, levels.Length);
                var high = (SkinnedMeshRenderer)levels[0].renderers[0];
                var low = (SkinnedMeshRenderer)levels[1].renderers[0];
                Assert.Less(low.sharedMesh.triangles.Length, high.sharedMesh.triangles.Length * .5f);
                CollectionAssert.AreEqual(high.bones.Select(b => b.name), low.bones.Select(b => b.name));
                foreach (var skin in new[] { high, low })
                {
                    Assert.AreEqual(materials, skin.sharedMaterials.Length);
                    Assert.IsTrue(skin.bones.All(b => b != null && b.IsChildOf(instance.transform)));
                    Assert.IsTrue(skin.sharedMaterials.All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"));
                    Assert.IsNotNull(skin.sharedMaterials.First(m => m.name.Contains("Face")).GetTexture("_BaseMap"));
                    var bounds = BlenderPackIntegration.SkinnedBounds(instance, skin);
                    Assert.That(bounds.size.y, Is.InRange(1.75f, 1.88f));
                    Assert.That(bounds.min.y, Is.InRange(-.02f, .02f));
                    foreach (var weight in skin.sharedMesh.boneWeights)
                        Assert.AreEqual(1f, weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, .002f);
                }
                clips.Single(c => c.name == name + "_Slide").SampleAnimation(instance, .5f);
                var slide = BlenderPackIntegration.SkinnedBounds(instance, high);
                Assert.GreaterOrEqual(slide.min.y, -.02f);
                Assert.LessOrEqual(slide.max.y, RunnerRules.SlideHeight + .02f);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void SceneReferencesUseNewPlayerChaserAndOpponent()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            var avatar = game.player.GetComponentInChildren<RunnerAvatar>(true);
            Assert.AreSame(game, avatar.game);
            Assert.AreSame(game.player, avatar.player);
            Assert.AreSame(avatar.GetComponent<RunnerCharacterRig>(), game.player.rig);
            Assert.AreEqual(RunnerRules.ItemKindCount, game.world.itemPrefabs.Length);
            Assert.IsTrue(game.world.itemPrefabs.All(item => item != null));
            Assert.AreEqual(RunnerProgress.SkinNames.Length,
                Object.FindAnyObjectByType<RunnerShop>().skinButtons.Length);
            Assert.IsNotNull(game.chase.officer);
            Assert.AreSame(game, game.chase.officer.game);
            foreach (var character in new[] { avatar, game.chase.officer,
                game.world.itemPrefabs[(int)RunnerItemKind.RunningPerson].GetComponentInChildren<RunnerAvatar>(true) })
            {
                Assert.IsNotNull(character.GetComponent<LODGroup>());
                Assert.IsNotNull(character.leftArm);
                string controller = AssetDatabase.GetAssetPath(character.animator.runtimeAnimatorController);
                Assert.IsTrue(controller.StartsWith(BlenderPackIntegration.Generated) || controller.StartsWith(MeshyPackIntegration.Generated));
            }
            Assert.IsTrue(game.player.animatedSlide);
        }

        [Test]
        public void OutfitTintChangesBodyWithoutReplacingFaceOrHair()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/Tsila.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var avatar = instance.GetComponentInChildren<RunnerAvatar>();
                avatar.SetSuitTint(Color.magenta);
                foreach (var skin in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var materials = skin.sharedMaterials;
                    int body = System.Array.FindIndex(materials, material => material != null && material.name.Contains("Body"));
                    Assert.GreaterOrEqual(body, 0);
                    var block = new MaterialPropertyBlock();
                    skin.GetPropertyBlock(block, body);
                    Assert.AreEqual(Color.magenta, block.GetColor("_BaseColor"));
                    Assert.IsNotNull(materials.First(material => material.name.Contains("Face")).GetTexture("_BaseMap"));
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void RoadAndObstaclesMatchGameplayScale()
        {
            var road = AssetDatabase.LoadAssetAtPath<GameObject>(BlenderPackIntegration.PrefabPath("RoadSection"));
            var bounds = BlenderPackIntegration.VisualBounds(road);
            Assert.AreEqual(24, bounds.size.z, .001f);
            Assert.AreEqual(7.2f, bounds.size.x, .001f);
            Assert.AreEqual(0, bounds.center.z, .001f);
            foreach (string name in new[] { "Barrier", "Coin" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/" + name + ".prefab");
                var item = prefab.GetComponent<RunnerItem>();
                var visual = BlenderPackIntegration.VisualBounds(prefab);
                Assert.LessOrEqual(Vector3.Distance(visual.center, item.hitbox.center), .03f);
                Assert.IsTrue(item.hitbox.isTrigger);
            }
            foreach (string name in new[] { "Tree", "Building", "FloatingPlatform" })
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(BlenderPackIntegration.PrefabPath(name)));
        }
    }
}
