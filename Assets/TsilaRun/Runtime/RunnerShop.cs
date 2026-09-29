using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun
{
    public sealed class RunnerShop : MonoBehaviour
    {
        public RunnerGame game;
        public GameObject panel;
        public Button openFromStart, openFromResults, close;
        public Button[] skinButtons;
        public Text[] skinLabels;
        public Text wallet;

        void OnEnable()
        {
            openFromStart.onClick.AddListener(game.OpenShop);
            openFromResults.onClick.AddListener(game.OpenShop);
            close.onClick.AddListener(game.CloseShop);
            game.StateChanged += Refresh;
        }

        void Start() { Refresh(); }

        void OnDisable()
        {
            game.StateChanged -= Refresh;
            openFromStart.onClick.RemoveListener(game.OpenShop);
            openFromResults.onClick.RemoveListener(game.OpenShop);
            close.onClick.RemoveListener(game.CloseShop);
        }

        void Refresh()
        {
            panel.SetActive(game.State == RunnerGame.RunState.Shop);
            if (!panel.activeSelf || game.Progress == null) return;
            wallet.text = "COINS  " + game.Progress.Wallet;
            for (int i = 0; i < skinButtons.Length; i++)
            {
                skinButtons[i].interactable = false;
                skinLabels[i].text = "TSILA ORIGINAL  /  EQUIPPED";
            }
        }
    }
}
