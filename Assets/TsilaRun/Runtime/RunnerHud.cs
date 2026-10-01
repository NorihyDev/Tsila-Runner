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
        public GameObject introPanel;
        public Button skipIntroButton;
        public Button menuButton, pausedMenuButton, resultsMenuButton;
        public Text zoneText;
        int shownDistance = -1, shownCoins = -1, shownBest = -1;
        int shownZone = -1;
        float nextRefresh;

        void Awake()
        {
            ApplyModernMobileStyle();
        }

        void OnEnable()
        {
            playButton.onClick.AddListener(game.StartRun);
            if (skipIntroButton != null) skipIntroButton.onClick.AddListener(game.CompleteIntro);
            pauseButton.onClick.AddListener(game.Pause);
            resumeButton.onClick.AddListener(game.Resume);
            pausedRestartButton.onClick.AddListener(game.StartRun);
            restartButton.onClick.AddListener(game.StartRun);
            menuButton.onClick.AddListener(game.ReturnToMenu);
            pausedMenuButton.onClick.AddListener(game.ReturnToMenu);
            resultsMenuButton.onClick.AddListener(game.ReturnToMenu);
            game.StateChanged += RefreshState;
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

            StyleText(distanceText, new Color32(255, 255, 255, 255), 28, true);
            StyleText(coinsText, new Color32(255, 206, 82, 255), 24, true);
            StyleText(bestText, new Color32(168, 207, 255, 255), 24, true);
            StyleText(resultText, new Color32(255, 255, 255, 255), 32, true);
            if (zoneText != null) StyleText(zoneText, new Color32(255, 213, 92, 255), 20, true);

            if (distanceText != null) AddShadow(distanceText, new Color32(12, 22, 45, 140), 2, 2);
            if (coinsText != null) AddShadow(coinsText, new Color32(45, 22, 0, 160), 2, 2);
            if (bestText != null) AddShadow(bestText, new Color32(15, 28, 54, 170), 2, 2);
            if (zoneText != null) AddShadow(zoneText, new Color32(38, 22, 0, 170), 2, 2);
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
            colors.highlightedColor = new Color(fill.r * 1.08f, fill.g * 1.08f, fill.b * 1.08f, 1f);
            colors.pressedColor = new Color(fill.r * 0.82f, fill.g * 0.82f, fill.b * 0.82f, 1f);
            colors.selectedColor = fill;
            colors.disabledColor = new Color(fill.r * 0.6f, fill.g * 0.6f, fill.b * 0.6f, 0.7f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (button.gameObject.GetComponent<Outline>() == null)
            {
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(7, 12, 20, 175);
                outline.effectDistance = new Vector2(0f, 3f);
            }

            if (button.gameObject.GetComponent<UiButtonMotion>() == null)
                button.gameObject.AddComponent<UiButtonMotion>();

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = textColor;
                text.fontSize = Mathf.Max(text.fontSize, 20);
                text.alignment = TextAnchor.MiddleCenter;
                text.fontStyle = FontStyle.Bold;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(rect.rect.width, 260f));
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(rect.rect.height, 62f));
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
            var rect = panelObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        static void StyleText(Text text, Color color, int size, bool bold)
        {
            if (text == null) return;
            text.color = color;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
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
            playButton.onClick.RemoveListener(game.StartRun);
            if (skipIntroButton != null) skipIntroButton.onClick.RemoveListener(game.CompleteIntro);
            pauseButton.onClick.RemoveListener(game.Pause);
            resumeButton.onClick.RemoveListener(game.Resume);
            pausedRestartButton.onClick.RemoveListener(game.StartRun);
            restartButton.onClick.RemoveListener(game.StartRun);
            menuButton.onClick.RemoveListener(game.ReturnToMenu);
            pausedMenuButton.onClick.RemoveListener(game.ReturnToMenu);
            resultsMenuButton.onClick.RemoveListener(game.ReturnToMenu);
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
        }

        void RefreshState()
        {
            startPanel.SetActive(game.State == RunnerGame.RunState.Ready);
            pausePanel.SetActive(game.State == RunnerGame.RunState.Paused);
            gameOverPanel.SetActive(game.State == RunnerGame.RunState.GameOver);
            hud.SetActive(game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop);
            if (zoneText != null) zoneText.gameObject.SetActive(game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop);
            if (introPanel != null) introPanel.SetActive(game.State == RunnerGame.RunState.Intro);
            pauseButton.gameObject.SetActive(game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro);
            if (game.State == RunnerGame.RunState.GameOver)
                resultText.text = game.Score + " METRES\n" + game.Coins + " COINS\nBEST  " + game.Best + " m";
            RefreshNumbers();
        }
    }
}
