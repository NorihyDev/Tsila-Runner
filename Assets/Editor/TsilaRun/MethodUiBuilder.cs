using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TsilaRun.Editor
{
    public static class MethodUiBuilder
    {
        static Font font;
        static readonly Color Neon = new Color32(0, 239, 136, 255);
        static readonly Color Dark = new Color32(5, 21, 18, 248);
        static readonly Color Pale = new Color32(218, 255, 238, 255);
        static readonly Color Violet = new Color32(71, 44, 115, 255);

        public static InputSystemUIInputModule Build(RunnerGame game, Font typeface, RunnerPresentation presentation)
        {
            font = typeface;
            var canvas = new GameObject("Method Mobile UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var safe = Rect(canvas.transform, "Safe Area", 0, 0, 1, 1);
            safe.gameObject.AddComponent<SafeAreaPanel>();
            var view = canvas.AddComponent<RunnerHud>(); view.game = game;
            var shop = canvas.AddComponent<RunnerShop>(); shop.game = game;

            var start = Rect(safe, "Main Menu", 0, 0, 1, 1); view.startPanel = start.gameObject;
            var title = Text(start, "TSILA RUN", "TSILA\nRUN", 76, .075f, .78f, .76f, .96f);
            title.fontStyle = FontStyle.Bold; title.alignment = TextAnchor.MiddleLeft; title.color = Neon;
            title.gameObject.AddComponent<Shadow>().effectColor = Violet;
            presentation.title = title.rectTransform;
            Text(start, "Tagline", "UNE ÎLE. TROIS VOIES. AUCUNE LIMITE.", 20, .08f, .735f, .92f, .78f);
            Text(start, "Outfit", "TSILA ORIGINAL  /  METHOD", 20, .1f, .30f, .9f, .34f).color = Neon;
            view.playButton = Button(start, "Play", "JOUER", .1f, .205f, .9f, .28f, Neon);
            shop.openFromStart = Button(start, "Shop", "MAGASIN", .1f, .12f, .9f, .187f, Violet);
            Text(start, "Controls", "GLISSE :  GAUCHE / DROITE = VOIE\nHAUT = SAUTER  ·  BAS = GLISSER", 20, .04f, .055f, .96f, .105f);

            var hud = Panel(safe, "HUD", 0, .83f, 1, 1); view.hud = hud.gameObject;
            view.distanceText = Text(hud, "Distance", "0 m", 43, .04f, .46f, .67f, .94f);
            view.distanceText.alignment = TextAnchor.MiddleLeft;
            view.coinsText = Text(hud, "Coins", "PIÈCES  0", 23, .04f, .06f, .4f, .44f);
            view.coinsText.alignment = TextAnchor.MiddleLeft;
            view.bestText = Text(hud, "Best", "RECORD  0 m", 20, .04f, .36f, .67f, .57f);
            view.bestText.alignment = TextAnchor.MiddleLeft;
            view.pauseButton = Button(hud, "Pause", "II", .82f, .04f, .96f, .42f, Neon);
            view.menuButton = Button(hud, "Menu", "MENU", .59f, .04f, .79f, .42f, Violet, 23);
            view.zoneText = Text(safe, "Zone", "ÎLE METHOD", 20, .1f, .045f, .9f, .08f);
            view.zoneText.gameObject.AddComponent<Outline>().effectColor = Color.black;

            var pause = Panel(safe, "Pause", .07f, .22f, .93f, .79f); view.pausePanel = pause.gameObject;
            Text(pause, "Title", "PAUSE", 64, .08f, .76f, .92f, .94f).color = Neon;
            Text(pause, "Message", "Reprends quand tu es prêt.", 26, .06f, .63f, .94f, .75f);
            view.resumeButton = Button(pause, "Resume", "REPRENDRE", .09f, .44f, .91f, .59f, Neon);
            view.pausedRestartButton = Button(pause, "Restart", "RECOMMENCER", .09f, .26f, .91f, .41f, Violet);
            view.pausedMenuButton = Button(pause, "Menu", "RETOUR AU MENU", .09f, .08f, .91f, .23f, Violet, 29);

            var over = Panel(safe, "Results", .07f, .17f, .93f, .82f); view.gameOverPanel = over.gameObject;
            Text(over, "Title", "BIEN JOUÉ !", 53, .05f, .80f, .95f, .96f).color = Neon;
            view.resultText = Text(over, "Result", "0 MÈTRES\n0 PIÈCES\nRECORD  0 m", 32, .05f, .49f, .95f, .79f);
            view.restartButton = Button(over, "Restart", "REJOUER", .09f, .34f, .91f, .47f, Neon);
            shop.openFromResults = Button(over, "Shop", "MAGASIN", .09f, .19f, .91f, .32f, Violet);
            view.resultsMenuButton = Button(over, "Menu", "RETOUR AU MENU", .09f, .04f, .91f, .17f, Violet, 29);

            var intro = Panel(safe, "Chase Intro", .06f, .64f, .94f, .81f); view.introPanel = intro.gameObject;
            Text(intro, "Caption", "COURS, TSILA !\nLa patrouille est juste derrière.", 27, .04f, .44f, .96f, .95f);
            view.skipIntroButton = Button(intro, "Skip", "C'EST PARTI", .12f, .04f, .88f, .41f, Neon, 29);

            var market = Rect(safe, "Shop", 0, 0, 1, 1); shop.panel = market.gameObject;
            Text(market, "Title", "MAGASIN", 52, .06f, .825f, .77f, .945f).color = Neon;
            shop.wallet = Text(market, "Wallet", "PIÈCES  0", 25, .1f, .77f, .9f, .825f);
            var baseSkin = Button(market, "Base Skin", "TSILA ORIGINAL  ·  ÉQUIPÉ", .1f, .265f, .9f, .33f, Neon, 25);
            baseSkin.interactable = false;
            shop.skinButtons = new[] { baseSkin }; shop.skinLabels = new[] { baseSkin.GetComponentInChildren<Text>() };
            Text(market, "Empty Catalog", "Le skin de base est équipé.\nAucun article disponible pour le moment.", 24, .06f, .16f, .94f, .25f);
            shop.close = Button(market, "Back", "RETOUR", .1f, .065f, .9f, .145f, Violet);

            // Branding stays above every panel and inside the device safe area.
            var brand = Rect(safe, "Method Logo Frame", .79f, .905f, .97f, .995f);
            var logoRect = Rect(brand, "Method Logo", 0, 0, 1, 1);
            var logo = logoRect.gameObject.AddComponent<RawImage>();
            logo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TsilaRunArt/Textures/MethodLogo.png");
            logo.raycastTarget = false;
            var fit = logoRect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)logo.texture.width / logo.texture.height;
            Text(safe, "Copyright", "© Copyright by Method", 17, .08f, .009f, .92f, .039f).color = Pale;
            pause.gameObject.SetActive(false); over.gameObject.SetActive(false); intro.gameObject.SetActive(false);
            market.gameObject.SetActive(false); hud.gameObject.SetActive(false);
            view.zoneText.gameObject.SetActive(false);
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
            var image = rect.gameObject.AddComponent<Image>(); image.color = Dark; image.raycastTarget = false;
            return rect;
        }
        static Text Text(Transform parent, string name, string value, int size, float x0, float y0, float x1, float y1)
        {
            var text = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Pale;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }
        static Button Button(Transform parent, string name, string value, float x0, float y0, float x1, float y1, Color color, int size = 32)
        {
            var rect = Rect(parent, name, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.pressedColor = new Color(.65f, .85f, .75f); colors.fadeDuration = .08f;
            colors.disabledColor = Color.white; button.colors = colors;
            Text(rect, "Label", value, size, .04f, .05f, .96f, .95f).color = color == Neon ? Dark : Pale;
            return button;
        }
    }
}
