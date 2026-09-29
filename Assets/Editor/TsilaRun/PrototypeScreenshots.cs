using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun.Editor
{
    // Optional batch review tool. Captures render textures; never changes the saved scene.
    public static class PrototypeScreenshots
    {
        public static void CaptureBatch()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var camera = Object.FindAnyObjectByType<Camera>();
            var hud = Object.FindAnyObjectByType<RunnerHud>();
            var shop = hud.GetComponent<RunnerShop>();
            var preview = Object.FindAnyObjectByType<EditorPreview>();
            if (preview != null) preview.gameObject.SetActive(false);
            hud.game.world.ResetWorld(42);
            // Bring a representative row into view while keeping the player stationary.
            var outsideRoad = new Bounds(new Vector3(100f, 0f, 0f), Vector3.one);
            hud.game.world.Simulate(30f, outsideRoad, outsideRoad);
            var canvas = hud.GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var safeArea = canvas.GetComponentInChildren<SafeAreaPanel>();
            safeArea.enabled = false;
            var safe = (RectTransform)safeArea.transform;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Directory.CreateDirectory("Screenshots");
            int[,] screens = { { 720, 1280, 0, 0 }, { 1170, 2532, 102, 141 }, { 1080, 2400, 72, 90 }, { 2160, 3840, 0, 0 } };
            for (int i = 0; i < screens.GetLength(0); i++)
            {
                int width = screens[i, 0], height = screens[i, 1];
                var target = new RenderTexture(width, height, 24);
                target.Create(); camera.targetTexture = target; camera.aspect = (float)width / height;
                // Match the runtime Expand scaler against this render texture's dimensions.
                scaler.enabled = false;
                canvas.scaleFactor = Mathf.Min(width / 720f, height / 1280f);
                var area = new Rect(0f, screens[i, 2], width, height - screens[i, 2] - screens[i, 3]);
                SafeAreaPanel.NormalizedAnchors(area, width, height, out Vector2 min, out Vector2 max);
                safe.anchorMin = min; safe.anchorMax = max;
                safe.offsetMin = safe.offsetMax = Vector2.zero;
                for (int state = 0; state < 8; state++)
                {
                    hud.startPanel.SetActive(state == 0);
                    hud.hud.SetActive(state != 0 && state != 5);
                    hud.pausePanel.SetActive(state == 2);
                    hud.gameOverPanel.SetActive(state == 3);
                    hud.introPanel.SetActive(state == 4);
                    shop.panel.SetActive(state == 5);
                    if (state == 5)
                    {
                        shop.wallet.text = "PI?CES  150";
                        for (int skin = 0; skin < shop.skinLabels.Length; skin++)
                            shop.skinLabels[skin].text = "TSILA ORIGINAL  ?  ?QUIP?";
                    }
                    hud.zoneText.gameObject.SetActive(state != 0 && state != 5);
                    hud.zoneText.text = RunnerRoadSection.ZoneNames[state == 6 ? 1 : state == 7 ? 2 : 0];
                    foreach (var section in hud.game.world.GetComponentsInChildren<RunnerRoadSection>())
                        section.SetLocation(state == 6 ? 300d : state == 7 ? 600d : 0d);
                    hud.game.chase.officer.gameObject.SetActive(state == 4);
                    if (state == 4) { hud.game.chase.ResetChase(); hud.game.chase.TickIntro(1.2f); }
                    camera.transform.position = state == 4 ? new Vector3(7f, 5f, -11f) : new Vector3(0f, 6.5f, -10f);
                    camera.transform.LookAt(state == 4 ? new Vector3(0f, 1f, -2.5f) : new Vector3(0f, 1f, 16f));
                    hud.pauseButton.gameObject.SetActive(state == 1 || state >= 4 && state != 5);
                    bool menu = state == 0 || state == 5;
                    hud.game.GetComponent<RunnerPresentation>().ShowMenu(menu);
                    if (menu)
                    {
                        camera.transform.position = new Vector3(0f, 1.45f, -4.3f);
                        camera.transform.LookAt(new Vector3(0f, 0.98f, 0f));
                    }
                    var avatar = hud.game.player.GetComponentInChildren<RunnerAvatar>();
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MethodVisualBuilder.Art + "Tsila_" + (menu ? "Idle" : "Run") + ".anim");
                    clip.SampleAnimation(avatar.gameObject, .25f);
                    if (state == 4)
                        AssetDatabase.LoadAssetAtPath<AnimationClip>(MethodVisualBuilder.Art + "Officer_Run.anim").SampleAnimation(hud.game.chase.officer.gameObject, .2f);

                    Canvas.ForceUpdateCanvases();
                    foreach (var text in canvas.GetComponentsInChildren<Text>())
                    {
                        if (text.preferredHeight > text.rectTransform.rect.height + 1f)
                            throw new System.InvalidOperationException("Text clips at " + width + "x" + height + ": " + text.name);
                    }
                    camera.Render();
                    RenderTexture.active = target;
                    var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0f, 0f, width, height), 0, 0); pixels.Apply();
                    File.WriteAllBytes("Screenshots/" + width + "x" + height + "-" + state + ".png", pixels.EncodeToPNG());
                    Object.DestroyImmediate(pixels);
                    Debug.Log("TSILA_UI_OK " + width + "x" + height + " state=" + state);
                }
                camera.targetTexture = null; RenderTexture.active = null;
                target.Release(); Object.DestroyImmediate(target);
            }
            Debug.Log("TSILA_SCREENSHOTS_OK");
        }
    }
}
