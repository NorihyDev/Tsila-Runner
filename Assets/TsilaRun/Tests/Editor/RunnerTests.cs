using System.Collections;
using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TsilaRun.Tests
{
    public sealed class RunnerTests
    {
        [Test]
        public void PatternBudgetAllowsReactionAndRecoveryAtMaximumSpeed()
        {
            float seconds = RunnerRules.RowSpacing / RunnerRules.MaxSpeed;
            Assert.Greater(seconds, RunnerRules.ReactionSeconds + 2f * RunnerRules.LaneSeconds + RunnerRules.JumpSeconds);
            Assert.Greater(seconds, RunnerRules.ReactionSeconds + 2f * RunnerRules.LaneSeconds + RunnerRules.SlideSeconds);
            var random = new System.Random(728);
            int safe = 1;
            for (int i = 0; i < 10000; i++)
            {
                int next = RunnerRules.NextSafeLane(safe, random);
                Assert.That(next, Is.InRange(0, 2));
                Assert.LessOrEqual(Mathf.Abs(safe - next), 1);
                safe = next;
            }
        }

        [Test]
        public void SweptCollisionCatchesTunnelingAndAllowsClearance()
        {
            Bounds standing = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(0.64f, 1.8f, 0.64f));
            Bounds obstacle = new Bounds(new Vector3(0f, 0.425f, 8f), new Vector3(1.7f, 0.85f, 0.9f));
            Assert.IsTrue(RunnerRules.SweptOverlap(standing, standing, obstacle, 16f));
            var jumped = standing; jumped.center += Vector3.up * 1.3f;
            Assert.IsFalse(RunnerRules.SweptOverlap(jumped, jumped, obstacle, 16f));
            var sliding = new Bounds(new Vector3(0f, 0.35f, 0f), new Vector3(0.64f, 0.7f, 0.64f));
            var overhead = new Bounds(new Vector3(0f, 1.9f, 8f), new Vector3(1.9f, 1.8f, 0.9f));
            Assert.IsFalse(RunnerRules.SweptOverlap(sliding, sliding, overhead, 16f));
            Assert.IsTrue(RunnerRules.SweptOverlap(standing, standing, overhead, 16f));
        }

        [TestCase(720, 1280, 0, 0)]
        [TestCase(1080, 1920, 0, 48)]
        [TestCase(1170, 2532, 102, 141)]
        [TestCase(1080, 2400, 72, 90)]
        [TestCase(1536, 2048, 24, 24)]
        public void SafeAreaAndSwipeScaleAcrossPortraitScreens(int width, int height, int bottom, int top)
        {
            var area = new Rect(0f, bottom, width, height - bottom - top);
            SafeAreaPanel.NormalizedAnchors(area, width, height, out Vector2 min, out Vector2 max);
            Assert.AreEqual(bottom, min.y * height, 0.01f);
            Assert.AreEqual(height - top, max.y * height, 0.01f);
            Assert.AreEqual(0, RunnerInput.ClassifySwipe(new Vector2(width * 0.04f, 0f), width, height));
            Assert.AreEqual(1, RunnerInput.ClassifySwipe(new Vector2(width * 0.06f, 0f), width, height));
            Assert.AreEqual(-1, RunnerInput.ClassifySwipe(new Vector2(-width * 0.06f, 0f), width, height));
            Assert.AreEqual(2, RunnerInput.ClassifySwipe(new Vector2(0f, width * 0.06f), width, height));
            Assert.AreEqual(-2, RunnerInput.ClassifySwipe(new Vector2(0f, -width * 0.06f), width, height));
        }

        [Test, Order(0)]
        public void GeneratorIsRepeatableAndPreservesSampleScene()
        {
            string sample = System.IO.File.ReadAllText("Assets/Scenes/SampleScene.unity");
            MobilePrototypeBuilder.GenerateBatch();
            string guid = AssetDatabase.AssetPathToGUID(MobilePrototypeBuilder.ScenePath);
            int count = UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount;
            MobilePrototypeBuilder.GenerateBatch();
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(MobilePrototypeBuilder.ScenePath));
            Assert.AreEqual(count, UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount);
            Assert.AreEqual(1, EditorBuildSettings.globalScenes.Count(s => s.path == MobilePrototypeBuilder.ScenePath));
            Assert.AreEqual(sample, System.IO.File.ReadAllText("Assets/Scenes/SampleScene.unity"));
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
        }

        [UnityTest]
        public IEnumerator GameplayLifecycleAndSyntheticTouch()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            yield return new EnterPlayMode();
            var game = Object.FindAnyObjectByType<RunnerGame>();
            var hud = Object.FindAnyObjectByType<RunnerHud>();
            Assert.IsNotNull(game, "The generated scene must contain the game component after entering Play mode.");
            Assert.IsNotNull(hud);
            var player = game.player;
            var world = game.world;
            game.enabled = false; // Drive exact simulation ticks while testing input through real Update.
            game.SendMessage("OnApplicationFocus", true);
            game.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(RunnerGame.RunState.Ready, game.State);
            hud.playButton.onClick.Invoke();
            Assert.AreEqual(RunnerGame.RunState.Running, game.State);
            player.ChangeLane(1);
            for (int i = 0; i < 20; i++) player.Simulate(0.02f);
            Assert.AreEqual(RunnerRules.LaneWidth, player.transform.position.x, 0.001f);
            player.ChangeLane(1);
            Assert.AreEqual(2, player.Lane);
            player.ResetPlayer();
            player.Jump();
            player.Simulate(0.3f);
            Assert.Greater(player.transform.position.y, 1f);
            for (int i = 0; i < 40; i++) player.Simulate(0.02f);
            Assert.IsTrue(player.IsGrounded);
            player.Slide();
            Assert.AreEqual(RunnerRules.SlideHeight, player.body.height);
            for (int i = 0; i < 60; i++) player.Simulate(0.02f);
            Assert.IsFalse(player.IsSliding);
            Assert.AreEqual(RunnerRules.StandingHeight, player.body.height);

            var pool = world.GetComponentsInChildren<RunnerItem>(true);
            foreach (var item in pool) item.Release();
            var beam = pool.First(i => i.kind == RunnerItemKind.Overhead);
            beam.Place(0f, 0f);
            player.Slide();
            for (int i = 0; i < 60; i++) player.Simulate(0.02f);
            Assert.IsTrue(player.IsSliding, "Standing must wait until the overhead is clear.");
            beam.Release(); player.Simulate(0.02f);
            Assert.IsFalse(player.IsSliding);
            var coin = pool.First(i => i.kind == RunnerItemKind.Coin);
            coin.Place(0f, 1f);
            world.Simulate(2f, player.HitBounds, player.HitBounds);
            Assert.AreEqual(1, game.Coins);
            world.Simulate(0f, player.HitBounds, player.HitBounds);
            Assert.AreEqual(1, game.Coins);
            var tower = pool.First(i => i.kind == RunnerItemKind.Tower);
            tower.Place(0f, 1f);
            int endings = 0;
            System.Action countEnd = () => { if (game.State == RunnerGame.RunState.GameOver) endings++; };
            game.StateChanged += countEnd;
            world.Simulate(2f, player.HitBounds, player.HitBounds);
            game.EndRun();
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
            Assert.AreEqual(1, endings);
            game.StateChanged -= countEnd;
            hud.restartButton.onClick.Invoke();
            Assert.AreEqual(0, game.Coins); Assert.AreEqual(0, game.Score);
            Assert.AreEqual(1, player.Lane); Assert.IsFalse(player.IsSliding);
            Assert.AreEqual(RunnerRules.StartSpeed, game.Speed); Assert.AreEqual(1f, Time.timeScale);
            game.SendMessage("FixedUpdate");
            Assert.Greater(game.Distance, 0d);
            game.SendMessage("OnApplicationFocus", false);
            Assert.AreEqual(RunnerGame.RunState.Paused, game.State);
            Assert.AreEqual(0f, Time.timeScale);
            game.Resume(); Assert.AreEqual(RunnerGame.RunState.Paused, game.State);
            game.SendMessage("OnApplicationFocus", true);
            Assert.AreEqual(RunnerGame.RunState.Paused, game.State);
            hud.resumeButton.onClick.Invoke(); Assert.AreEqual(RunnerGame.RunState.Running, game.State);
            game.SendMessage("OnApplicationPause", true);
            game.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(RunnerGame.RunState.Paused, game.State);
            hud.pausedRestartButton.onClick.Invoke(); Assert.AreEqual(1f, Time.timeScale);

            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            Vector2 origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.3f);
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Began, origin);
            yield return null;
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Moved, origin + Vector2.right * Screen.width * 0.1f);
            yield return null;
            Assert.AreEqual(2, player.Lane, "Touch should trigger before finger release.");
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Moved, origin - Vector2.right * Screen.width * 0.2f);
            yield return null;
            Assert.AreEqual(2, player.Lane, "A touch may only dispatch one action.");
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Canceled, origin);
            yield return null;
            QueueTouch(touchscreen, 2, UnityEngine.InputSystem.TouchPhase.Began, origin);
            yield return null;
            QueueTouch(touchscreen, 2, UnityEngine.InputSystem.TouchPhase.Moved, origin - Vector2.right * Screen.width * 0.1f);
            yield return null;
            Assert.AreEqual(1, player.Lane, "Canceled touch must not lock the next finger.");
            QueueTouch(touchscreen, 2, UnityEngine.InputSystem.TouchPhase.Ended, origin);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Vector2 buttonPoint = RectTransformUtility.WorldToScreenPoint(null, hud.pauseButton.transform.position);
            QueueTouch(touchscreen, 3, UnityEngine.InputSystem.TouchPhase.Began, buttonPoint);
            yield return null;
            QueueTouch(touchscreen, 3, UnityEngine.InputSystem.TouchPhase.Moved, buttonPoint - Vector2.right * Screen.width * 0.2f);
            yield return null;
            Assert.AreEqual(1, player.Lane, "Gestures starting on UI must not change lanes.");
            QueueTouch(touchscreen, 3, UnityEngine.InputSystem.TouchPhase.Canceled, buttonPoint);
            yield return null;
            QueueTouch(touchscreen, 4, UnityEngine.InputSystem.TouchPhase.Began, buttonPoint);
            yield return null;
            QueueTouch(touchscreen, 4, UnityEngine.InputSystem.TouchPhase.Ended, buttonPoint);
            yield return null;
            Assert.AreEqual(RunnerGame.RunState.Paused, game.State, "A touch tap must operate the serialized UI actions.");
            Vector2 resumePoint = RectTransformUtility.WorldToScreenPoint(null, hud.resumeButton.transform.position);
            QueueTouch(touchscreen, 5, UnityEngine.InputSystem.TouchPhase.Began, resumePoint);
            yield return null;
            QueueTouch(touchscreen, 5, UnityEngine.InputSystem.TouchPhase.Ended, resumePoint);
            yield return null;
            Assert.AreEqual(RunnerGame.RunState.Running, game.State);
            InputSystem.RemoveDevice(touchscreen);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator LongRunKeepsPoolsBoundedAndCoinsClear()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            yield return new EnterPlayMode();
            var game = Object.FindAnyObjectByType<RunnerGame>();
            game.enabled = false;
            game.StartRun();
            var world = game.world;
            var items = world.GetComponentsInChildren<RunnerItem>(true);
            int initial = world.transform.childCount;
            var away = new Bounds(new Vector3(100f, 0f, 0f), Vector3.one);
            // 110 km at the maximum speed, far beyond float-origin trouble for an unre-based runner.
            for (int step = 0; step < 100000; step++)
            {
                world.Simulate(1.1f, away, away);
                if (step % 1000 != 0) continue;
                Assert.AreEqual(initial, world.transform.childCount);
                Assert.LessOrEqual(world.ActiveItemCount, 4 * RunnerRules.PoolPerKind);
                foreach (var item in items)
                {
                    if (!item.InUse) continue;
                    Assert.That(item.transform.position.z, Is.InRange(-12f, RunnerRules.Horizon + 4f));
                    if (item.kind != RunnerItemKind.Coin) continue;
                    foreach (var other in items)
                        if (other.InUse && other.kind != RunnerItemKind.Coin)
                            Assert.IsFalse(item.HitBounds.Intersects(other.HitBounds));
                }
            }
            game.Pause(); game.StartRun();
            Assert.AreEqual(initial, world.transform.childCount);
            Assert.AreEqual(0d, game.Distance);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        static void QueueTouch(Touchscreen device, int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 point)
        {
            InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase, position = point });
        }
    }
}
