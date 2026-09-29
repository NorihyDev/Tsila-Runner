using UnityEngine;
using UnityEngine.Events;
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
        UnityAction[] chooseSkin;

        void OnEnable()
        {
            openFromStart.onClick.AddListener(game.OpenShop);
            openFromResults.onClick.AddListener(game.OpenShop);
            close.onClick.AddListener(game.CloseShop);
            chooseSkin = new UnityAction[skinButtons.Length];
            for (int i = 0; i < skinButtons.Length; i++)
            {
                int index = i;
                chooseSkin[i] = () => Choose(index);
                skinButtons[i].onClick.AddListener(chooseSkin[i]);
            }
            game.StateChanged += Refresh;
        }

        void Start() { Refresh(); }

        void OnDisable()
        {
            game.StateChanged -= Refresh;
            openFromStart.onClick.RemoveListener(game.OpenShop);
            openFromResults.onClick.RemoveListener(game.OpenShop);
            close.onClick.RemoveListener(game.CloseShop);
            for (int i = 0; i < skinButtons.Length; i++) skinButtons[i].onClick.RemoveListener(chooseSkin[i]);
        }

        void Choose(int index)
        {
            if (game.State != RunnerGame.RunState.Shop) return;
            if (game.Progress.BuyOrEquip(index)) game.ApplySkin();
            Refresh();
        }

        void Refresh()
        {
            panel.SetActive(game.State == RunnerGame.RunState.Shop);
            if (!panel.activeSelf || game.Progress == null) return;
            wallet.text = "WALLET  " + game.Progress.Wallet + " COINS";
            for (int i = 0; i < skinButtons.Length; i++)
            {
                bool owned = game.Progress.Owns(i);
                bool selected = game.Progress.Selected == i;
                skinButtons[i].interactable = !selected && (owned || game.Progress.Wallet >= RunnerProgress.Prices[i]);
                skinLabels[i].text = RunnerProgress.SkinNames[i] + "   " +
                    (selected ? "EQUIPPED" : owned ? "EQUIP" : RunnerProgress.Prices[i] + " COINS");
            }
        }
    }
}
