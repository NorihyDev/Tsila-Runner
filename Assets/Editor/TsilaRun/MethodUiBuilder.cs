using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TsilaRun.Editor
{
    public static class MethodUiBuilder
    {
        static Font font;
        static Sprite rounded;
        static readonly Color Muted = new Color32(153, 172, 194, 255);
        static readonly Color Neon = new Color32(105, 239, 195, 255);
        static readonly Color Dark = new Color32(13, 19, 31, 248);
        static readonly Color Pale = new Color32(242, 246, 255, 255);
        static readonly Color Violet = new Color32(39, 50, 70, 255);

        public static InputSystemUIInputModule Build(RunnerGame game, Font typeface, RunnerPresentation presentation)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/TsilaRun/Art/UI/Fonts/Rajdhani-Bold.ttf") ?? typeface;
            rounded = UiShapeBuilder.Rounded();
            var canvas = new GameObject("Method Mobile UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            var safe = Rect(canvas.transform, "Safe Area", 0, 0, 1, 1);
            safe.gameObject.AddComponent<SafeAreaPanel>();
            var view = canvas.AddComponent<RunnerHud>(); view.game = game;
            var shop = canvas.AddComponent<RunnerShop>(); shop.game = game;

            var start = Rect(safe, "Main Menu", 0, 0, 1, 1); view.startPanel = start.gameObject;
            var title = SportText(start, "TSILA RUN", "TSILA RUN", 142, .05f, .81f, .95f, .945f);
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(Color.white, Color.white, Neon, Neon);
            title.fontSharedMaterial = MenuPolishBuilder.TitleMaterial();
            title.fontSharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, .06f);
            title.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(5, 20, 27, 255));
            title.gameObject.AddComponent<UiEntrance>().offset = 45;
            presentation.title = title.rectTransform;
            var subtitle = SportText(start, "Tagline", "RUN THE ISLAND", 25, .08f, .765f, .92f, .805f);
            subtitle.characterSpacing = 7f; subtitle.color = Muted;
            view.playButton = Button(start, "Play", "PLAY", .15f, .19f, .85f, .275f, Neon);
            Object.DestroyImmediate(view.playButton.GetComponentInChildren<Text>().gameObject);
            SportText(view.playButton.transform, "Label", "PLAY", 54, .12f, .02f, .93f, .98f).color = Dark;
            var triangle = Rect(view.playButton.transform, "Play icon", .11f, .33f, .18f, .67f).gameObject.AddComponent<PlayIconGraphic>();
            triangle.color = Dark; triangle.raycastTarget = false;
            view.playButton.gameObject.AddComponent<UiGradient>().bottom = new Color32(41, 188, 112, 255);
            var playEntry = view.playButton.gameObject.AddComponent<UiEntrance>(); playEntry.delay = .16f; playEntry.offset = -25;
            shop.openFromStart = Button(start, "Shop", "SHOP", .15f, .09f, .85f, .162f, Dark);
            Object.DestroyImmediate(shop.openFromStart.GetComponentInChildren<Text>().gameObject);
            SportText(shop.openFromStart.transform, "Label", "SHOP", 40, .04f, .05f, .96f, .95f).color = Neon;
            var shopOutline = shop.openFromStart.gameObject.AddComponent<Outline>(); shopOutline.effectColor = Neon; shopOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var shopEntry = shop.openFromStart.gameObject.AddComponent<UiEntrance>(); shopEntry.delay = .28f; shopEntry.offset = -25;
            view.menuMissionText = Text(start, "Current Mission", "COLLECT COINS  0/30  +40", 18, .1f, .278f, .9f, .31f);
            view.menuMissionText.color = Muted;

            var hud = Panel(safe, "HUD", .025f, .83f, .975f, .99f); view.hud = hud.gameObject;
            view.distanceText = Text(hud, "Distance", "0 m", 43, .04f, .46f, .67f, .94f);
            view.distanceText.alignment = TextAnchor.MiddleLeft; view.distanceText.fontStyle = FontStyle.Bold;
            view.coinsText = Text(hud, "Coins", "COINS  0", 23, .04f, .06f, .4f, .44f);
            view.coinsText.alignment = TextAnchor.MiddleLeft;
            view.bestText = Text(hud, "Best", "BEST  0 m", 20, .04f, .36f, .67f, .57f);
            view.bestText.alignment = TextAnchor.MiddleLeft; view.bestText.color = Muted;
            view.pauseButton = Button(hud, "Pause", "II", .82f, .04f, .96f, .42f, Neon);
            view.menuButton = Button(hud, "Menu", "MENU", .59f, .04f, .79f, .42f, Violet, 23);
            view.missionText = Text(safe, "Mission Status", "MISSION  COLLECT COINS  0/30  +40", 17, .1f, .78f, .9f, .825f);
            view.powerUpText = Text(safe, "Power Up Status", "", 17, .1f, .73f, .9f, .775f);
            view.zoneText = Text(safe, "Zone", "METHOD ISLAND", 20, .1f, .045f, .9f, .08f);
            view.zoneText.gameObject.AddComponent<Outline>().effectColor = Color.black;

            var pause = Panel(safe, "Pause", .07f, .22f, .93f, .79f); view.pausePanel = pause.gameObject;
            Text(pause, "Title", "PAUSE", 64, .08f, .76f, .92f, .94f).color = Neon;
            view.pauseMissionText = Text(pause, "Current Mission", "COLLECT COINS  0/30\nREWARD  +40 COINS", 23, .08f, .60f, .92f, .76f);
            view.resumeButton = Button(pause, "Resume", "RESUME", .09f, .44f, .91f, .59f, Neon);
            view.pausedRestartButton = Button(pause, "Restart", "RESTART", .09f, .26f, .91f, .41f, Violet);
            view.pausedMenuButton = Button(pause, "Menu", "MAIN MENU", .09f, .08f, .91f, .23f, Violet, 29);

            var over = Panel(safe, "Results", .07f, .17f, .93f, .82f); view.gameOverPanel = over.gameObject;
            view.gameOverTitleText = Text(over, "Title", RunnerHud.GameOverTitle, 53, .05f, .80f, .95f, .96f);
            view.gameOverTitleText.color = Neon;
            view.resultText = Text(over, "Result", "0 METRES\n0 COINS\nBEST  0 m", 32, .05f, .49f, .95f, .79f);
            view.restartButton = Button(over, "Restart", "PLAY AGAIN", .09f, .34f, .91f, .47f, Neon);
            shop.openFromResults = Button(over, "Shop", "SHOP", .09f, .19f, .91f, .32f, Violet);
            view.resultsMenuButton = Button(over, "Menu", "MAIN MENU", .09f, .04f, .91f, .17f, Violet, 29);

            var intro = Panel(safe, "Chase Intro", .14f, .82f, .86f, .96f); view.introPanel = intro.gameObject;
            Text(intro, "Caption", "READY?", 22, .04f, .08f, .68f, .92f);
            view.skipIntroButton = Button(intro, "Skip", "SKIP", .72f, .12f, .96f, .88f, Neon, 18);

            var market = Rect(safe, "Shop", 0, 0, 1, 1); shop.panel = market.gameObject;
            Text(market, "Title", "SHOP", 52, .06f, .825f, .77f, .945f).color = Neon;
            shop.wallet = Text(market, "Wallet", "COINS  0", 25, .1f, .77f, .9f, .825f);
            Text(market, "Catalog Title", "CHARACTERS / OUTFITS", 24, .1f, .70f, .9f, .76f).color = Muted;
            var skinButtons = new Button[RunnerProgress.SkinNames.Length];
            var skinLabels = new Text[RunnerProgress.SkinNames.Length];
            for (int i = 0; i < RunnerProgress.SkinNames.Length; i++)
            {
                string label = i == 0 ? RunnerProgress.SkinNames[i].ToUpper() + "  /  FREE" : RunnerProgress.SkinNames[i].ToUpper() + "  /  " + RunnerProgress.Prices[i] + " COINS";
                var button = Button(market, "Skin " + i, label, .1f, .62f - i * 0.085f, .9f, .69f - i * 0.085f, i == 0 ? Neon : Violet, 22);
                skinButtons[i] = button;
                skinLabels[i] = button.GetComponentInChildren<Text>();
            }
            shop.skinButtons = skinButtons;
            shop.skinLabels = skinLabels;
            shop.close = Button(market, "Back", "BACK", .1f, .065f, .9f, .145f, Violet);

            Text(start, "Copyright", "METHOD", 12, .08f, .009f, .92f, .039f).color = Muted;
            var tutorial = canvas.AddComponent<RunnerTutorial>(); tutorial.game = game;
            var tutorialCover = Panel(safe, "First Run Tutorial", 0, 0, 1, 1);
            tutorialCover.GetComponent<Image>().color = new Color32(5, 10, 23, 248);
            tutorialCover.GetComponent<Image>().raycastTarget = true;
            SportText(tutorialCover, "Title", "READY TO RUN?", 65, .06f, .72f, .94f, .85f).color = Neon;
            Text(tutorialCover, "Gestures", "SWIPE LEFT / RIGHT\nChange lanes\n\nSWIPE UP\nJump\n\nSWIPE DOWN\nRoll under obstacles", 32, .08f, .28f, .92f, .70f);
            Text(tutorialCover, "Keyboard", "KEYBOARD: ARROW KEYS / WASD", 18, .08f, .20f, .92f, .25f).color = Muted;
            tutorial.continueButton = Button(tutorialCover, "Continue", "LET'S GO", .15f, .09f, .85f, .17f, Neon);
            tutorial.panel = tutorialCover.gameObject; tutorial.panel.SetActive(false);
            foreach (var panel in new[] { start, pause, over, intro, market }) panel.gameObject.AddComponent<UiPanelMotion>();
            pause.gameObject.SetActive(false); over.gameObject.SetActive(false); intro.gameObject.SetActive(false);
            market.gameObject.SetActive(false); hud.gameObject.SetActive(false);
            view.zoneText.gameObject.SetActive(false);
            view.ApplyMobileLayout(); view.ApplyModernMobileStyle();
            return new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>();
        }
        static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        static RectTransform Panel(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rect = Rect(parent, name, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Dark; image.raycastTarget = false; image.sprite = rounded; image.type = Image.Type.Sliced;
            return rect;
        }
        static Text Text(Transform parent, string name, string value, int size, float x0, float y0, float x1, float y1)
        {
            var text = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = Mathf.RoundToInt(size * 1.5f); text.color = Pale;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = Mathf.RoundToInt(size * 1.5f);
            return text;
        }
        static TextMeshProUGUI SportText(Transform parent, string name, string value, int size, float x0, float y0, float x1, float y1)
        {
            var text = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = MenuPolishBuilder.FontAsset(); text.text = value; text.fontSize = size; text.color = Pale;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.enableAutoSizing = true; text.fontSizeMin = size * .65f; text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
        static Button Button(Transform parent, string name, string value, float x0, float y0, float x1, float y1, Color color, int size = 32)
        {
            var rect = Rect(parent, name, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.sprite = rounded; image.type = Image.Type.Sliced;
            var shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, .2f); shadow.effectDistance = new Vector2(0f, -3f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.pressedColor = new Color(.65f, .85f, .75f); colors.fadeDuration = .12f;
            colors.disabledColor = Color.white; button.colors = colors;
            Text(rect, "Label", value, size, .04f, .05f, .96f, .95f).color = color == Neon ? Dark : Pale;
            rect.gameObject.AddComponent<UiButtonMotion>();
            return button;
        }
    }
}
