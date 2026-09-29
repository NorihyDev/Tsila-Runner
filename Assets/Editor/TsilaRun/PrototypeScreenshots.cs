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
            int[,] screens = { { 720, 1280, 0, 0 }, { 1170, 2532, 102, 141 }, { 1080, 2400, 72, 90 } };
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
                for (int state = 0; state < 4; state++)
                {
                    hud.startPanel.SetActive(state == 0);
                    hud.hud.SetActive(state != 0);
                    hud.pausePanel.SetActive(state == 2);
                    hud.gameOverPanel.SetActive(state == 3);
                    hud.pauseButton.gameObject.SetActive(state == 1);
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
