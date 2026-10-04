using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TsilaRun.Tests
{
    public sealed class GameLifecycleRegressionTests
    {
        const string SaveKey = "TsilaRun.Tests.CleanupProgress";

        [TearDown]
        public void Cleanup()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            Time.timeScale = 1f;
        }

        [Test]
        public void RestartAndMenuClearParticlesFromThePreviousRun()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            typeof(RunnerGame).GetProperty("Progress").SetValue(game, new RunnerProgress(SaveKey));
            game.vfx = game.GetComponent<RunnerVfx>() ?? game.gameObject.AddComponent<RunnerVfx>();
            game.vfx.Bind(game);
            game.StartRun();
            game.CompleteIntro();
            game.CollectPowerUp(RunnerItemKind.Shield);
            game.vfx.SpawnCoinBurst(Vector3.zero);
            game.vfx.SpawnCrashBurst(Vector3.zero);
            Assert.Greater(game.vfx.crashBurst.particleCount, 0);

            game.StartRun();
            Assert.AreEqual(0f, game.ShieldRemaining);
            Assert.AreEqual(0, game.vfx.coinBurst.particleCount);
            Assert.AreEqual(0, game.vfx.crashBurst.particleCount);
            Assert.AreEqual(0, game.vfx.powerUpBurst.particleCount);
            Assert.IsTrue(game.vfx.speedTrail.main.loop, "The trail must continue on long runs.");

            game.vfx.SpawnCrashBurst(Vector3.zero);
            game.ReturnToMenu();
            Assert.AreEqual(0, game.vfx.crashBurst.particleCount);
            Assert.AreEqual(0, game.vfx.speedTrail.particleCount);
            Assert.IsFalse(game.vfx.speedTrail.isPlaying);
            Assert.AreEqual(RunnerGame.RunState.Ready, game.State);
        }

        [Test]
        public void NeonEmissionSurvivesUrpMaterialValidation()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/TsilaRun/Art/UI/Stage Neon.mat");
            Assert.IsNotNull(material);
            Assert.AreNotEqual(MaterialGlobalIlluminationFlags.None,
                material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive,
                "URP uses the emissive flag to preserve the _EMISSION shader keyword.");
            Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
        }
    }
}
