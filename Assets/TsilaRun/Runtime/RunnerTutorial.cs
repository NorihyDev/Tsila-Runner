using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun
{
    public sealed class RunnerTutorial : MonoBehaviour
    {
        public const string PreferenceKey = "TsilaRun.ControlsSeen.v1";
        public RunnerGame game;
        public GameObject panel;
        public Button continueButton;
        void OnEnable()
        {
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (game != null) game.StateChanged += Refresh;
        }
        void OnDisable()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
            if (game != null) game.StateChanged -= Refresh;
            if (panel != null) panel.SetActive(false);
        }
        void Refresh()
        {
            if (panel != null && (game == null || game.State != RunnerGame.RunState.Ready)) panel.SetActive(false);
        }
        public void Play()
        {
            if (game == null || game.State != RunnerGame.RunState.Ready) return;
            if (PlayerPrefs.GetInt(PreferenceKey, 0) == 0 && panel != null && continueButton != null) panel.SetActive(true);
            else game.StartRun();
        }
        void Continue()
        {
            if (game == null || game.State != RunnerGame.RunState.Ready || panel == null || !panel.activeSelf) return;
            PlayerPrefs.SetInt(PreferenceKey, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
            game.StartRun();
        }
    }
}
