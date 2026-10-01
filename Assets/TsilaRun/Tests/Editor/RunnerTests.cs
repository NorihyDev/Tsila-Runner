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
        static Touchscreen syntheticTouchscreen;
        static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        static InputSettings.BackgroundBehavior previousBackgroundInput;
        static bool changedInputSettings;
        static RunnerGame trackedGame;
        static int gameOverEvents;
        const string TestSave = "TsilaRun.Tests.Progress";

        static void IsolateWallet(RunnerGame game)
        {
            PlayerPrefs.DeleteKey(TestSave);
            typeof(RunnerGame).GetProperty("Progress").SetValue(game, new RunnerProgress(TestSave));
            game.ApplySkin();
        }

        static void CountGameOver()
        {
            if (trackedGame.State == RunnerGame.RunState.GameOver) gameOverEvents++;
        }

        [SetUp]
        public void UseDeterministicSceneReload()
        {
            SessionState.SetBool("TsilaRun.Tests.FastPlay", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }

        [TearDown]
        public void RestoreTestEnvironment()
        {
            PlayerPrefs.DeleteKey(TestSave);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("TsilaRun.Tests.FastPlay", true);
            if (syntheticTouchscreen != null && syntheticTouchscreen.added) InputSystem.RemoveDevice(syntheticTouchscreen);
            if (changedInputSettings)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                InputSystem.settings.backgroundBehavior = previousBackgroundInput;
                changedInputSettings = false;
            }
        }

        [Test]
        public void PatternBudgetAllowsReactionAndRecoveryAtMaximumSpeed()
        {
            float seconds = (RunnerRules.RowSpacing - RunnerRules.PersonDriftBudget) / RunnerRules.MaxSpeed;
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

        [Test]
        public void DifficultyScalingAddsMoreObstaclesAndCoinArcs()
        {
            Assert.Greater(RunnerRules.RecommendedObstacleCount(0d), 0);
            Assert.Greater(RunnerRules.RecommendedObstacleCount(420d), RunnerRules.RecommendedObstacleCount(0d));
            Assert.Greater(RunnerRules.RecommendedCoinTrailLength(420d), RunnerRules.RecommendedCoinTrailLength(0d));
            Assert.IsTrue(RunnerRules.CanCoinRideObstacle(RunnerItemKind.Barrier));
            Assert.IsTrue(RunnerRules.CanCoinRideObstacle(RunnerItemKind.Overhead));
        }

        [Test]
        public void PowerUpsHaveTimedEffectsAndAreNotClassifiedAsObstacles()
        {
            Assert.AreEqual(RunnerRules.MagnetDuration, RunnerRules.PowerUpDuration(RunnerItemKind.CoinMagnet));
            Assert.AreEqual(RunnerRules.ShieldDuration, RunnerRules.PowerUpDuration(RunnerItemKind.Shield));
            Assert.AreEqual(RunnerRules.SpeedBoostDuration, RunnerRules.PowerUpDuration(RunnerItemKind.SpeedBoost));
            Assert.AreEqual(0f, RunnerRules.PowerUpDuration(RunnerItemKind.Coin));
            Assert.IsTrue(RunnerRules.IsPowerUp(RunnerItemKind.CoinMagnet));
            Assert.IsFalse(RunnerRules.IsObstacle(RunnerItemKind.Shield));
            Assert.IsTrue(RunnerRules.IsObstacle(RunnerItemKind.RunningPerson));
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

        [Test, Order(0), Category("SceneGeneration")]
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
            IsolateWallet(game);
            var player = game.player;
            var world = game.world;
            game.enabled = false; // Drive exact simulation ticks while testing input through real Update.
            game.SendMessage("OnApplicationFocus", true);
            game.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(RunnerGame.RunState.Ready, game.State);
            hud.playButton.onClick.Invoke();
            Assert.AreEqual(RunnerGame.RunState.Intro, game.State);
            game.SendMessage("FixedUpdate");
            float introProgress = game.chase.IntroProgress;
            game.SendMessage("OnApplicationPause", true);
            game.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(RunnerGame.RunState.Paused, game.State);
            game.SendMessage("FixedUpdate");
            Assert.AreEqual(introProgress, game.chase.IntroProgress);
            Assert.AreEqual(0d, game.Distance);
            hud.resumeButton.onClick.Invoke();
            Assert.AreEqual(RunnerGame.RunState.Intro, game.State);
            hud.skipIntroButton.onClick.Invoke();
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
            Assert.AreEqual(1, game.Progress.Wallet);
            Assert.AreEqual(1, game.Progress.ActiveMissionProgress);

            var magnet = pool.First(i => i.kind == RunnerItemKind.CoinMagnet);
            magnet.Place(0f, 0f);
            world.Simulate(0f, player.HitBounds, player.HitBounds);
            Assert.AreEqual(RunnerRules.MagnetDuration, game.MagnetRemaining);
            var attractedCoin = pool.First(i => i.kind == RunnerItemKind.Coin);
            attractedCoin.Place(RunnerRules.LaneWidth, 4f, 0.9f);
            float distanceBeforeAttraction = Vector3.Distance(attractedCoin.transform.position, player.HitBounds.center);
            world.Simulate(0.1f, player.HitBounds, player.HitBounds);
            float distanceAfterAttraction = Vector3.Distance(attractedCoin.transform.position, player.HitBounds.center);
            Assert.Less(distanceAfterAttraction, distanceBeforeAttraction, "The magnet should pull coins toward the runner in three dimensions.");

            var boost = pool.First(i => i.kind == RunnerItemKind.SpeedBoost);
            boost.Place(0f, 0f);
            world.Simulate(0f, player.HitBounds, player.HitBounds);
            Assert.AreEqual(RunnerRules.SpeedBoostDuration, game.BoostRemaining);
            Assert.AreEqual(game.Speed + RunnerRules.SpeedBoostBonus, game.TravelSpeed);
            var shield = pool.First(i => i.kind == RunnerItemKind.Shield);
            shield.Place(0f, 0f);
            world.Simulate(0f, player.HitBounds, player.HitBounds);
            Assert.AreEqual(RunnerRules.ShieldDuration, game.ShieldRemaining);
            var absorbedTower = pool.First(i => i.kind == RunnerItemKind.Tower);
            absorbedTower.Place(0f, 0f);
            world.Simulate(0f, player.HitBounds, player.HitBounds);
            Assert.AreEqual(RunnerGame.RunState.Running, game.State);
            Assert.AreEqual(0f, game.ShieldRemaining);

            var tower = pool.First(i => i.kind == RunnerItemKind.Tower);
            tower.Place(0f, 1f);
            gameOverEvents = 0;
            trackedGame = game;
            game.StateChanged += CountGameOver;
            world.Simulate(2f, player.HitBounds, player.HitBounds);
            game.EndRun();
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
            Assert.AreEqual(RunnerHud.GameOverTitle, hud.gameOverTitleText.text);
            Assert.AreEqual(1, gameOverEvents);
            game.StateChanged -= CountGameOver;
            var shop = hud.GetComponent<RunnerShop>();
            shop.openFromResults.onClick.Invoke();
            Assert.AreEqual(RunnerGame.RunState.Shop, game.State);
            Assert.AreEqual(RunnerProgress.SkinNames.Length, shop.skinButtons.Length);
            Assert.IsFalse(shop.skinButtons[0].interactable, "The starter skin should remain equipped.");
            shop.skinButtons[0].onClick.Invoke();
            Assert.AreEqual(1, game.Progress.Wallet, "The starter skin should stay free and never charge coins.");
            Assert.IsNotNull(player.GetComponentInChildren<Animator>());
            shop.close.onClick.Invoke();
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
            hud.restartButton.onClick.Invoke();
            Assert.AreEqual(0, game.Coins); Assert.AreEqual(0, game.Score);
            Assert.AreEqual(1, player.Lane); Assert.IsFalse(player.IsSliding);
            Assert.AreEqual(RunnerRules.StartSpeed, game.Speed); Assert.AreEqual(1f, Time.timeScale);
            for (int i = 0; i < 150; i++) game.SendMessage("FixedUpdate");
            Assert.AreEqual(RunnerGame.RunState.Running, game.State, "The chase must also finish without skipping.");
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
            game.CompleteIntro();

            // A hidden batch Editor has no focused Game view. Route virtual devices to the
            // player for this test only, then restore the user's input settings in teardown.
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackgroundInput = InputSystem.settings.backgroundBehavior;
            changedInputSettings = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var touchscreen = syntheticTouchscreen = InputSystem.AddDevice<Touchscreen>();
            Assert.AreEqual(RunnerGame.RunState.Running, game.State);
            Assert.IsTrue(UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled);
            Vector2 origin = new Vector2(Screen.width * 0.5f, Screen.height * 0.3f);
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Began, origin);
            yield return null;
            yield return null;
            yield return null;
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Moved, origin + Vector2.right * Screen.width * 0.1f);
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(2, player.Lane, "Touch should trigger before finger release.");
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Moved, origin - Vector2.right * Screen.width * 0.2f);
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(2, player.Lane, "A touch may only dispatch one action.");
            QueueTouch(touchscreen, 1, UnityEngine.InputSystem.TouchPhase.Canceled, origin);
            yield return null;
            yield return null;
            yield return null;
            QueueTouch(touchscreen, 2, UnityEngine.InputSystem.TouchPhase.Began, origin);
            yield return null;
            yield return null;
            yield return null;
            QueueTouch(touchscreen, 2, UnityEngine.InputSystem.TouchPhase.Moved, origin - Vector2.right * Screen.width * 0.1f);
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(1, player.Lane, "Canceled touch must not lock the next finger.");
            QueueTouch(touchscreen, 2, UnityEngine.InputSystem.TouchPhase.Ended, origin);
            yield return null;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Vector2 buttonPoint = RectTransformUtility.WorldToScreenPoint(null, hud.pauseButton.transform.position);
            QueueTouch(touchscreen, 3, UnityEngine.InputSystem.TouchPhase.Began, buttonPoint);
            yield return null;
            yield return null;
            yield return null;
            QueueTouch(touchscreen, 3, UnityEngine.InputSystem.TouchPhase.Moved, buttonPoint - Vector2.right * Screen.width * 0.2f);
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(1, player.Lane, "Gestures starting on UI must not change lanes.");
            QueueTouch(touchscreen, 3, UnityEngine.InputSystem.TouchPhase.Canceled, buttonPoint);
            yield return null;
            yield return null;
            yield return null;
            QueueTouch(touchscreen, 4, UnityEngine.InputSystem.TouchPhase.Began, buttonPoint);
            yield return null;
            yield return null;
            yield return null;
            QueueTouch(touchscreen, 4, UnityEngine.InputSystem.TouchPhase.Ended, buttonPoint);
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(RunnerGame.RunState.Paused, game.State, "A touch tap must operate the serialized UI actions.");
            game.SendMessage("OnApplicationFocus", true);
            game.SendMessage("OnApplicationPause", false);
            Canvas.ForceUpdateCanvases();
            // Exercise the wired Resume event directly. Physical taps on newly opened
            // modal UI still belong to the device checklist (batch raycasts can be empty).
            hud.resumeButton.onClick.Invoke();
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
            IsolateWallet(game);
            game.enabled = false;
            game.StartRun();
            game.CompleteIntro();
            var world = game.world;
            var items = world.GetComponentsInChildren<RunnerItem>(true);
            int initial = world.transform.childCount;
            var away = new Bounds(new Vector3(100f, 0f, 0f), Vector3.one);
            typeof(RunnerGame).GetProperty("Speed").SetValue(game, RunnerRules.MaxSpeed);
            // 110 km at the maximum speed, far beyond float-origin trouble for an unre-based runner.
            for (int step = 0; step < 100000; step++)
            {
                world.Simulate(1.1f, away, away);
                if (step % 1000 != 0) continue;
                Assert.AreEqual(initial, world.transform.childCount);
                Assert.LessOrEqual(world.ActiveItemCount, RunnerRules.ItemKindCount * RunnerRules.PoolPerKind);
                foreach (var item in items)
                {
                    if (!item.InUse) continue;
                    Assert.That(item.transform.position.z, Is.InRange(-12f, RunnerRules.Horizon + 4f));
                    if (item.kind != RunnerItemKind.Coin) continue;
                    foreach (var other in items)
                        if (other.InUse && RunnerRules.IsObstacle(other.kind))
                            Assert.IsFalse(item.HitBounds.Intersects(other.HitBounds),
                                $"Coin {item.transform.position} intersects {other.kind} {other.transform.position} at step {step}");
                }
            }
            game.Pause(); game.StartRun();
            Assert.AreEqual(initial, world.transform.childCount);
            Assert.AreEqual(0d, game.Distance);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator MenuReturnsResetRunAndKeepWalletWithoutPurchases()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            yield return new EnterPlayMode();
            var game = Object.FindAnyObjectByType<RunnerGame>();
            IsolateWallet(game); game.enabled = false;
            game.SendMessage("OnApplicationFocus", true); game.SendMessage("OnApplicationPause", false);
            var hud = Object.FindAnyObjectByType<RunnerHud>();
            var presentation = game.GetComponent<RunnerPresentation>();
            Assert.IsTrue(presentation.stage.activeSelf);
            Assert.IsFalse(game.world.gameObject.activeSelf);
            game.OpenShop();
            Assert.IsTrue(presentation.stage.activeSelf);
            game.CloseShop();
            for (int mode = 0; mode < 4; mode++)
            {
                game.StartRun();
                Assert.AreEqual(Quaternion.identity, game.player.visual.localRotation);
                Assert.IsFalse(presentation.stage.activeSelf);
                if (mode != 0) game.CompleteIntro();
                if (mode == 2) { game.CollectCoin(); game.player.Slide(); game.Pause(); hud.pausedMenuButton.onClick.Invoke(); }
                else if (mode == 3) { game.EndRun(); hud.resultsMenuButton.onClick.Invoke(); }
                else hud.menuButton.onClick.Invoke();
                Assert.AreEqual(RunnerGame.RunState.Ready, game.State);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.AreEqual(0d, game.Distance);
                Assert.AreEqual(0, game.Coins);
                Assert.AreEqual(1, game.player.Lane);
                Assert.IsFalse(game.player.IsSliding);
                Assert.IsTrue(presentation.stage.activeSelf);
                Assert.IsFalse(game.world.gameObject.activeSelf);
                Assert.IsFalse(game.chase.officer.gameObject.activeSelf);
            }
            Assert.AreEqual(1, game.Progress.Wallet);
            Assert.AreEqual(1, new RunnerProgress(TestSave).Wallet);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [Test]
        public void WalletPurchasesPersistAndNeverChargeOwnedSkinsTwice()
        {
            PlayerPrefs.DeleteKey(TestSave);
            var progress = new RunnerProgress(TestSave);
            Assert.IsTrue(progress.Owns(0));
            Assert.IsFalse(progress.BuyOrEquip(1));
            Assert.IsFalse(progress.BuyOrEquip(-1));
            for (int i = 0; i < 45; i++) progress.EarnCoin();
            Assert.IsTrue(progress.BuyOrEquip(1));
            Assert.AreEqual(5, progress.Wallet);
            progress = new RunnerProgress(TestSave);
            Assert.AreEqual(1, progress.Selected);
            Assert.IsTrue(progress.BuyOrEquip(0));
            Assert.IsTrue(progress.BuyOrEquip(1));
            Assert.AreEqual(5, progress.Wallet);
            PlayerPrefs.SetString(TestSave, "{\"coins\":-10,\"owned\":0,\"selected\":99}");
            progress = new RunnerProgress(TestSave);
            Assert.AreEqual(0, progress.Wallet);
            Assert.AreEqual(0, progress.Selected);
            Assert.IsTrue(progress.Owns(0));
        }

        [Test]
        public void MissionProgressPersistsAndPaysOnceBeforeRotating()
        {
            PlayerPrefs.DeleteKey(TestSave);
            var progress = new RunnerProgress(TestSave);
            for (int i = 0; i < 12; i++) Assert.AreEqual(0, progress.AdvanceMission(RunnerMissionKind.CollectCoins));
            progress.Save();

            progress = new RunnerProgress(TestSave);
            Assert.AreEqual(RunnerMissionKind.CollectCoins, progress.ActiveMission);
            Assert.AreEqual(12, progress.ActiveMissionProgress);
            Assert.AreEqual(0, progress.AdvanceMission(RunnerMissionKind.DodgeObstacles));
            for (int i = 12; i < 29; i++) Assert.AreEqual(0, progress.AdvanceMission(RunnerMissionKind.CollectCoins));
            Assert.AreEqual(RunnerProgress.MissionReward, progress.AdvanceMission(RunnerMissionKind.CollectCoins));
            Assert.AreEqual(RunnerProgress.MissionReward, progress.Wallet);
            Assert.AreEqual(RunnerMissionKind.DodgeObstacles, progress.ActiveMission);
            Assert.AreEqual(0, progress.ActiveMissionProgress);

            progress = new RunnerProgress(TestSave);
            Assert.AreEqual(RunnerProgress.MissionReward, progress.Wallet);
            Assert.AreEqual(0, progress.AdvanceMission(RunnerMissionKind.CollectCoins));
            PlayerPrefs.DeleteKey(TestSave);
        }

        [Test]
        public void MethodModelsMatchRoadCoinAndSlideConventions()
        {
            var coin = AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/Coin.prefab");
            Assert.AreEqual(0.9f, BlenderPackIntegration.VisualBounds(coin).center.y, .001f);
            var road = AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/RoadSection.prefab");
            var roadModel = road.transform.Find("RoadSection");
            var roadBounds = BlenderPackIntegration.VisualBounds(roadModel.gameObject);
            Assert.AreEqual(0f, roadModel.localPosition.z + roadBounds.center.z, .001f);
            Assert.AreEqual(24f, roadBounds.size.z, .001f);
            bool integrated = AssetDatabase.LoadAssetAtPath<GameObject>(BlenderPackIntegration.PrefabPath("Tsila")) != null;
            string art = integrated ? BlenderPackIntegration.Generated + "/" : MethodVisualBuilder.Art;
            var character = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(art + "Tsila.prefab"));
            try
            {
                var slide = AssetDatabase.LoadAssetAtPath<AnimationClip>(art + "Tsila_Slide.anim");
                slide.SampleAnimation(character, .4f);
                var skin = character.GetComponentInChildren<SkinnedMeshRenderer>();
                Bounds low = BlenderPackIntegration.SkinnedBounds(character, skin);
                Assert.GreaterOrEqual(low.min.y, -.02f, "Slide must stay above the road.");
                Assert.LessOrEqual(low.max.y, .72f, "Slide mesh must fit beneath the beam.");
                slide.SampleAnimation(character, 1f);
                Bounds held = BlenderPackIntegration.SkinnedBounds(character, skin);
                Assert.AreEqual(low.center.y, held.center.y, .001f, "Hold the pose until gameplay allows standing.");
                Debug.Log("TSILA_SLIDE_BOUNDS " + held);
            }
            finally { Object.DestroyImmediate(character); }
        }

        [UnityTest]
        public IEnumerator AnimatedPersonCollidesAndPauseFreezesLimbs()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            yield return new EnterPlayMode();
            var game = Object.FindAnyObjectByType<RunnerGame>();
            IsolateWallet(game);
            game.enabled = false;
            game.SendMessage("OnApplicationFocus", true);
            game.SendMessage("OnApplicationPause", false);
            game.StartRun(); game.CompleteIntro();
            var avatar = game.player.GetComponentInChildren<RunnerAvatar>();
            Assert.AreSame(game, avatar.game);
            Assert.AreSame(game.player, avatar.player);
            avatar.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Quaternion before = avatar.leftArm.localRotation;
            avatar.Animate(0.1f); // Drive the actual imported Animator deterministically.
            avatar.animator.Update(0.15f);
            Assert.AreEqual(Animator.StringToHash("Run"), avatar.CurrentAnimation);
            Assert.Greater(Quaternion.Angle(before, avatar.leftArm.localRotation), 0.1f);
            game.Pause();
            before = avatar.leftArm.localRotation;
            avatar.Animate(0.1f);
            Assert.Less(Quaternion.Angle(before, avatar.leftArm.localRotation), 0.001f);
            game.Resume();
            game.player.Jump(); game.player.Simulate(0.1f); avatar.Animate(0.1f); avatar.animator.Update(0.12f);
            Assert.AreEqual(Animator.StringToHash("Jump"), avatar.CurrentAnimation);
            game.player.ResetPlayer(); game.player.Slide(); avatar.Animate(0.1f); avatar.animator.Update(0.12f);
            Assert.AreEqual(Animator.StringToHash("Slide"), avatar.CurrentAnimation);
            Assert.AreEqual(Vector3.one, game.player.visual.localScale, "A real slide clip must not squash the model.");
            avatar.animator.Update(1.2f);
            var slideBounds = BlenderPackIntegration.SkinnedBounds(avatar.gameObject, avatar.GetComponentInChildren<SkinnedMeshRenderer>());
            Assert.GreaterOrEqual(slideBounds.min.y, -.02f);
            Assert.LessOrEqual(slideBounds.max.y, .72f, "Runtime Animator must preserve the low pose too.");
            game.player.ResetPlayer();
            var items = game.world.GetComponentsInChildren<RunnerItem>(true);
            foreach (var item in items) item.Release();
            var person = items.First(i => i.kind == RunnerItemKind.RunningPerson);
            person.Place(0f, 1f);
            game.world.Simulate(2f, game.player.HitBounds, game.player.HitBounds);
            Assert.AreEqual(RunnerGame.RunState.GameOver, game.State);
            game.StartRun();
            Assert.AreEqual(0d, game.Distance);
            Assert.AreEqual(0, game.world.GetComponentInChildren<RunnerRoadSection>().Zone);
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }

        [Test]
        public void SceneryCyclesAndMovingPeopleHaveBoundedDrift()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var world = Object.FindAnyObjectByType<RunnerWorld>();
            world.ResetWorld(42);
            var section = world.GetComponentInChildren<RunnerRoadSection>();
            for (int i = 0; i < 12; i++)
            {
                section.SetLocation(i * RunnerRoadSection.ZoneLength);
                Assert.AreEqual(i % 3, section.Zone);
                Assert.AreEqual(1, section.scenery.Count(s => s.activeSelf));
            }
            var person = world.GetComponentsInChildren<RunnerItem>(true).First(i => i.kind == RunnerItemKind.RunningPerson);
            person.Place(0f, 100f);
            float drift = 0f;
            for (int i = 0; i < 1000; i++) drift += 1f - person.TravelThisTick(1f, 0.1f);
            Assert.AreEqual(RunnerRules.PersonDriftBudget, drift, 0.001f);
            Assert.Greater(person.HitBounds.size.y, 1.8f);
            Assert.IsNotNull(person.GetComponentInChildren<RunnerAvatar>().leftArm);
        }

        static void QueueTouch(Touchscreen device, int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 point)
        {
            InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase, position = point });
            // EditMode coroutines do not guarantee a player input/update tick between yields.
            // Drive the real input backend and polling method in a deterministic order.
            InputSystem.Update();
            Object.FindAnyObjectByType<RunnerInput>().SendMessage("Update");
            var module = UnityEngine.EventSystems.EventSystem.current.currentInputModule;
            if (module != null) { module.UpdateModule(); module.Process(); }
        }

        [Test, Order(99)]
        public void PortraitScreensRenderWithoutClippedText()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Portrait rendering requires an Editor graphics device; rerun without -nographics.");
            PrototypeScreenshots.CaptureBatch();
        }
    }
}
