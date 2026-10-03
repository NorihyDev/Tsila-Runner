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
        void OnEnable() { continueButton.onClick.AddListener(Continue); }
        void OnDisable() { continueButton.onClick.RemoveListener(Continue); }
        public void Play()
        {
            if (PlayerPrefs.GetInt(PreferenceKey, 0) == 0) panel.SetActive(true);
            else game.StartRun();
        }
        void Continue()
        {
            PlayerPrefs.SetInt(PreferenceKey, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
            game.StartRun();
        }
    }
}
