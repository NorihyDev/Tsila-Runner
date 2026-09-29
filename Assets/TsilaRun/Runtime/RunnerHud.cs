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
