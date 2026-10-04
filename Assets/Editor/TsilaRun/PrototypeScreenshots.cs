using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TsilaRun.Editor
{
    // Optional batch review tool. Captures render textures; never changes the saved scene.
    public static class PrototypeScreenshots
    {
        public static void CaptureBatch()
        { Capture(false); }

        public static void CaptureQuick()
        { Capture(true); }

        public static void RefreshLayoutAndCapture()
        { MeshyPackIntegration.RefreshLayoutBatch(); CaptureBatch(); }

        static void Capture(bool quick)
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var camera = Object.FindAnyObjectByType<Camera>();
            var hud = Object.FindAnyObjectByType<RunnerHud>();
            var shop = hud.GetComponent<RunnerShop>();
            shop.ApplyModernShopStyle();
            var preview = Object.FindAnyObjectByType<EditorPreview>();
            if (preview != null) preview.gameObject.SetActive(false);
            hud.game.world.ResetWorld(42);
            // Bring a representative row into view while keeping the player stationary.
            var outsideRoad = new Bounds(new Vector3(100f, 0f, 0f), Vector3.one);
            hud.game.world.Simulate(30f, outsideRoad, outsideRoad);
            // Manual Editor camera renders do not tick normal runtime LOD selection.
            foreach (var group in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include))
                group.ForceLOD(0);
            // Multiple snapshots render inside one Editor frame. Freeze each CPU-skinned pose
            // into a temporary mesh; Unity's GPU bone buffers only refresh once per frame.
            var baked = new Dictionary<SkinnedMeshRenderer, Mesh>();
            foreach (var group in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include))
                foreach (var skin in group.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>())
                {
                    var pose = new GameObject("Snapshot Pose"); pose.transform.SetParent(skin.transform, false);
                    var filter = pose.AddComponent<MeshFilter>(); filter.sharedMesh = new Mesh();
                    pose.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                    baked[skin] = filter.sharedMesh; skin.enabled = false;
                }
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
            if (quick) screens = new int[,] { { 720, 1280, 0, 0 } };
            for (int i = 0; i < screens.GetLength(0); i++)
            {
                int width = screens[i, 0], height = screens[i, 1];
                var target = new RenderTexture(width, height, 24);
                target.Create(); camera.targetTexture = target; camera.aspect = (float)width / height;
                // Match the runtime 0.5 width/height scaler against this render texture.
                scaler.enabled = false;
                canvas.scaleFactor = Mathf.Sqrt((width / 1080f) * (height / 1920f));
                var area = new Rect(0f, screens[i, 2], width, height - screens[i, 2] - screens[i, 3]);
                SafeAreaPanel.NormalizedAnchors(area, width, height, out Vector2 min, out Vector2 max);
                safe.anchorMin = min; safe.anchorMax = max;
                safe.offsetMin = safe.offsetMax = Vector2.zero;
                for (int state = 0; state < (quick ? 2 : i == 0 ? 13 : 8); state++)
                {
                    hud.startPanel.SetActive(state == 0);
                    hud.hud.SetActive(state != 0 && state != 5);
                    hud.pausePanel.SetActive(state == 2);
                    hud.gameOverPanel.SetActive(state == 3);
                    hud.introPanel.SetActive(state == 4);
                    shop.panel.SetActive(state == 5);
                    if (state == 5)
                    {
                        shop.wallet.text = "COINS  150";
                        for (int skin = 0; skin < shop.skinLabels.Length && skin < RunnerProgress.SkinNames.Length; skin++)
                            shop.skinLabels[skin].text = RunnerProgress.SkinNames[skin].ToUpperInvariant() +
                                (skin == 0 ? "  /  EQUIPPED" : "  /  " + RunnerProgress.Prices[skin] + " COINS");
                    }
                    if (hud.missionText != null) hud.missionText.gameObject.SetActive(false);
                    if (hud.powerUpText != null) hud.powerUpText.gameObject.SetActive(false);
                    hud.zoneText.gameObject.SetActive(state == 6 || state == 7);
                    hud.zoneText.text = RunnerRoadSection.ZoneNames[state == 6 ? 1 : state == 7 ? 2 : 0];
                    foreach (var section in hud.game.world.GetComponentsInChildren<RunnerRoadSection>(true))
                        section.SetLocation((state == 6 ? 288d : state == 7 ? 576d : 48d) + section.transform.position.z);
                    hud.game.chase.officer.gameObject.SetActive(state == 4);
                    if (state == 4) { hud.game.chase.ResetChase(); hud.game.chase.TickIntro(1.2f); }
                    camera.transform.position = state == 4 ? new Vector3(3.8f, 4.5f, -10.5f) : new Vector3(0f, 6.5f, -10f);
                    camera.transform.LookAt(state == 4 ? new Vector3(0f, 1f, -2.5f) : new Vector3(0f, 1f, 16f));
                    hud.pauseButton.gameObject.SetActive(state == 1 || state >= 4 && state != 5);
                    bool menu = state == 0 || state == 5;
                    hud.game.GetComponent<RunnerPresentation>().ShowMenu(menu);
                    if (menu)
                    {
                        camera.transform.position = new Vector3(0f, 1.55f, -4.6f);
                        camera.transform.LookAt(new Vector3(0f, .6f, 0f));
                    }
                    var avatar = hud.game.player.GetComponentInChildren<RunnerAvatar>();
                    avatar.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    hud.game.chase.officer.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var clip = avatar.animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("_" + (menu ? "Idle" : state >= 8 ? "Slide" : "Run")));
                    float poseTime = state >= 8 ? (state - 8) * .25f : .25f;
                    avatar.animator.Rebind();
                    avatar.animator.Play(menu ? "Idle" : state >= 8 ? "Slide" : "Run", 0, Mathf.Min(.999f, poseTime / clip.length));
                    avatar.animator.Update(0f);
                    if (state >= 8) { camera.transform.position = new Vector3(2f, 1.5f, -3f); camera.transform.LookAt(new Vector3(0, .4f, 0)); }
                    if (state == 4) { hud.game.chase.officer.animator.Rebind(); hud.game.chase.officer.animator.Play("Run", 0, .3f); hud.game.chase.officer.animator.Update(0f); }

                    foreach (var motion in canvas.GetComponentsInChildren<UiEntrance>()) motion.Finish();
                    foreach (var group in canvas.GetComponentsInChildren<CanvasGroup>()) group.alpha = 1f;
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in canvas.GetComponentsInChildren<Text>())
                    {
                        if (text.preferredHeight > text.rectTransform.rect.height + 1f)
                            throw new System.InvalidOperationException("Text clips at " + width + "x" + height + ": " + text.name);
                    }
                    foreach (var text in canvas.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        if (text.isTextOverflowing) throw new System.InvalidOperationException("TMP text clips at " + width + "x" + height + ": " + text.name);
                    }
                    foreach (var pose in baked) pose.Key.BakeMesh(pose.Value);
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
            foreach (var pose in baked) Object.DestroyImmediate(pose.Value);
            Debug.Log("TSILA_SCREENSHOTS_OK");
        }
    }
}
