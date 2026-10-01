using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun
{
    public sealed class RunnerHud : MonoBehaviour
    {
        public RunnerGame game;
        public GameObject startPanel, pausePanel, gameOverPanel, hud;
        public Button playButton, pauseButton, resumeButton, pausedRestartButton, restartButton;
        public Text distanceText, coinsText, bestText, resultText;
        public Text missionText, powerUpText;
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
            if (missionText == null) missionText = CreateStatusText("Mission Status", new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.825f));
            if (powerUpText == null) powerUpText = CreateStatusText("Power Up Status", new Vector2(0.1f, 0.73f), new Vector2(0.9f, 0.775f));
            ApplyModernMobileStyle();
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
            if (playButton != null) playButton.onClick.AddListener(game.StartRun);
            if (skipIntroButton != null) skipIntroButton.onClick.AddListener(game.CompleteIntro);
            if (pauseButton != null) pauseButton.onClick.AddListener(game.Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(game.Resume);
            if (pausedRestartButton != null) pausedRestartButton.onClick.AddListener(game.StartRun);
            if (restartButton != null) restartButton.onClick.AddListener(game.StartRun);
            if (menuButton != null) menuButton.onClick.AddListener(game.ReturnToMenu);
            if (pausedMenuButton != null) pausedMenuButton.onClick.AddListener(game.ReturnToMenu);
            if (resultsMenuButton != null) resultsMenuButton.onClick.AddListener(game.ReturnToMenu);
            game.StateChanged += RefreshState;
            game.MissionCompleted += ShowMissionComplete;
        }

        void Start() { RefreshState(); }

        void ApplyModernMobileStyle()
        {
            StyleButton(playButton, new Color32(20, 96, 255, 255), new Color32(245, 248, 255, 255));
            StyleButton(pauseButton, new Color32(18, 25, 44, 255), new Color32(212, 220, 255, 255));
            StyleButton(resumeButton, new Color32(20, 96, 255, 255), new Color32(245, 248, 255, 255));
            StyleButton(pausedRestartButton, new Color32(18, 25, 44, 255), new Color32(212, 220, 255, 255));
            StyleButton(restartButton, new Color32(20, 96, 255, 255), new Color32(245, 248, 255, 255));
            StyleButton(menuButton, new Color32(15, 140, 120, 255), new Color32(236, 255, 246, 255));
            StyleButton(pausedMenuButton, new Color32(15, 140, 120, 255), new Color32(236, 255, 246, 255));
            StyleButton(resultsMenuButton, new Color32(15, 140, 120, 255), new Color32(236, 255, 246, 255));
            if (skipIntroButton != null) StyleButton(skipIntroButton, new Color32(64, 72, 92, 255), new Color32(255, 255, 255, 255));

            StylePanel(startPanel, new Color32(9, 16, 33, 210));
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

            if (distanceText != null) AddShadow(distanceText, new Color32(12, 22, 45, 140), 2, 2);
            if (coinsText != null) AddShadow(coinsText, new Color32(45, 22, 0, 160), 2, 2);
            if (bestText != null) AddShadow(bestText, new Color32(15, 28, 54, 170), 2, 2);
            if (zoneText != null) AddShadow(zoneText, new Color32(38, 22, 0, 170), 2, 2);
            if (resultText != null) AddShadow(resultText, new Color32(0, 0, 0, 100), 2, -2);
            if (missionText != null) AddShadow(missionText, new Color32(0, 0, 0, 180), 2, 2);
            if (powerUpText != null) AddShadow(powerUpText, new Color32(0, 0, 0, 180), 2, 2);
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
            colors.normalColor = fill;
            colors.highlightedColor = new Color(Mathf.Min(1f, fill.r * 1.14f), Mathf.Min(1f, fill.g * 1.14f), Mathf.Min(1f, fill.b * 1.14f), 1f);
            colors.pressedColor = new Color(fill.r * 0.76f, fill.g * 0.76f, fill.b * 0.76f, 1f);
            colors.selectedColor = fill;
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
                text.resizeTextMaxSize = 30;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(rect.rect.width, 256f));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(rect.rect.height, 68f));
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
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
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
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
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
            game.StateChanged -= RefreshState;
            game.MissionCompleted -= ShowMissionComplete;
            if (playButton != null) playButton.onClick.RemoveListener(game.StartRun);
            if (skipIntroButton != null) skipIntroButton.onClick.RemoveListener(game.CompleteIntro);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(game.Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(game.Resume);
            if (pausedRestartButton != null) pausedRestartButton.onClick.RemoveListener(game.StartRun);
            if (restartButton != null) restartButton.onClick.RemoveListener(game.StartRun);
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
            if (shownDistance != game.Score) { shownDistance = game.Score; distanceText.text = shownDistance + " m"; }
            if (shownCoins != game.Coins) { shownCoins = game.Coins; coinsText.text = "COINS  " + shownCoins; }
            if (shownBest != game.Best) { shownBest = game.Best; bestText.text = "BEST  " + shownBest + " m"; }
            int zone = RunnerRoadSection.ZoneAt(game.Distance);
            if (zoneText != null && zone != shownZone) { shownZone = zone; zoneText.text = RunnerRoadSection.ZoneNames[zone]; }
            bool running = game.State == RunnerGame.RunState.Running;
            if (missionText != null)
            {
                missionText.gameObject.SetActive(running);
                if (running && game.Progress != null)
                {
                    missionText.text = Time.time < missionBannerUntil
                        ? missionBanner
                        : "MISSION  " + game.Progress.ActiveMissionName + "  " + game.Progress.ActiveMissionProgress + "/" + game.Progress.ActiveMissionTarget + "  +" + RunnerProgress.MissionReward;
                }
            }
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

        void RefreshState()
        {
            if (startPanel != null) startPanel.SetActive(game.State == RunnerGame.RunState.Ready);
            if (pausePanel != null) pausePanel.SetActive(game.State == RunnerGame.RunState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(game.State == RunnerGame.RunState.GameOver);
            if (hud != null) hud.SetActive(game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop);
            if (zoneText != null) zoneText.gameObject.SetActive(game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop);
            if (missionText != null) missionText.gameObject.SetActive(game.State == RunnerGame.RunState.Running);
            if (powerUpText != null) powerUpText.gameObject.SetActive(game.State == RunnerGame.RunState.Running && game.PowerUpStatus.Length > 0);
            if (introPanel != null) introPanel.SetActive(game.State == RunnerGame.RunState.Intro);
            if (pauseButton != null) pauseButton.gameObject.SetActive(game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro);
            if (game.State == RunnerGame.RunState.GameOver && resultText != null)
                resultText.text = game.Score + " METRES\n" + game.Coins + " COINS\nBEST  " + game.Best + " m";
            RefreshNumbers();
        }
    }
}
