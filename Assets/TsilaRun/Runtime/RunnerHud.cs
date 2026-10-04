using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun
{
    public sealed class RunnerHud : MonoBehaviour
    {
        public const string GameOverTitle = "Hay lery maty";
        public RunnerGame game;
        public GameObject startPanel, pausePanel, gameOverPanel, hud;
        public Button playButton, pauseButton, resumeButton, pausedRestartButton, restartButton;
        public Button volumeDownButton, volumeUpButton, fpsButton;
        public Text distanceText, coinsText, bestText, resultText, gameOverTitleText;
        public Text missionText, powerUpText, menuMissionText, pauseMissionText;
        public Text settingsText;
        public GameObject introPanel;
        public Button skipIntroButton;
        public Button menuButton, pausedMenuButton, resultsMenuButton;
        public Text zoneText;
        int shownDistance = -1, shownCoins = -1, shownBest = -1;
        int shownZone = -1;
        float nextRefresh;
        float missionBannerUntil;
        string missionBanner = "";

        void Awake()
        {
            EnsureBrandMark();
            UpdateLegacyIntroCopy();
            if (missionText == null) missionText = CreateStatusText("Mission Status", new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.825f));
            if (powerUpText == null) powerUpText = CreateStatusText("Power Up Status", new Vector2(0.1f, 0.73f), new Vector2(0.9f, 0.775f));
            ApplyMobileLayout();
            ApplyModernMobileStyle();
        }

        public void ApplyMobileLayout()
        {
            SetAnchors(hud, new Vector2(0.035f, 0.915f), new Vector2(0.965f, 0.99f));
            SetAnchors(distanceText, new Vector2(0.04f, 0.1f), new Vector2(0.44f, 0.9f));
            SetAnchors(coinsText, new Vector2(0.46f, 0.1f), new Vector2(0.78f, 0.9f));
            if (bestText != null) bestText.gameObject.SetActive(false);
            if (menuButton != null) menuButton.gameObject.SetActive(false);
            SetAnchors(pauseButton, new Vector2(0.83f, 0.15f), new Vector2(0.97f, 0.85f));
            SetAnchors(missionText, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.90f));
            SetAnchors(powerUpText, new Vector2(0.12f, 0.075f), new Vector2(0.88f, 0.11f));
            SetAnchors(zoneText, new Vector2(0.1f, 0.865f), new Vector2(0.9f, 0.90f));
            SetAnchors(introPanel, new Vector2(0.16f, 0.82f), new Vector2(0.84f, 0.90f));
            if (introPanel != null)
            {
                Transform caption = introPanel.transform.Find("Caption");
                SetAnchors(caption != null ? caption.GetComponent<RectTransform>() : null,
                    new Vector2(0.04f, 0.08f), new Vector2(0.68f, 0.92f));
            }
        }

        static void SetAnchors(GameObject target, Vector2 min, Vector2 max)
        {
            if (target != null) SetAnchors(target.GetComponent<RectTransform>(), min, max);
        }

        static void SetAnchors(Component target, Vector2 min, Vector2 max)
        {
            if (target != null) SetAnchors(target.GetComponent<RectTransform>(), min, max);
        }

        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        void UpdateLegacyIntroCopy()
        {
            if (introPanel == null) return;
            Transform caption = introPanel.transform.Find("Caption");
            Text captionText = caption != null ? caption.GetComponent<Text>() : null;
            if (captionText != null) captionText.text = "READY?";
            if (skipIntroButton != null)
            {
                Text buttonText = skipIntroButton.GetComponentInChildren<Text>();
                if (buttonText != null) buttonText.text = "SKIP";
            }
        }

        void EnsureBrandMark()
        {
            Transform frame = transform.Find("Safe Area/Method Logo Frame");
            if (frame == null) return;
            var logo = frame.GetComponentInChildren<RawImage>(true);
            if (logo != null && logo.texture != null) { logo.enabled = true; return; }
            var label = frame.GetComponent<Text>() ?? frame.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "METHOD";
            label.fontSize = 19;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color32(105, 239, 195, 255);
            label.alignment = TextAnchor.MiddleRight;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 19;
        }

        Text CreateStatusText(string objectName, Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform safeArea = transform.Find("Safe Area");
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(safeArea != null ? safeArea : transform, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 17;
            text.raycastTarget = false;
            return text;
        }

        void OnEnable()
        {
            if (game == null) return;
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (skipIntroButton != null) skipIntroButton.onClick.AddListener(game.CompleteIntro);
            if (pauseButton != null) pauseButton.onClick.AddListener(game.Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(game.Resume);
            if (pausedRestartButton != null) pausedRestartButton.onClick.AddListener(game.StartRun);
            if (restartButton != null) restartButton.onClick.AddListener(game.StartRun);
            if (volumeDownButton != null) volumeDownButton.onClick.AddListener(VolumeDown);
            if (volumeUpButton != null) volumeUpButton.onClick.AddListener(VolumeUp);
            if (fpsButton != null) fpsButton.onClick.AddListener(ToggleFps);
            if (menuButton != null) menuButton.onClick.AddListener(game.ReturnToMenu);
            if (pausedMenuButton != null) pausedMenuButton.onClick.AddListener(game.ReturnToMenu);
            if (resultsMenuButton != null) resultsMenuButton.onClick.AddListener(game.ReturnToMenu);
            game.StateChanged += RefreshState;
            game.MissionCompleted += ShowMissionComplete;
        }

        void Play()
        {
            if (game == null) return;
            var tutorial = GetComponent<RunnerTutorial>();
            if (tutorial != null) tutorial.Play(); else game.StartRun();
        }

        void Start() { RefreshState(); }

        public void ApplyModernMobileStyle()
        {
            Color primary = new Color32(105, 239, 195, 255);
            Color secondary = new Color32(39, 50, 70, 255);
            Color ink = new Color32(13, 19, 31, 255);
            Color pale = new Color32(242, 246, 255, 255);
            StyleButton(playButton, primary, ink);
            StyleButton(pauseButton, secondary, pale);
            StyleButton(resumeButton, primary, ink);
            StyleButton(pausedRestartButton, secondary, pale);
            StyleButton(volumeDownButton, secondary, pale);
            StyleButton(volumeUpButton, secondary, pale);
            StyleButton(fpsButton, secondary, pale);
            StyleButton(restartButton, primary, ink);
            StyleButton(menuButton, secondary, pale);
            StyleButton(pausedMenuButton, secondary, pale);
            StyleButton(resultsMenuButton, secondary, pale);
            if (skipIntroButton != null) StyleButton(skipIntroButton, primary, ink);

            // The menu is a showcase: keep its 3D character clear of a full-screen tint.
            if (startPanel != null && startPanel.TryGetComponent<Image>(out var menuImage)) menuImage.enabled = false;
            StylePanel(pausePanel, new Color32(9, 16, 33, 210));
            StylePanel(gameOverPanel, new Color32(9, 16, 33, 210));
            StylePanel(hud, new Color32(9, 16, 33, 120));
            EnsurePanelMotion(startPanel);
            EnsurePanelMotion(pausePanel);
            EnsurePanelMotion(gameOverPanel);
            EnsurePanelMotion(hud);

            StyleText(distanceText, new Color32(255, 255, 255, 255), 28, true);
            StyleText(coinsText, new Color32(255, 206, 82, 255), 24, true);
            StyleText(bestText, new Color32(168, 207, 255, 255), 24, true);
            StyleText(resultText, new Color32(255, 255, 255, 255), 32, true);
            if (zoneText != null) StyleText(zoneText, new Color32(255, 213, 92, 255), 20, true);
            StyleText(missionText, new Color32(236, 255, 246, 255), 17, true);
            StyleText(powerUpText, new Color32(255, 216, 115, 255), 17, true);
            StyleText(settingsText, new Color32(168, 207, 255, 255), 17, true);

            if (distanceText != null) AddShadow(distanceText, new Color32(12, 22, 45, 140), 2, 2);
            if (coinsText != null) AddShadow(coinsText, new Color32(45, 22, 0, 160), 2, 2);
            if (bestText != null) AddShadow(bestText, new Color32(15, 28, 54, 170), 2, 2);
            if (zoneText != null) AddShadow(zoneText, new Color32(38, 22, 0, 170), 2, 2);
            if (resultText != null) AddShadow(resultText, new Color32(0, 0, 0, 100), 2, -2);
            if (missionText != null) AddShadow(missionText, new Color32(0, 0, 0, 180), 2, 2);
            if (powerUpText != null) AddShadow(powerUpText, new Color32(0, 0, 0, 180), 2, 2);
            if (settingsText != null) AddShadow(settingsText, new Color32(0, 0, 0, 160), 2, 2);
        }

        static void StyleButton(Button button, Color fill, Color textColor)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.color = fill;
            image.raycastTarget = true;
            image.type = Image.Type.Sliced;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.82f, .82f, .82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(fill.r * 0.6f, fill.g * 0.6f, fill.b * 0.6f, 0.7f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (button.gameObject.GetComponent<Outline>() == null)
            {
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(7, 12, 20, 175);
                outline.effectDistance = new Vector2(0f, 5f);
            }

            if (button.gameObject.GetComponent<Shadow>() == null)
            {
                var shadow = button.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color32(8, 16, 28, 180);
                shadow.effectDistance = new Vector2(0f, -8f);
            }

            if (button.gameObject.GetComponent<CanvasGroup>() == null)
                button.gameObject.AddComponent<CanvasGroup>();

            if (button.gameObject.GetComponent<UiButtonMotion>() == null)
                button.gameObject.AddComponent<UiButtonMotion>();

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = textColor;
                text.fontSize = Mathf.Max(text.fontSize, 20);
                text.alignment = TextAnchor.MiddleCenter;
                text.fontStyle = FontStyle.Bold;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 14;
                text.resizeTextMaxSize = 45;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
                rect.localScale = Vector3.one;
            }
        }

        static void StylePanel(GameObject panelObj, Color tone)
        {
            if (panelObj == null) return;
            var image = panelObj.GetComponent<Image>() ?? panelObj.AddComponent<Image>();
            image.color = tone;
            image.raycastTarget = false;
            image.type = Image.Type.Sliced;
            if (panelObj.GetComponent<Outline>() == null)
            {
                var outline = panelObj.AddComponent<Outline>();
                outline.effectColor = new Color32(90, 120, 175, 60);
                outline.effectDistance = new Vector2(0f, 2f);
            }
            if (panelObj.GetComponent<Shadow>() == null)
            {
                var shadow = panelObj.AddComponent<Shadow>();
                shadow.effectColor = new Color32(4, 9, 18, 120);
                shadow.effectDistance = new Vector2(0f, -10f);
            }
            var rect = panelObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        static void EnsurePanelMotion(GameObject panelObj)
        {
            if (panelObj == null) return;
            if (panelObj.GetComponent<CanvasGroup>() == null) panelObj.AddComponent<CanvasGroup>();
            if (panelObj.GetComponent<UiPanelMotion>() == null) panelObj.AddComponent<UiPanelMotion>();
        }

        static void StyleText(Text text, Color color, int size, bool bold)
        {
            if (text == null) return;
            text.color = color;
            text.fontSize = Mathf.RoundToInt(size * 1.5f);
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = Mathf.RoundToInt(size * 1.5f);
        }

        static void AddShadow(Text text, Color shadowColor, float x, float y)
        {
            if (text == null) return;
            var shadow = text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(x, -y);
        }

        void OnDisable()
        {
            if (game == null) return;
            game.StateChanged -= RefreshState;
            game.MissionCompleted -= ShowMissionComplete;
            if (playButton != null) playButton.onClick.RemoveListener(Play);
            if (skipIntroButton != null) skipIntroButton.onClick.RemoveListener(game.CompleteIntro);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(game.Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(game.Resume);
            if (pausedRestartButton != null) pausedRestartButton.onClick.RemoveListener(game.StartRun);
            if (restartButton != null) restartButton.onClick.RemoveListener(game.StartRun);
            if (volumeDownButton != null) volumeDownButton.onClick.RemoveListener(VolumeDown);
            if (volumeUpButton != null) volumeUpButton.onClick.RemoveListener(VolumeUp);
            if (fpsButton != null) fpsButton.onClick.RemoveListener(ToggleFps);
            if (menuButton != null) menuButton.onClick.RemoveListener(game.ReturnToMenu);
            if (pausedMenuButton != null) pausedMenuButton.onClick.RemoveListener(game.ReturnToMenu);
            if (resultsMenuButton != null) resultsMenuButton.onClick.RemoveListener(game.ReturnToMenu);
        }

        void Update()
        {
            // Format score strings only when needed, at most five times/second, not every frame.
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            RefreshNumbers();
        }

        void RefreshNumbers()
        {
            if (game == null) return;
            if (distanceText != null && shownDistance != game.Score) { shownDistance = game.Score; distanceText.text = shownDistance + " m"; }
            if (coinsText != null && shownCoins != game.Coins) { shownCoins = game.Coins; coinsText.text = "COINS  " + shownCoins; }
            if (bestText != null && shownBest != game.Best) { shownBest = game.Best; bestText.text = "BEST  " + shownBest + " m"; }
            RefreshSettingsText();
            int zone = RunnerRoadSection.ZoneAt(game.Distance);
            if (zoneText != null && zone != shownZone) { shownZone = zone; zoneText.text = RunnerRoadSection.ZoneNames[zone]; }
            bool running = game.State == RunnerGame.RunState.Running;
            if (game.Progress != null)
            {
                string mission = game.Progress.ActiveMissionName + "  " + game.Progress.ActiveMissionProgress + "/" + game.Progress.ActiveMissionTarget;
                if (menuMissionText != null) menuMissionText.text = mission + "  +" + RunnerProgress.MissionReward;
                if (pauseMissionText != null) pauseMissionText.text = mission + "\nREWARD  +" + RunnerProgress.MissionReward + " COINS";
            }
            if (missionText != null)
            {
                missionText.gameObject.SetActive(running && Time.time < missionBannerUntil);
                if (running && game.Progress != null)
                {
                    missionText.text = Time.time < missionBannerUntil
                        ? missionBanner
                        : "MISSION  " + game.Progress.ActiveMissionName + "  " + game.Progress.ActiveMissionProgress + "/" + game.Progress.ActiveMissionTarget + "  +" + RunnerProgress.MissionReward;
                }
            }
            if (zoneText != null) zoneText.gameObject.SetActive(running && game.Distance < 3d && Time.time >= missionBannerUntil);
            if (powerUpText != null)
            {
                string status = running ? game.PowerUpStatus : "";
                powerUpText.text = status;
                powerUpText.gameObject.SetActive(status.Length > 0);
            }
        }

        void ShowMissionComplete(int reward)
        {
            missionBanner = "MISSION COMPLETE  +" + reward;
            missionBannerUntil = Time.time + 2.5f;
            RefreshNumbers();
        }

        void VolumeDown()
        {
            if (game.settings == null) return;
            game.settings.VolumeDown();
            RefreshSettingsText();
        }

        void VolumeUp()
        {
            if (game.settings == null) return;
            game.settings.VolumeUp();
            RefreshSettingsText();
        }

        void ToggleFps()
        {
            if (game.settings == null) return;
            game.settings.ToggleFps();
            RefreshSettingsText();
        }

        void RefreshSettingsText()
        {
            if (game == null || game.settings == null) return;
            if (settingsText != null) settingsText.text = "MUSIC " + Mathf.RoundToInt(game.settings.MusicVolume * 100f) + "%   FPS " + game.settings.TargetFps;
            if (fpsButton != null)
            {
                var label = fpsButton.GetComponentInChildren<Text>();
                if (label != null) label.text = (game.settings.TargetFps >= 120 ? 60 : 120) + " FPS";
            }
        }

        void RefreshState()
        {
            if (game == null) return;
            if (game.State == RunnerGame.RunState.Ready || game.State == RunnerGame.RunState.Shop ||
                game.State == RunnerGame.RunState.GameOver || game.State == RunnerGame.RunState.Intro ||
                game.State == RunnerGame.RunState.Running && game.Distance == 0d)
            {
                missionBannerUntil = 0f;
                missionBanner = "";
            }
            if (startPanel != null) startPanel.SetActive(game.State == RunnerGame.RunState.Ready);
            if (pausePanel != null) pausePanel.SetActive(game.State == RunnerGame.RunState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(game.State == RunnerGame.RunState.GameOver);
            if (hud != null) hud.SetActive(game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop);
            if (zoneText != null) zoneText.gameObject.SetActive(game.State == RunnerGame.RunState.Running && game.Distance < 3d);
            if (missionText != null) missionText.gameObject.SetActive(false);
            if (powerUpText != null) powerUpText.gameObject.SetActive(game.State == RunnerGame.RunState.Running && game.PowerUpStatus.Length > 0);
            if (introPanel != null) introPanel.SetActive(game.State == RunnerGame.RunState.Intro);
            if (pauseButton != null) pauseButton.gameObject.SetActive(game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro);
            if (game.State == RunnerGame.RunState.GameOver)
            {
                if (gameOverTitleText == null && gameOverPanel != null)
                {
                    Transform title = gameOverPanel.transform.Find("Title");
                    if (title != null) gameOverTitleText = title.GetComponent<Text>();
                }
                if (gameOverTitleText != null) gameOverTitleText.text = GameOverTitle;
                if (resultText != null) resultText.text = game.Score + " METRES\n" + game.Coins + " COINS\nBEST  " + game.Best + " m";
            }
            RefreshNumbers();
        }
    }
}
