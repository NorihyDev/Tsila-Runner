using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TsilaRun.Editor
{
    public static class ModernUiUpdater
    {
        [MenuItem("Tools/Tsila Run/Refresh Modern UI")]
        public static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RefreshBatch();
        }
        public static void RefreshBatch()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new System.InvalidOperationException("Save scene changes before refreshing UI.");
            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            var previous = Object.FindAnyObjectByType<RunnerHud>();
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var events = Object.FindAnyObjectByType<EventSystem>();
            if (events != null) Object.DestroyImmediate(events.gameObject);
            var module = MethodUiBuilder.Build(game, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), game.GetComponent<RunnerPresentation>());
            MobilePrototypeBuilder.ConfigureUIInput(module);
            Camera.main.backgroundColor = new Color32(10, 15, 25, 255);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_MODERN_UI_OK");
        }
        public static void RefreshAndPreviewBatch()
        {
            RefreshBatch();
            PrototypeScreenshots.CaptureQuick();
        }
    }
}
