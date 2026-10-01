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
            EnsureSkinButtons();
            ApplyModernShopStyle();
        }

        void OnEnable()
        {
            if (openFromStart != null) openFromStart.onClick.AddListener(game.OpenShop);
            if (openFromResults != null) openFromResults.onClick.AddListener(game.OpenShop);
            if (close != null) close.onClick.AddListener(game.CloseShop);
            if (game != null) game.StateChanged += Refresh;
        }

        void Start() { Refresh(); }

        void ApplyModernShopStyle()
        {
            if (panel != null)
            {
                var bg = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
                bg.color = new Color32(10, 15, 25, 235);
                bg.type = Image.Type.Sliced;
                if (panel.GetComponent<Outline>() == null)
                {
                    var outline = panel.AddComponent<Outline>();
                    outline.effectColor = new Color32(135, 170, 255, 75);
                    outline.effectDistance = new Vector2(0f, 2f);
                }
                if (panel.GetComponent<Shadow>() == null)
                {
                    var shadow = panel.AddComponent<Shadow>();
                    shadow.effectColor = new Color32(4, 8, 18, 160);
                    shadow.effectDistance = new Vector2(0f, -12f);
                }
            }

            if (close != null)
            {
                var img = close.GetComponent<Image>() ?? close.gameObject.AddComponent<Image>();
                img.color = new Color32(20, 96, 255, 255);
                close.targetGraphic = img;
                close.transition = Selectable.Transition.ColorTint;
                if (close.GetComponent<Outline>() == null)
                {
                    var outline = close.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color32(7, 12, 20, 175);
                    outline.effectDistance = new Vector2(0f, 5f);
                }
            }

            if (wallet != null)
            {
                wallet.color = new Color32(255, 206, 82, 255);
                wallet.fontSize = 24;
                wallet.fontStyle = FontStyle.Bold;
                wallet.alignment = TextAnchor.MiddleRight;
                wallet.resizeTextForBestFit = true;
                wallet.resizeTextMinSize = 14;
                wallet.resizeTextMaxSize = 24;
                wallet.horizontalOverflow = HorizontalWrapMode.Wrap;
                wallet.verticalOverflow = VerticalWrapMode.Truncate;
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
                    outline.effectDistance = new Vector2(0f, 5f);
                    outline.effectColor = new Color32(3, 8, 15, 170);
                }
                if (skinButtons[i].gameObject.GetComponent<UiButtonMotion>() == null)
                    skinButtons[i].gameObject.AddComponent<UiButtonMotion>();
                if (skinButtons[i].gameObject.GetComponent<Shadow>() == null)
                {
                    var shadow = skinButtons[i].gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color32(6, 12, 22, 150);
                    shadow.effectDistance = new Vector2(0f, -8f);
                }
            }

            for (int i = 0; i < skinLabels.Length; i++)
            {
                if (skinLabels[i] == null) continue;
                skinLabels[i].color = new Color32(245, 249, 255, 255);
                skinLabels[i].fontStyle = FontStyle.Bold;
                skinLabels[i].alignment = TextAnchor.MiddleCenter;
                skinLabels[i].resizeTextForBestFit = true;
                skinLabels[i].resizeTextMinSize = 13;
                skinLabels[i].resizeTextMaxSize = 22;
                skinLabels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                skinLabels[i].verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        void EnsureSkinButtons()
        {
            if (panel == null) return;
            var buttons = panel.GetComponentsInChildren<Button>(true);
            if (buttons != null && buttons.Length > 0)
            {
                skinButtons = buttons;
                skinLabels = new Text[buttons.Length];
                for (int i = 0; i < buttons.Length; i++)
                {
                    skinLabels[i] = buttons[i].GetComponentInChildren<Text>();
                    int index = i;
                    buttons[i].onClick.RemoveAllListeners();
                    buttons[i].onClick.AddListener(() => SelectSkin(index));
                }
            }
            else if (skinButtons == null || skinButtons.Length == 0)
            {
                skinButtons = new Button[RunnerProgress.SkinNames.Length];
                skinLabels = new Text[RunnerProgress.SkinNames.Length];
            }
        }

        void OnDisable()
        {
            if (game != null) game.StateChanged -= Refresh;
            if (openFromStart != null) openFromStart.onClick.RemoveListener(game.OpenShop);
            if (openFromResults != null) openFromResults.onClick.RemoveListener(game.OpenShop);
            if (close != null) close.onClick.RemoveListener(game.CloseShop);
        }

        void SelectSkin(int index)
        {
            if (game == null || game.Progress == null) return;
            if (!game.Progress.BuyOrEquip(index)) return;
            game.ApplySkin();
            Refresh();
        }

        void Refresh()
        {
            if (panel != null) panel.SetActive(game != null && game.State == RunnerGame.RunState.Shop);
            if (game == null || game.Progress == null || panel == null || !panel.activeSelf) return;
            if (wallet != null) wallet.text = "COINS  " + game.Progress.Wallet;
            for (int i = 0; i < skinButtons.Length; i++)
            {
                if (skinButtons[i] == null) continue;
                bool owned = game.Progress.Owns(i);
                bool selected = game.Progress.Selected == i;
                int price = RunnerProgress.Prices[i];
                string label = selected
                    ? RunnerProgress.SkinNames[i] + "  /  EQUIPPED"
                    : owned
                        ? RunnerProgress.SkinNames[i] + "  /  EQUIP"
                        : RunnerProgress.SkinNames[i] + "  /  " + price + " COINS";
                if (skinLabels[i] != null) skinLabels[i].text = label;
                var img = skinButtons[i].GetComponent<Image>();
                if (img != null) img.color = selected ? new Color32(20, 96, 255, 255) : owned ? new Color32(15, 140, 120, 255) : new Color32(16, 50, 82, 255);
                skinButtons[i].interactable = !selected;
                var colors = skinButtons[i].colors;
                colors.normalColor = img != null ? img.color : colors.normalColor;
                colors.highlightedColor = new Color(img != null ? img.color.r * 1.08f : 1f, img != null ? img.color.g * 1.08f : 1f, img != null ? img.color.b * 1.08f : 1f, 1f);
                skinButtons[i].colors = colors;
            }
        }
    }
}
