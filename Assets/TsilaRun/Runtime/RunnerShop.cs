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

        void Awake()
        {
            ApplyModernShopStyle();
        }

        void OnEnable()
        {
            openFromStart.onClick.AddListener(game.OpenShop);
            openFromResults.onClick.AddListener(game.OpenShop);
            close.onClick.AddListener(game.CloseShop);
            game.StateChanged += Refresh;
        }

        void Start() { Refresh(); }

        void ApplyModernShopStyle()
        {
            if (panel != null)
            {
                var bg = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
                bg.color = new Color32(10, 15, 25, 235);
                bg.type = Image.Type.Sliced;
            }

            if (close != null)
            {
                var img = close.GetComponent<Image>() ?? close.gameObject.AddComponent<Image>();
                img.color = new Color32(20, 96, 255, 255);
                close.targetGraphic = img;
                close.transition = Selectable.Transition.ColorTint;
            }

            if (wallet != null)
            {
                wallet.color = new Color32(255, 206, 82, 255);
                wallet.fontSize = 24;
                wallet.fontStyle = FontStyle.Bold;
                wallet.alignment = TextAnchor.MiddleRight;
            }

            for (int i = 0; i < skinButtons.Length; i++)
            {
                if (skinButtons[i] == null) continue;
                var img = skinButtons[i].GetComponent<Image>() ?? skinButtons[i].gameObject.AddComponent<Image>();
                img.color = new Color32(16, 50, 82, 255);
                img.type = Image.Type.Sliced;
                skinButtons[i].targetGraphic = img;
                skinButtons[i].transition = Selectable.Transition.ColorTint;
                if (skinButtons[i].gameObject.GetComponent<Outline>() == null)
                {
                    var outline = skinButtons[i].gameObject.AddComponent<Outline>();
                    outline.effectDistance = new Vector2(0f, 3f);
                    outline.effectColor = new Color32(3, 8, 15, 170);
                }
                if (skinButtons[i].gameObject.GetComponent<UiButtonMotion>() == null)
                    skinButtons[i].gameObject.AddComponent<UiButtonMotion>();
            }

            for (int i = 0; i < skinLabels.Length; i++)
            {
                if (skinLabels[i] == null) continue;
                skinLabels[i].color = new Color32(245, 249, 255, 255);
                skinLabels[i].fontStyle = FontStyle.Bold;
            }
        }

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
