using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TsilaRun.Editor
{
    public static class MobilePrototypeBuilder
    {
        public const string Root = "Assets/TsilaRun/Generated";
        public const string ScenePath = Root + "/Scenes/TsilaRun.unity";
        static Material road, sand, teal, coral, gold, ink, white, green;
        static Font font;

        [MenuItem("Tools/Tsila Run/Create or Update Mobile Prototype")]
        public static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Tsila Run: leave Play mode before generating the scene.");
                return;
            }
            // This is the standard Save / Don't Save / Cancel prompt before replacing open scenes.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Tsila Run is ready. Press Play in the Editor, then tap PLAY in the game.");
        }

        // For an isolated validation checkout. Never discard dirty scenes silently in batch mode.
        public static void GenerateBatch()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save dirty scenes before batch generation.");
            Generate();
            Debug.Log("TSILA_GENERATION_OK");
        }

        static void Generate()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Input");
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) throw new InvalidOperationException("Unity's built-in LegacyRuntime.ttf font is unavailable.");
            road = Material("Road", "25465B"); sand = Material("Sand", "EACB91");
            teal = Material("Teal", "23CABD"); coral = Material("Coral", "FF6B61");
            gold = Material("Gold", "FFD44A"); ink = Material("Ink", "142B47");
            white = Material("Cream", "FFF5D9"); green = Material("Green", "70B879");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.8f, 0.85f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = ColorHex("A9DEEA");
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 175f;
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject roadPrefab = RoadPrefab();
            RunnerItem[] itemPrefabs = new RunnerItem[4];
            for (int i = 0; i < 4; i++) itemPrefabs[i] = ItemPrefab((RunnerItemKind)i);
            RunnerPlayer playerPrefab = PlayerPrefab();

            var game = new GameObject("Tsila Run").AddComponent<RunnerGame>();
            var world = new GameObject("Recycled World").AddComponent<RunnerWorld>();
            var player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject)).GetComponent<RunnerPlayer>();
            player.name = "Tsila";
            game.player = player; game.world = world;
            world.player = player; world.game = game; world.roadPrefab = roadPrefab; world.itemPrefabs = itemPrefabs;
            player.world = world;
            var input = game.gameObject.AddComponent<RunnerInput>();
            input.game = game; input.player = player;

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 185f;
            camera.allowHDR = false;
            camera.gameObject.AddComponent<AudioListener>();
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.antialiasing = AntialiasingMode.None;
            }
            var follow = camera.gameObject.AddComponent<RunnerCamera>();
            follow.player = player; follow.game = game;
            camera.transform.position = follow.offset;
            camera.transform.LookAt(new Vector3(0f, 1f, 16f));
            CreateUI(game);

            // Show the opening road in edit mode too. Runtime replaces this preview with its pool.
            var preview = new GameObject("Editor Road Preview");
            preview.tag = "EditorOnly";
            for (int i = 0; i < RunnerRules.RoadCount; i++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab, preview.transform);
                tile.transform.position = new Vector3(0f, 0f, (i - 1) * RunnerRules.RoadLength);
            }
            preview.AddComponent<EditorPreview>();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddBuildScene();
            AssetDatabase.SaveAssets();
        }

        static void AddBuildScene()
        {
            // Put this entry first, retaining all unrelated scenes in their original order.
            var profile = BuildProfile.GetActiveBuildProfile();
            EditorBuildSettingsScene[] existing = profile != null && profile.overrideGlobalScenes
                ? profile.scenes : EditorBuildSettings.globalScenes;
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var entry in existing) if (entry.path != ScenePath) scenes.Add(entry);
            if (profile != null && profile.overrideGlobalScenes)
            {
                profile.scenes = scenes.ToArray();
                EditorUtility.SetDirty(profile);
            }
            else EditorBuildSettings.globalScenes = scenes.ToArray();
        }

        static Material Material(string name, string hex)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Shader shader = Shader.Find(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset
                ? "Universal Render Pipeline/Simple Lit" : "Standard");
            if (shader == null) throw new InvalidOperationException("Compatible prototype shader not found.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.color = ColorHex(hex);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", Color.black);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        static GameObject SavePrefab(GameObject root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject RoadPrefab()
        {
            var root = new GameObject("Road Section");
            Shape(root.transform, "Road", PrimitiveType.Cube, new Vector3(0f, -0.18f, 0f), new Vector3(8f, 0.36f, 24f), road);
            Shape(root.transform, "Island", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(30f, 0.5f, 24f), sand);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape(root.transform, "Curb", PrimitiveType.Cube, new Vector3(side * 4.1f, 0.03f, 0f), new Vector3(0.2f, 0.16f, 24f), teal);
                Shape(root.transform, "Lane Stripe", PrimitiveType.Cube, new Vector3(side * 1.2f, 0.008f, 0f), new Vector3(0.055f, 0.012f, 24f), white);
                Shape(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(side * 7f, 0.8f, 4f), new Vector3(0.45f, 0.8f, 0.45f), ink);
                Shape(root.transform, "Tree", PrimitiveType.Sphere, new Vector3(side * 7f, 2.4f, 4f), new Vector3(2.7f, 3.1f, 2.7f), green);
                Shape(root.transform, "Block House", PrimitiveType.Cube, new Vector3(side * 11f, 2.1f, -5f), new Vector3(3.5f, 4.2f, 5f), side < 0 ? teal : coral);
                Shape(root.transform, "Roof", PrimitiveType.Cube, new Vector3(side * 11f, 4.3f, -5f), new Vector3(3.9f, 0.25f, 5.4f), white);
            }
            return SavePrefab(root, "RoadSection");
        }

        static RunnerPlayer PlayerPrefab()
        {
            var root = new GameObject("Tsila");
            var player = root.AddComponent<RunnerPlayer>();
            player.body = root.AddComponent<CapsuleCollider>();
            player.body.height = RunnerRules.StandingHeight;
            player.body.radius = RunnerRules.PlayerRadius;
            player.body.center = Vector3.up * 0.9f;
            player.body.isTrigger = true;
            player.visual = new GameObject("Visual").transform;
            player.visual.SetParent(root.transform, false);
            Shape(player.visual, "Suit", PrimitiveType.Capsule, Vector3.up * 0.9f, new Vector3(0.68f, 0.9f, 0.68f), teal);
            Shape(player.visual, "Helmet", PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.76f, white);
            Shape(player.visual, "Visor", PrimitiveType.Cube, new Vector3(0f, 1.52f, 0.34f), new Vector3(0.52f, 0.22f, 0.12f), ink);
            Shape(player.visual, "Backpack", PrimitiveType.Cube, new Vector3(0f, 1f, -0.36f), new Vector3(0.45f, 0.55f, 0.22f), coral);
            return SavePrefab(root, "Tsila").GetComponent<RunnerPlayer>();
        }

        static RunnerItem ItemPrefab(RunnerItemKind kind)
        {
            var root = new GameObject(kind.ToString());
            var item = root.AddComponent<RunnerItem>();
            item.kind = kind;
            item.hitbox = root.AddComponent<BoxCollider>();
            item.hitbox.isTrigger = true;
            Vector3 center, size;
            switch (kind)
            {
                case RunnerItemKind.Barrier:
                    center = new Vector3(0f, 0.425f, 0f); size = new Vector3(1.7f, 0.85f, 0.9f);
                    Shape(root.transform, "Jump Block", PrimitiveType.Cube, center, size, coral);
                    Shape(root.transform, "Top Stripe", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), new Vector3(1.75f, 0.035f, 0.95f), white);
                    break;
                case RunnerItemKind.Overhead:
                    center = new Vector3(0f, 1.9f, 0f); size = new Vector3(1.9f, 1.8f, 0.9f);
                    Shape(root.transform, "Slide Beam", PrimitiveType.Cube, center, size, gold);
                    // Posts sit outside the runner's legal centre line. Beam has 1m clearance.
                    for (int side = -1; side <= 1; side += 2)
                        Shape(root.transform, "Post", PrimitiveType.Cube, new Vector3(side * 1.05f, 1.4f, 0f), new Vector3(0.14f, 2.8f, 0.7f), ink);
                    break;
                case RunnerItemKind.Tower:
                    center = new Vector3(0f, 1.8f, 0f); size = new Vector3(1.8f, 3.6f, 1.1f);
                    Shape(root.transform, "Dodge Tower", PrimitiveType.Cube, center, size, ink);
                    Shape(root.transform, "Warning", PrimitiveType.Cube, new Vector3(0f, 1.7f, -0.56f), new Vector3(1.5f, 0.3f, 0.03f), coral);
                    break;
                default:
                    center = new Vector3(0f, 0.9f, 0f); size = new Vector3(0.65f, 0.65f, 0.3f);
                    var coin = Shape(root.transform, "Coin", PrimitiveType.Cylinder, center, new Vector3(0.6f, 0.1f, 0.6f), gold);
                    coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;
            }
            item.hitbox.center = center; item.hitbox.size = size;
            return SavePrefab(root, kind.ToString()).GetComponent<RunnerItem>();
        }

        static void CreateUI(RunnerGame game)
        {
            var canvas = new GameObject("Mobile UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            // Expand guarantees at least 720x1280 logical units, including short phones/tablets.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform safe = Rect("Safe Area", canvas.transform, Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<SafeAreaPanel>();
            var view = canvas.AddComponent<RunnerHud>();
            view.game = game;
            RectTransform hud = Rect("HUD", safe, new Vector2(0f, 1f), Vector2.one);
            hud.pivot = new Vector2(0.5f, 1f); hud.sizeDelta = new Vector2(0f, 180f);
            var hudBackground = hud.gameObject.AddComponent<Image>();
            hudBackground.color = new Color(0.055f, 0.12f, 0.2f, 0.94f);
            hudBackground.raycastTarget = false;
            view.hud = hud.gameObject;
            view.distanceText = Label(hud, "Distance", "0 m", 46, new Vector2(0.04f, 0.45f), new Vector2(0.64f, 0.94f), TextAnchor.MiddleLeft);
            view.coinsText = Label(hud, "Coins", "COINS  0", 26, new Vector2(0.04f, 0f), new Vector2(0.48f, 0.45f), TextAnchor.MiddleLeft);
            view.bestText = Label(hud, "Best", "BEST  0 m", 23, new Vector2(0.48f, 0f), new Vector2(0.96f, 0.45f), TextAnchor.MiddleRight);
            view.pauseButton = Button(hud, "Pause", "II", new Vector2(0.82f, 0.42f), new Vector2(0.96f, 0.94f), teal);

            RectTransform start = Card(safe, "Start"); view.startPanel = start.gameObject;
            Label(start, "Eyebrow", "THE ISLAND IS YOUR RUNWAY", 22, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.91f));
            Label(start, "Title", "TSILA\nRUN", 94, new Vector2(0.08f, 0.53f), new Vector2(0.92f, 0.84f));
            Label(start, "Guide", "SWIPE LEFT / RIGHT  -  CHANGE LANE\nSWIPE UP  -  JUMP\nSWIPE DOWN  -  SLIDE", 27, new Vector2(0.06f, 0.29f), new Vector2(0.94f, 0.51f));
            view.playButton = Button(start, "Play", "PLAY", new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.255f), teal);
            Label(start, "Hint", "Find the coin trail. Keep running.", 23, new Vector2(0.06f, 0.025f), new Vector2(0.94f, 0.105f));

            RectTransform pause = Card(safe, "Paused"); view.pausePanel = pause.gameObject;
            Label(pause, "Title", "TAKE A\nBREATHER", 65, new Vector2(0.08f, 0.57f), new Vector2(0.92f, 0.85f));
            Label(pause, "Hint", "Your run is paused.\nResume when you're ready.", 28, new Vector2(0.08f, 0.41f), new Vector2(0.92f, 0.57f));
            view.resumeButton = Button(pause, "Resume", "RESUME", new Vector2(0.12f, 0.245f), new Vector2(0.88f, 0.375f), teal);
            view.pausedRestartButton = Button(pause, "Restart", "RESTART", new Vector2(0.12f, 0.085f), new Vector2(0.88f, 0.215f), coral);

            RectTransform over = Card(safe, "Game Over"); view.gameOverPanel = over.gameObject;
            Label(over, "Title", "NICE RUN!", 68, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.86f));
            view.resultText = Label(over, "Result", "0 METRES\n0 COINS\nBEST  0 m", 38, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.64f));
            view.restartButton = Button(over, "Restart", "RUN AGAIN", new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.255f), teal);
            pause.gameObject.SetActive(false); over.gameObject.SetActive(false); hud.gameObject.SetActive(false);

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            ConfigureUIInput(events.GetComponent<InputSystemUIInputModule>());
        }

        static void ConfigureUIInput(InputSystemUIInputModule module)
        {
            // Persist both the asset and its imported action-reference subassets. Temporary
            // InputActionReference.Create objects are not durable scene wiring after reload.
            string path = Root + "/Input/TsilaUI.inputactions";
            var defaults = new DefaultInputActions();
            try { File.WriteAllText(path, defaults.asset.ToJson()); }
            finally { UnityEngine.Object.DestroyImmediate(defaults.asset); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            module.point = UIAction(path, "Point");
            module.leftClick = UIAction(path, "Click");
            module.move = UIAction(path, "Navigate");
            module.submit = UIAction(path, "Submit");
            module.cancel = UIAction(path, "Cancel");
            module.scrollWheel = UIAction(path, "ScrollWheel");
        }

        static InputActionReference UIAction(string path, string actionName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is InputActionReference reference && reference.action != null &&
                    reference.action.actionMap.name == "UI" && reference.action.name == actionName) return reference;
            throw new InvalidOperationException("Missing UI input action: " + actionName);
        }

        static RectTransform Card(Transform parent, string name)
        {
            RectTransform card = Rect(name, parent, new Vector2(0.045f, 0.16f), new Vector2(0.955f, 0.8f));
            var image = card.gameObject.AddComponent<Image>();
            image.color = new Color(0.055f, 0.12f, 0.2f, 0.96f);
            image.raycastTarget = false;
            return card;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text Label(Transform parent, string name, string value, int size, Vector2 min, Vector2 max, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.alignment = alignment;
            text.color = ColorHex("FFF5D9"); text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static Button Button(Transform parent, string name, string value, Vector2 min, Vector2 max, Material color)
        {
            RectTransform rect = Rect(name, parent, min, max);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color.color;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(rect, "Label", value, 36, Vector2.zero, Vector2.one).color = ColorHex("142B47");
            return button;
        }

        static Color ColorHex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }
    }
}
