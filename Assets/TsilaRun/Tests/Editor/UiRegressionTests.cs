using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TsilaRun.Tests
{
    public sealed class UiRegressionTests
    {
        [TestCase(0, 1920)]
        [TestCase(1080, 0)]
        [TestCase(-1, 1920)]
        public void SafeAreaFallsBackDuringResolutionChanges(int width, int height)
        {
            SafeAreaPanel.NormalizedAnchors(new Rect(0, 0, 1080, 1920), width, height, out var min, out var max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void SafeAreaClampsStaleScreenBounds()
        {
            SafeAreaPanel.NormalizedAnchors(new Rect(-20, 24, 2200, 4000), 1080, 1920, out var min, out var max);
            Assert.AreEqual(new Vector2(0f, 24f / 1920f), min);
            Assert.AreEqual(Vector2.one, max);
            SafeAreaPanel.NormalizedAnchors(Rect.zero, 1080, 1920, out min, out max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void FinishedEntranceRestoresPositionAndStaysVisible()
        {
            var panel = new GameObject("Entrance", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                var rect = panel.GetComponent<RectTransform>();
                var rest = new Vector2(45f, 18f);
                rect.anchoredPosition = rest;
                var entrance = panel.AddComponent<UiEntrance>();
                entrance.delay = .3f;
                entrance.offset = 45f;
                entrance.SendMessage("OnEnable");
                Assert.AreEqual(rest + Vector2.up * 45f, rect.anchoredPosition);
                entrance.Finish();
                entrance.SendMessage("Update");
                Assert.AreEqual(rest, rect.anchoredPosition);
                Assert.AreEqual(1f, panel.GetComponent<CanvasGroup>().alpha);
                entrance.SendMessage("OnDisable");
                entrance.SendMessage("OnEnable");
                Assert.AreEqual(0f, panel.GetComponent<CanvasGroup>().alpha, "Reopening the menu starts a fresh entrance.");
            }
            finally { Object.DestroyImmediate(panel); }
        }

        [Test]
        public void SevenShopRowsHaveGapsAndHideTheShowcase()
        {
            var canvas = new GameObject("Shop UI", typeof(RectTransform));
            try
            {
                var shop = canvas.AddComponent<RunnerShop>();
                shop.panel = new GameObject("Shop", typeof(RectTransform));
                shop.panel.transform.SetParent(canvas.transform, false);
                shop.skinButtons = new Button[7];
                shop.skinLabels = new Text[0];
                for (int i = 0; i < shop.skinButtons.Length; i++)
                {
                    var row = new GameObject("Skin " + i, typeof(RectTransform), typeof(Image), typeof(Button));
                    row.transform.SetParent(shop.panel.transform, false);
                    shop.skinButtons[i] = row.GetComponent<Button>();
                }
                shop.ApplyModernShopStyle();
                var background = shop.panel.GetComponent<Image>();
                Assert.AreEqual(1f, background.color.a);
                Assert.IsTrue(background.raycastTarget);
                for (int i = 1; i < shop.skinButtons.Length; i++)
                {
                    var previous = (RectTransform)shop.skinButtons[i - 1].transform;
                    var current = (RectTransform)shop.skinButtons[i].transform;
                    Assert.Less(current.anchorMax.y, previous.anchorMin.y, "Catalog rows must not overlap.");
                    Assert.Greater(current.anchorMin.y, .145f, "Keep every row above Back.");
                }
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        [Test]
        public void ShopKeepsOtherButtonListenersAcrossEnableCycles()
        {
            var canvas = new GameObject("Shop UI", typeof(RectTransform));
            try
            {
                var shop = canvas.AddComponent<RunnerShop>();
                var row = new GameObject("Skin", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(canvas.transform, false);
                shop.skinButtons = new[] { row.GetComponent<Button>() };
                int observed = 0;
                shop.skinButtons[0].onClick.AddListener(() => observed++);
                shop.SendMessage("Awake");
                shop.SendMessage("OnEnable");
                shop.skinButtons[0].onClick.Invoke();
                shop.SendMessage("OnDisable");
                shop.SendMessage("OnEnable");
                shop.skinButtons[0].onClick.Invoke();
                Assert.AreEqual(2, observed);
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        [Test]
        public void SpeedEffectsLoopAndClearWithoutRestartingWhileStopped()
        {
            var owner = new GameObject("Game");
            try
            {
                var game = owner.AddComponent<RunnerGame>();
                var effects = owner.AddComponent<RunnerVfx>();
                effects.Bind(game);
                Assert.IsTrue(effects.speedTrail.main.loop);
                Assert.IsFalse(effects.speedTrail.main.playOnAwake);
                effects.SetRunning(true);
                Assert.IsTrue(effects.speedTrail.isPlaying);
                effects.SetRunning(false);
                effects.SetRunning(false);
                Assert.IsFalse(effects.speedTrail.isPlaying);
                effects.SpawnCrashBurst(Vector3.zero);
                effects.SpawnCoinBurst(Vector3.zero);
                Assert.IsTrue(effects.crashBurst.isPlaying);
                Assert.IsTrue(effects.coinBurst.isPlaying);
                Assert.Greater(effects.crashBurst.particleCount, 0);
                Assert.Greater(effects.coinBurst.particleCount, 0);
                effects.ClearEffects();
                foreach (var system in owner.GetComponentsInChildren<ParticleSystem>())
                {
                    Assert.AreEqual(0, system.particleCount);
                    Assert.IsFalse(system.isPlaying);
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
