using System;
using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TsilaRun.Tests
{
    public sealed class HorrorContentTests
    {
        const string Key = "TsilaRun.Tests.HorrorProgress";
        [TearDown] public void Cleanup() { PlayerPrefs.DeleteKey(Key); Time.timeScale = 1f; }

        [Test]
        public void NewSkinsCostExactAmountsAndPreserveOldPurchases()
        {
            PlayerPrefs.SetString(Key, "{\"coins\":6000,\"owned\":15,\"selected\":3,\"missionIndex\":1,\"missionProgress\":4}");
            var progress = new RunnerProgress(Key);
            for (int index = 0; index < 4; index++) Assert.IsTrue(progress.Owns(index));
            Assert.IsFalse(progress.Owns(4)); Assert.IsFalse(progress.Owns(5));
            Assert.AreEqual("Lucef", RunnerProgress.SkinNames[4]); Assert.AreEqual(1000, RunnerProgress.Prices[4]);
            Assert.AreEqual("Mianja", RunnerProgress.SkinNames[5]); Assert.AreEqual(5000, RunnerProgress.Prices[5]);
            Assert.IsTrue(progress.BuyOrEquip(4)); Assert.AreEqual(5000, progress.Wallet);
            Assert.IsTrue(progress.BuyOrEquip(5)); Assert.AreEqual(0, progress.Wallet);
            progress = new RunnerProgress(Key);
            Assert.AreEqual(5, progress.Selected); Assert.IsTrue(progress.Owns(4)); Assert.IsTrue(progress.Owns(5));
            Assert.IsTrue(progress.BuyOrEquip(4)); Assert.AreEqual(0, progress.Wallet);
            Assert.AreEqual(RunnerMissionKind.DodgeObstacles, progress.ActiveMission); Assert.AreEqual(4, progress.ActiveMissionProgress);
        }

        [Test]
        public void EightMissionsRotatePayOnceAndPersistTheirProgress()
        {
            PlayerPrefs.DeleteKey(Key);
            var progress = new RunnerProgress(Key);
            foreach (RunnerMissionKind kind in Enum.GetValues(typeof(RunnerMissionKind)))
            {
                Assert.AreEqual(kind, progress.ActiveMission);
                Assert.AreEqual(0, progress.AdvanceMission(kind, -1));
                Assert.AreEqual(0, progress.AdvanceMission(kind, progress.ActiveMissionTarget - 1));
                progress.Save(); progress = new RunnerProgress(Key);
                Assert.AreEqual(progress.ActiveMissionTarget - 1, progress.ActiveMissionProgress);
                Assert.AreEqual(RunnerProgress.MissionReward, progress.AdvanceMission(kind));
                Assert.AreEqual(0, progress.AdvanceMission(kind));
            }
            Assert.AreEqual(RunnerMissionKind.CollectCoins, progress.ActiveMission);
            Assert.AreEqual(8 * RunnerProgress.MissionReward, progress.Wallet);
        }

        [Test]
        public void EquippingCharactersChangesTheModelAndKeepsPlayerCollisionAndRig()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            PlayerPrefs.SetString(Key, "{\"coins\":6000,\"owned\":1}");
            var progress = new RunnerProgress(Key);
            typeof(RunnerGame).GetProperty("Progress").SetValue(game, progress);
            var collider = game.player.body; var visual = game.player.visual;
            foreach (int index in new[] { 0, 4, 5, 0, 5 })
            {
                Assert.IsTrue(progress.BuyOrEquip(index)); game.ApplySkin();
                var avatar = game.player.GetComponentInChildren<RunnerAvatar>();
                Assert.That(avatar.name, Does.StartWith(index == 4 ? "Lucef" : index == 5 ? "Mianja" : "Tsila"));
                Assert.AreSame(game, avatar.game); Assert.AreSame(game.player, avatar.player);
                Assert.AreSame(avatar.GetComponent<RunnerCharacterRig>(), game.player.rig);
                Assert.AreSame(collider, game.player.body); Assert.AreSame(visual, game.player.visual);
                Assert.AreEqual(1, game.player.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(1, visual.GetComponentsInChildren<RunnerAvatar>(true).Length);
                foreach (string state in new[] { "Idle", "Run", "Jump", "Slide" })
                    Assert.IsTrue(avatar.animator.runtimeAnimatorController.animationClips.Any(c => c.name.EndsWith("_" + state)));
            }
        }

        [TestCase("HorrorGirl")]
        [TestCase("HorrorSkunx")]
        public void HorrorMotionIsGroundedInPlaceAcrossBothLods(string name)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
            try
            {
                var animator = model.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0f);
                foreach (string state in new[] { "Run", "Idle" })
                    for (int frame = 0; frame < 60; frame++)
                    {
                        animator.Play(state, 0, frame / 60f); animator.Update(0f);
                        foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            Assert.IsTrue(skin.bones.All(b => b != null));
                            var bounds = BlenderPackIntegration.SkinnedBounds(model, skin);
                            Assert.That(bounds.min.y, Is.InRange(-.02f, .045f), name + " floor at " + frame);
                            Assert.Less(Mathf.Abs(bounds.center.x), .06f); Assert.Less(Mathf.Abs(bounds.center.z), .06f);
                            Assert.That(bounds.size.y, Is.InRange(1.3f, 2.1f));
                        }
                    }
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test]
        public void MissionActionsCountRealJumpsRollsAndBonusesOnlyDuringRunning()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            PlayerPrefs.SetString(Key, "{\"missionIndex\":3}");
            var progress = new RunnerProgress(Key);
            typeof(RunnerGame).GetProperty("Progress").SetValue(game, progress);
            game.RegisterPlayerAction(RunnerMissionKind.Jump); Assert.AreEqual(0, progress.ActiveMissionProgress);
            game.StartRun(); game.CompleteIntro(); game.player.Jump(); game.player.Jump();
            Assert.AreEqual(1, progress.ActiveMissionProgress, "Repeated inputs before the first physics tick are one jump.");
            game.Pause(); game.RegisterPlayerAction(RunnerMissionKind.Jump); Assert.AreEqual(1, progress.ActiveMissionProgress);
            game.Resume();
            PlayerPrefs.SetString(Key, "{\"missionIndex\":4}"); progress = new RunnerProgress(Key);
            typeof(RunnerGame).GetProperty("Progress").SetValue(game, progress);
            game.player.ResetPlayer(); game.player.Slide(); game.player.Slide(); Assert.AreEqual(1, progress.ActiveMissionProgress);
            PlayerPrefs.SetString(Key, "{\"missionIndex\":5}"); progress = new RunnerProgress(Key);
            typeof(RunnerGame).GetProperty("Progress").SetValue(game, progress);
            game.CollectPowerUp(RunnerItemKind.CoinMagnet); game.CollectPowerUp(RunnerItemKind.Barrier);
            Assert.AreEqual(1, progress.ActiveMissionProgress);
        }

        [Test]
        public void SceneUsesHorrorForPatrolAndObstacleWhileTsilaRemainsStarter()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            Assert.That(game.chase.officer.name, Does.StartWith("HorrorGirl"));
            Assert.AreEqual(1, game.chase.officer.transform.parent.GetComponentsInChildren<RunnerAvatar>(true).Length, "No inherited prefab visual alongside the scene's chaser.");
            Assert.AreEqual(1, game.player.GetComponentsInChildren<RunnerAvatar>(true).Length);
            var opponent = game.world.itemPrefabs[(int)RunnerItemKind.RunningPerson];
            Assert.That(opponent.GetComponentInChildren<RunnerAvatar>(true).name, Does.StartWith("HorrorSkunx"));
            Assert.That(game.characterPrefabs[0].name, Does.StartWith("Tsila"));
            Assert.AreEqual(new Vector3(.95f, 1.88f, .8f), opponent.hitbox.size);
            Assert.AreEqual(1, opponent.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void DistanceMissionsUseThePlayersBiomeAndDoNotCountWhilePaused()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            var tick = typeof(RunnerGame).GetMethod("FixedUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (var entry in new[] { (kind: RunnerMissionKind.TravelMetres, distance: 10.95d), (kind: RunnerMissionKind.MountainMetres, distance: 288.95d), (kind: RunnerMissionKind.TunnelMetres, distance: 576.95d) })
            {
                PlayerPrefs.SetString(Key, "{\"missionIndex\":" + (int)entry.kind + "}");
                var progress = new RunnerProgress(Key);
                typeof(RunnerGame).GetProperty("Progress").SetValue(game, progress);
                game.StartRun(); game.CompleteIntro();
                typeof(RunnerGame).GetProperty("Distance").SetValue(game, entry.distance);
                tick.Invoke(game, null); Assert.AreEqual(1, progress.ActiveMissionProgress);
                game.Pause(); tick.Invoke(game, null); Assert.AreEqual(1, progress.ActiveMissionProgress);
                game.Resume();
                if (entry.kind != RunnerMissionKind.TravelMetres)
                {
                    typeof(RunnerGame).GetProperty("Distance").SetValue(game, 10.95d);
                    tick.Invoke(game, null); Assert.AreEqual(1, progress.ActiveMissionProgress, "Island travel is not rocky or tunnel travel.");
                }
            }
        }

        [Test]
        public void RockyBiomeHasContinuousSandAndGroundedDecorations()
        {
            var road = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/RoadSection.prefab"));
            try
            {
                var section = road.GetComponent<RunnerRoadSection>(); section.SetLocation(300d);
                var mountain = section.scenery[1]; Assert.IsTrue(mountain.activeSelf);
                var sand = mountain.transform.Find("Mountain Sand");
                Assert.AreEqual("TsilaRun/Sand", sand.GetComponent<Renderer>().sharedMaterial.shader.name);
                var surface = MeshyPackIntegration.PlacedBounds(sand.gameObject);
                Assert.AreEqual(RunnerRules.RoadLength, surface.size.z, .001f);
                Assert.AreEqual(-.01f, surface.max.y, .001f);
                var roadBacking = MeshyPackIntegration.PlacedBounds(road.transform.Find("Road Foundation").gameObject);
                Assert.AreEqual(RunnerRules.RoadLength, roadBacking.size.z, .001f);
                Assert.Greater(roadBacking.max.y, surface.max.y);
                Assert.Less(roadBacking.max.y, 0f, "Backing closes module gaps underneath the textured deck.");
                foreach (var zone in section.scenery.Take(2))
                    foreach (Transform prop in zone.transform)
                    {
                        if (prop.name == "Island" || prop.name == "Mountain Sand") continue;
                        var bounds = MeshyPackIntegration.PlacedBounds(prop.gameObject);
                        Assert.AreEqual(-.015f, bounds.min.y, .003f, prop.name + " touches sand");
                    }
            }
            finally { Object.DestroyImmediate(road); }
        }
    }
}
