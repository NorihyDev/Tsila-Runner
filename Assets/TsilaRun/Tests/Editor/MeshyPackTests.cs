using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TsilaRun.Tests
{
    public sealed class MeshyPackTests
    {
        [Serializable]
        sealed class ManifestEntry
        {
            public string name;
            public string input;
            public int triangles;
            public int lodTriangles;
        }

        [Serializable]
        sealed class ManifestContainer
        {
            public ManifestEntry[] models;
        }

        [Test]
        public void EveryGlbHasAnOptimizedTexturedPrefabAndLod()
        {
            string manifestPath = MeshyPackIntegration.Root + "/Manifest.json";
            var manifest = JsonUtility.FromJson<ManifestContainer>(
                "{\"models\":" + File.ReadAllText(manifestPath) + "}");
            Assert.IsNotNull(manifest);
            Assert.IsNotNull(manifest.models);

            string[] sources = Directory.GetFiles("Assets/TsilaRun/Art/AI", "*.glb")
                .Select(Path.GetFileName).OrderBy(name => name).ToArray();
            string[] mapped = manifest.models.Select(model => model.input).OrderBy(name => name).ToArray();
            CollectionAssert.AreEqual(sources, mapped);

            foreach (var entry in manifest.models)
            {
                Assert.Greater(entry.triangles, 0, entry.name);
                Assert.Greater(entry.lodTriangles, 0, entry.name);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(
                    MeshyPackIntegration.Source + "/" + entry.name + ".fbx"), entry.name);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(entry.name));
                Assert.IsNotNull(prefab, entry.name);
                var lods = prefab.GetComponent<LODGroup>().GetLODs();
                Assert.AreEqual(2, lods.Length, entry.name);
                Assert.IsTrue(lods.All(lod => lod.renderers.Length > 0), entry.name);
                Assert.IsTrue(prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .All(material => material != null && material.GetTexture("_BaseMap") != null), entry.name);
            }

            var footprints = AssetDatabase.LoadAssetAtPath<Material>(
                MeshyPackIntegration.Generated + "/Materials/SandFootprints_Material.mat");
            Assert.AreEqual(1f, footprints.GetFloat("_Surface"));
        }

        [TestCase("Tsila")]
        [TestCase("Officer")]
        public void TexturedCharactersAnimateAndFitSlidingClearance(string name)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
            try
            {
                var animator = instance.GetComponent<Animator>();
                Assert.IsTrue(animator.avatar.isValid);
                Assert.IsFalse(animator.applyRootMotion);
                var levels = instance.GetComponent<LODGroup>().GetLODs();
                Assert.AreEqual(2, levels.Length);
                var skin = (SkinnedMeshRenderer)levels[0].renderers[0];
                var low = (SkinnedMeshRenderer)levels[1].renderers[0];
                Assert.LessOrEqual(skin.sharedMesh.triangles.Length / 3, 21000);
                Assert.Less(low.sharedMesh.triangles.Length, skin.sharedMesh.triangles.Length * .5f);
                CollectionAssert.AreEqual(skin.bones.Select(b => b.name), low.bones.Select(b => b.name));
                Assert.IsTrue(skin.sharedMaterials.All(m => m != null && m.GetTexture("_BaseMap") != null));
                var clips = animator.runtimeAnimatorController.animationClips;
                clips.Single(c => c.name.EndsWith("_Idle")).SampleAnimation(instance, 0f);
                var standing = BlenderPackIntegration.SkinnedBounds(instance, skin);
                Assert.That(standing.size.y, Is.InRange(1.75f, 1.85f));
                clips.Single(c => c.name.EndsWith("_Run")).SampleAnimation(instance, 0f);
                var knee = skin.bones.Single(b => b.name == "lower_leg.L");
                Vector3 start = knee.position;
                clips.Single(c => c.name.EndsWith("_Run")).SampleAnimation(instance, .25f);
                Assert.Greater(Vector3.Distance(start, knee.position), .08f);
                foreach (var renderer in new[] { skin, low })
                {
                    clips.Single(c => c.name.EndsWith("_Slide")).SampleAnimation(instance, .5f);
                    var slide = BlenderPackIntegration.SkinnedBounds(instance, renderer);
                    Assert.GreaterOrEqual(slide.min.y, -.03f);
                    Assert.LessOrEqual(slide.max.y, RunnerRules.SlideHeight + .03f);
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void CoinsStayOutsideMovingRunnersFuturePath()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var world = Object.FindAnyObjectByType<RunnerWorld>();
            world.ResetWorld(42);
            var items = world.GetComponentsInChildren<RunnerItem>(true);
            foreach (var item in items) item.Release();
            items.First(i => i.kind == RunnerItemKind.RunningPerson).Place(0f, 30f);
            var spawn = typeof(RunnerWorld).GetMethod("PlaceCoinAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawn.Invoke(world, new object[] { 0f, 30f + RunnerRules.PersonDriftBudget - .1f, .9f });
            Assert.IsFalse(items.Any(i => i.InUse && i.kind == RunnerItemKind.Coin), "Do not spawn a coin a runner can catch during its drift.");
            spawn.Invoke(world, new object[] { 0f, 30f + RunnerRules.PersonDriftBudget + 2f, .9f });
            Assert.AreEqual(1, items.Count(i => i.InUse && i.kind == RunnerItemKind.Coin), "Coins beyond the drift corridor must still spawn.");
        }

        [Test]
        public void ReplacementsKeepCollisionAndRequiredSceneReferences()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            var avatar = game.player.GetComponentInChildren<RunnerAvatar>();
            Assert.AreSame(game, avatar.game);
            Assert.AreSame(game.player, avatar.player);
            Assert.AreSame(avatar.GetComponent<RunnerCharacterRig>(), game.player.rig);
            Assert.AreSame(game, game.chase.officer.game);
            Assert.That(AssetDatabase.GetAssetPath(avatar.animator.runtimeAnimatorController), Does.StartWith(MeshyPackIntegration.Generated));
            Assert.That(AssetDatabase.GetAssetPath(game.chase.officer.animator.runtimeAnimatorController), Does.StartWith(MeshyPackIntegration.Generated));
            Assert.IsTrue(game.world.itemPrefabs.All(p => p != null));
            Assert.AreEqual(RunnerRules.StandingHeight, game.player.body.height, .001f);
            var tower = game.world.itemPrefabs[(int)RunnerItemKind.Tower];
            Assert.AreEqual(3.6f, tower.hitbox.size.y, .001f);
            var barrier = game.world.itemPrefabs[(int)RunnerItemKind.Barrier];
            Assert.AreEqual(.85f, barrier.hitbox.size.y, .001f);
            Assert.AreEqual(1, barrier.GetComponentsInChildren<Collider>(true).Length);
            Assert.IsNotNull(barrier.GetComponentInChildren<MeshFilter>(true));
            var overhead = game.world.itemPrefabs[(int)RunnerItemKind.Overhead];
            Assert.AreEqual(1f, overhead.hitbox.center.y - overhead.hitbox.size.y * .5f, .001f);
            Assert.AreEqual(1, overhead.GetComponentsInChildren<Collider>().Length);
            var road = game.world.roadPrefab;
            Assert.AreEqual(3, road.GetComponent<RunnerRoadSection>().scenery.Length);
            Assert.AreEqual(0, road.GetComponentsInChildren<Collider>(true).Length);
            var roadArt = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath("RoadSection"));
            var bounds = BlenderPackIntegration.VisualBounds(roadArt);
            Assert.AreEqual(24, bounds.size.z, .01f);
            Assert.AreEqual(8, bounds.size.x, .01f);
            Assert.AreEqual(0, bounds.center.z, .01f);
            var coin = game.world.itemPrefabs[(int)RunnerItemKind.Coin];
            Assert.Less(Vector3.Distance(BlenderPackIntegration.VisualBounds(coin.gameObject).center, coin.hitbox.center), .02f);
            var magnet = game.world.itemPrefabs[(int)RunnerItemKind.CoinMagnet];
            Assert.Less(Vector3.Distance(BlenderPackIntegration.VisualBounds(magnet.gameObject).center, magnet.hitbox.center), .02f);
            var speedBoost = game.world.itemPrefabs[(int)RunnerItemKind.SpeedBoost];
            Assert.AreEqual(Vector3.one * .9f, speedBoost.hitbox.size);
            Assert.IsNotNull(speedBoost.GetComponentInChildren<MeshFilter>(true));
            foreach (var zone in road.GetComponent<RunnerRoadSection>().scenery)
                Assert.Greater(zone.GetComponentsInChildren<Renderer>(true).Length, 0);
        }
    }
}
