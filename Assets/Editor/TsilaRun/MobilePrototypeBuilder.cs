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
            Method.TsilaRun.Editor.TsilaRunAssetImporter.ImportForPrototype();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) throw new InvalidOperationException("Unity's built-in LegacyRuntime.ttf font is unavailable.");
            road = Material("Road", "142A2B"); sand = Material("Sand", "315D4D");
            teal = Material("Teal", "00EF88"); coral = Material("Coral", "6946AC");
            gold = Material("Gold", "B9FF51"); ink = Material("Ink", "071A1C");
            white = Material("Cream", "DBFFEE"); green = Material("Green", "438C6E");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.8f, 0.85f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = ColorHex("173C36");
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 175f;
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject roadPrefab = RoadPrefab();
            RunnerItem[] itemPrefabs = new RunnerItem[RunnerRules.ItemKindCount];
            for (int i = 0; i < itemPrefabs.Length; i++) itemPrefabs[i] = ItemPrefab((RunnerItemKind)i);
            RunnerPlayer playerPrefab = PlayerPrefab();

            var game = new GameObject("Tsila Run").AddComponent<RunnerGame>();
            var world = new GameObject("Recycled World").AddComponent<RunnerWorld>();
            var player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject)).GetComponent<RunnerPlayer>();
            player.name = "Tsila";
            game.player = player; game.world = world;
            game.skinMaterials = new Material[0]; // Method atlas is the sole base outfit.
            world.player = player; world.game = game; world.roadPrefab = roadPrefab; world.itemPrefabs = itemPrefabs;
            player.world = world;
            var avatar = player.GetComponentInChildren<RunnerAvatar>();
            avatar.game = game; avatar.player = player;
            var officerRoot = new GameObject("Officer");
            var officerRig = MethodVisualBuilder.Character(officerRoot.transform, "Officer");
            officerRig.alwaysRun = true;
            GameObject officerPrefab = SavePrefab(officerRoot, "Officer");
            var officer = ((GameObject)PrefabUtility.InstantiatePrefab(officerPrefab)).GetComponentInChildren<RunnerAvatar>();
            var chase = game.gameObject.AddComponent<RunnerChase>();
            chase.game = game; chase.officer = officer; game.chase = chase;
            officer.gameObject.SetActive(false);
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
            var presentation = MethodVisualBuilder.Stage(game, ink, teal, coral);
            ConfigureUIInput(MethodUiBuilder.Build(game, font, presentation));
            camera.transform.position = new Vector3(0f, 1.45f, -4.3f);
            camera.transform.LookAt(new Vector3(0f, 0.98f, 0f));
            player.visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
            presentation.stage.SetActive(true);

            // Show the opening road in edit mode too. Runtime replaces this preview with its pool.
            var preview = new GameObject("Editor Road Preview");
            preview.tag = "EditorOnly";
            for (int i = 0; i < RunnerRules.RoadCount; i++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab, preview.transform);
                tile.transform.position = new Vector3(0f, 0f, (i - 1) * RunnerRules.RoadLength);
            }
            preview.AddComponent<EditorPreview>();
            preview.SetActive(false);
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

        internal static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
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
            var section = root.AddComponent<RunnerRoadSection>();
            var island = new GameObject("Island Scenery"); island.transform.SetParent(root.transform, false);
            var mountain = new GameObject("Mountain Scenery"); mountain.transform.SetParent(root.transform, false);
            var tunnel = new GameObject("Underground Scenery"); tunnel.transform.SetParent(root.transform, false);
            section.scenery = new[] { island, mountain, tunnel };
            MethodVisualBuilder.Model(root.transform, "RoadSection");
            Shape(island.transform, "Island", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(30f, 0.5f, 24f), sand);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape(island.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(side * 7f, 0.8f, 4f), new Vector3(0.45f, 0.8f, 0.45f), ink);
                Shape(island.transform, "Tree", PrimitiveType.Sphere, new Vector3(side * 7f, 2.4f, 4f), new Vector3(2.7f, 3.1f, 2.7f), green);
                Shape(island.transform, "Block House", PrimitiveType.Cube, new Vector3(side * 11f, 2.1f, -5f), new Vector3(3.5f, 4.2f, 5f), side < 0 ? teal : coral);
                Shape(island.transform, "Roof", PrimitiveType.Cube, new Vector3(side * 11f, 4.3f, -5f), new Vector3(3.9f, 0.25f, 5.4f), white);
                Shape(mountain.transform, "Cliff", PrimitiveType.Cube, new Vector3(side * 5.5f, -5f, 0f), new Vector3(3f, 10f, 24f), ink);
                Shape(mountain.transform, "Safety Rail", PrimitiveType.Cube, new Vector3(side * 4.25f, 0.65f, 0f), new Vector3(0.15f, 0.25f, 24f), gold);
                var peak = Shape(mountain.transform, "Angular Mountain", PrimitiveType.Cube, new Vector3(side * 14f, 2f, 0f), new Vector3(11f, 15f, 13f), green);
                peak.transform.localRotation = Quaternion.Euler(0f, 15f, side * 35f);
                var snow = Shape(mountain.transform, "Snow Cap", PrimitiveType.Cube, new Vector3(side * 14f, 8f, 0f), new Vector3(5f, 5f, 6f), white);
                snow.transform.localRotation = Quaternion.Euler(0f, 15f, side * 35f);
                Shape(tunnel.transform, "Tunnel Wall", PrimitiveType.Cube, new Vector3(side * 5f, 4.5f, 0f), new Vector3(1f, 9f, 24f), ink);
                Shape(tunnel.transform, "Light Strip", PrimitiveType.Cube, new Vector3(side * 4.45f, 3f, 0f), new Vector3(0.1f, 0.16f, 23f), gold);
                Shape(tunnel.transform, "Stone Rib", PrimitiveType.Cube, new Vector3(side * 4.5f, 4.5f, 0f), new Vector3(0.3f, 9f, 0.8f), road);
            }
            Shape(mountain.transform, "Bridge Deck", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(8.5f, 0.5f, 24f), ink);
            // Ceiling clears the follow camera and maximum jump; scenery has no hazard colliders.
            Shape(tunnel.transform, "Tunnel Ceiling", PrimitiveType.Cube, new Vector3(0f, 9f, 0f), new Vector3(11f, 0.6f, 24f), ink);
            Shape(tunnel.transform, "Ceiling Light", PrimitiveType.Cube, new Vector3(0f, 8.6f, 0f), new Vector3(1.5f, 0.08f, 5f), white);
            section.SetLocation(0d);
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
            MethodVisualBuilder.Character(player.visual, "Tsila").player = player;
            player.animatedSlide = true;
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
                case RunnerItemKind.RunningPerson:
                    center = new Vector3(0f, 0.94f, 0f); size = new Vector3(0.95f, 1.88f, 0.8f);
                    MethodVisualBuilder.Character(root.transform, "RunningPerson").alwaysRun = true;
                    break;
                case RunnerItemKind.Barrier:
                    center = new Vector3(0f, 0.425f, 0f); size = new Vector3(1.7f, 0.85f, 0.9f); break;
                case RunnerItemKind.Overhead:
                    center = new Vector3(0f, 1.9f, 0f); size = new Vector3(1.9f, 1.8f, 0.9f); break;
                case RunnerItemKind.Tower:
                    center = new Vector3(0f, 1.8f, 0f); size = new Vector3(1.8f, 3.6f, 1.1f); break;
                default:
                    center = new Vector3(0f, 0.9f, 0f); size = new Vector3(0.65f, 0.65f, 0.3f); break;
            }
            if (kind != RunnerItemKind.RunningPerson) MethodVisualBuilder.Model(root.transform, kind.ToString());
            item.hitbox.center = center; item.hitbox.size = size;
            return SavePrefab(root, kind.ToString()).GetComponent<RunnerItem>();
        }

        internal static void ConfigureUIInput(InputSystemUIInputModule module)
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

        static Color ColorHex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }
    }
}
