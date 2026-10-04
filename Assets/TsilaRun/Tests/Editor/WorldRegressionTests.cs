using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TsilaRun.Tests
{
    // Exercise real pools and simulation without opening or regenerating the user's scene.
    public sealed class WorldRegressionTests
    {
        GameObject fixture;
        RunnerGame game;
        RunnerWorld world;
        RunnerItem[] pool;

        [SetUp]
        public void CreateWorld()
        {
            fixture = new GameObject("World regression fixture");
            fixture.SetActive(false);
            game = fixture.AddComponent<RunnerGame>();
            game.enabled = false;
            var playerRoot = new GameObject("Player");
            playerRoot.transform.SetParent(fixture.transform, false);
            game.player = playerRoot.AddComponent<RunnerPlayer>();
            game.player.body = playerRoot.AddComponent<CapsuleCollider>();
            var worldRoot = new GameObject("World");
            worldRoot.transform.SetParent(fixture.transform, false);
            world = worldRoot.AddComponent<RunnerWorld>();
            world.game = game;
            world.player = game.player;
            game.world = world;
            game.player.world = world;
            var road = new GameObject("Road template");
            road.transform.SetParent(fixture.transform, false);
            road.AddComponent<RunnerRoadSection>().scenery = new GameObject[0];
            world.roadPrefab = road;
            world.itemPrefabs = new RunnerItem[RunnerRules.ItemKindCount];
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
            {
                var root = new GameObject("Template " + (RunnerItemKind)kind);
                root.transform.SetParent(fixture.transform, false);
                var item = root.AddComponent<RunnerItem>();
                item.kind = (RunnerItemKind)kind;
                item.hitbox = root.AddComponent<BoxCollider>();
                item.hitbox.isTrigger = true;
                item.hitbox.center = Vector3.up * .9f;
                item.hitbox.size = Vector3.one * .9f;
                if (item.kind == RunnerItemKind.Tower)
                {
                    item.hitbox.center = Vector3.up * 1.8f;
                    item.hitbox.size = new Vector3(1.8f, 3.6f, 1.1f);
                }
                else if (item.kind == RunnerItemKind.Barrier)
                {
                    item.hitbox.center = Vector3.up * .425f;
                    item.hitbox.size = new Vector3(1.7f, .85f, .9f);
                }
                else if (item.kind == RunnerItemKind.Overhead)
                {
                    item.hitbox.center = Vector3.up * (RunnerRules.OverheadClearance + .9f);
                    item.hitbox.size = new Vector3(1.9f, 1.8f, .9f);
                }
                else if (item.kind == RunnerItemKind.Coin)
                    item.hitbox.size = new Vector3(.65f, .65f, .3f);
                world.itemPrefabs[kind] = item;
            }
            game.StartRun();
            pool = world.GetComponentsInChildren<RunnerItem>(true);
            foreach (var item in pool) item.Release();
        }

        [TearDown]
        public void DestroyWorld()
        {
            Object.DestroyImmediate(fixture);
        }

        RunnerItem Place(RunnerItemKind kind, float z, int index = 0)
        {
            var item = pool.Where(candidate => candidate.kind == kind).ElementAt(index);
            item.Place(0f, z);
            return item;
        }

        void Travel(float distance)
        {
            world.Simulate(distance, game.player.HitBounds, game.player.HitBounds);
        }

        [Test]
        public void EarlierShieldProtectsAgainstHazardInSameTick()
        {
            var shield = Place(RunnerItemKind.Shield, 1.5f);
            var tower = Place(RunnerItemKind.Tower, 4f);
            Travel(8f);
            Assert.AreEqual(RunnerGame.RunState.Running, game.State);
            Assert.IsFalse(shield.InUse);
            Assert.IsFalse(tower.InUse);
            Assert.AreEqual(0f, game.ShieldRemaining);
            Assert.AreEqual(0, world.ActiveItemCount);
        }

        [Test]
        public void LaterShieldCannotProtectAgainstEarlierHazard()
        {
            var tower = Place(RunnerItemKind.Tower, 1.5f);
            var shield = Place(RunnerItemKind.Shield, 4f);
            Travel(8f);
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
            Assert.IsTrue(tower.InUse);
            Assert.IsTrue(shield.InUse);
            Assert.AreEqual(0f, game.ShieldRemaining);
        }

        [Test]
        public void ShieldAbsorbsNearestHazardRegardlessOfPoolOrder()
        {
            game.CollectPowerUp(RunnerItemKind.Shield);
            var farther = Place(RunnerItemKind.Tower, 4f);
            var nearer = Place(RunnerItemKind.Tower, 1.5f, 1);
            Travel(8f);
            Assert.IsFalse(nearer.InUse);
            Assert.IsTrue(farther.InUse);
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
        }

        [Test]
        public void CoinBeforeCrashCountsAndCoinAfterCrashDoesNot()
        {
            var before = Place(RunnerItemKind.Coin, 1.5f);
            Place(RunnerItemKind.Tower, 4f);
            var after = Place(RunnerItemKind.Coin, 6f, 1);
            Travel(8f);
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
            Assert.AreEqual(1, game.Coins);
            Assert.IsFalse(before.InUse);
            Assert.IsTrue(after.InUse);
        }

        [Test]
        public void BoostPickupDoesNotChangeTimestepForLaterPoolEntries()
        {
            game.CollectPowerUp(RunnerItemKind.CoinMagnet);
            var first = Place(RunnerItemKind.Coin, 5f);
            var second = Place(RunnerItemKind.Coin, 5f, 1);
            first.transform.position += Vector3.right * RunnerRules.LaneWidth;
            second.transform.position += Vector3.right * RunnerRules.LaneWidth;
            Place(RunnerItemKind.SpeedBoost, 0f);
            Travel(.3f);
            Assert.Greater(game.BoostRemaining, 0f);
            Assert.Less(Vector3.Distance(first.transform.position, second.transform.position), .0001f);
        }

        [Test]
        public void LargeTravelRecyclesAllRoadsAndSpawnsOnlyAhead()
        {
            float distance = RunnerRules.RoadCount * RunnerRules.RoadLength * 3f + .2f;
            Bounds away = new Bounds(Vector3.right * 100f, Vector3.one);
            world.Simulate(distance, away, away);
            var roads = world.GetComponentsInChildren<RunnerRoadSection>(true)
                .OrderBy(section => section.transform.position.z).ToArray();
            Assert.AreEqual(RunnerRules.RoadCount, roads.Length);
            for (int i = 0; i < roads.Length; i++)
            {
                Assert.That(roads[i].transform.position.z, Is.InRange(-RunnerRules.RoadLength * 1.5f,
                    RunnerRules.RoadCount * RunnerRules.RoadLength - RunnerRules.RoadLength * 1.5f));
                if (i > 0) Assert.AreEqual(RunnerRules.RoadLength,
                    roads[i].transform.position.z - roads[i - 1].transform.position.z, .001f);
            }
            Assert.IsTrue(pool.Where(item => item.InUse).All(item => item.transform.position.z > 0f));
            Assert.AreEqual(pool.Count(item => item.InUse), world.ActiveItemCount);
        }

        [Test]
        public void MissingPooledObjectIsRecoveredOnRestart()
        {
            int originalCount = world.transform.childCount;
            Object.DestroyImmediate(world.transform.GetChild(1).gameObject);
            Assert.DoesNotThrow(() => world.ResetWorld(42));
            Assert.AreEqual(originalCount, world.transform.childCount);
            Object.DestroyImmediate(world.GetComponentsInChildren<RunnerItem>(true)[7].gameObject);
            Assert.DoesNotThrow(() => world.ResetWorld(43));
            Assert.AreEqual(originalCount, world.transform.childCount);
        }

        [Test]
        public void AllHardWavesHaveCoinsAndPreserveBoostedRecoveryBudget()
        {
            typeof(RunnerGame).GetProperty("Distance").SetValue(game, 600d);
            for (int seed = 0; seed < 50; seed++)
            {
                world.ResetWorld(seed);
                var items = world.GetComponentsInChildren<RunnerItem>(true).Where(item => item.InUse).ToArray();
                var hazards = items.Where(item => RunnerRules.IsObstacle(item.kind))
                    .OrderBy(item => item.transform.position.z).ToArray();
                for (int i = 0; i < hazards.Length; i += 3)
                {
                    float row = hazards[i].transform.position.z;
                    Assert.GreaterOrEqual(items.Count(item => item.kind == RunnerItemKind.Coin &&
                        item.transform.position.z >= row - 6.01f && item.transform.position.z <= row + 15f), 5,
                        "Every visible hard wave needs its reward trail at seed " + seed);
                    if (i == 0) continue;
                    float seconds = (row - hazards[i - 1].transform.position.z - RunnerRules.PersonDriftBudget) /
                        (RunnerRules.MaxSpeed + RunnerRules.SpeedBoostBonus);
                    Assert.GreaterOrEqual(seconds, RunnerRules.ReactionSeconds + 2f * RunnerRules.LaneSeconds + RunnerRules.SlideSeconds,
                        "Generation must preserve the recovery budget even during a boost.");
                }
            }
        }

        [Test]
        public void OverlappingRewardTrailsDoNotStackCoinMeshes()
        {
            var spawn = typeof(RunnerWorld).GetMethod("PlaceCoinAt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawn.Invoke(world, new object[] { 0f, 20f, .9f });
            spawn.Invoke(world, new object[] { 0f, 20f, .9f });
            Assert.AreEqual(1, pool.Count(item => item.InUse && item.kind == RunnerItemKind.Coin));
        }

        [Test]
        public void InvalidSceneryPeriodStillCyclesWithoutDivisionByZero()
        {
            var prop = new GameObject("Spaced prop");
            prop.transform.SetParent(fixture.transform, false);
            var spacing = prop.AddComponent<ScenerySpacing>();
            spacing.period = 0;
            spacing.slot = -1;
            Assert.DoesNotThrow(() => spacing.SetSection(-10));
            Assert.IsTrue(prop.activeSelf);
            spacing.period = 3;
            spacing.slot = 2;
            spacing.SetSection(-1);
            Assert.IsTrue(prop.activeSelf);
            spacing.SetSection(0);
            Assert.IsFalse(prop.activeSelf);
        }
    }
}
