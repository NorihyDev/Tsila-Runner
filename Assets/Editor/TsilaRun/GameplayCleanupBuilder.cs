using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TsilaRun.Editor
{
    // Applies targeted presentation fixes without rebuilding the integrated art or scene.
    public static class GameplayCleanupBuilder
    {
        [MenuItem("Tools/Tsila Run/Apply Gameplay Cleanup")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBatch();
        }

        public static void ApplyBatch()
        {
            MenuPolishBuilder.RepairStageMaterials();
            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var shop = Object.FindAnyObjectByType<RunnerShop>();
            if (shop != null) shop.ApplyModernShopStyle();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_GAMEPLAY_CLEANUP_OK");
        }

        public static void ApplyAndCapture()
        {
            ApplyBatch();
            PrototypeScreenshots.CaptureBatch();
        }

        public static void InspectRoadSurface()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/RoadSection.prefab");
            var root = Object.Instantiate(prefab);
            var report = new System.Text.StringBuilder();
            try
            {
                var road = root.transform.Find("RoadSection");
                foreach (var filter in road.GetComponentsInChildren<MeshFilter>(true))
                {
                    var vertices = filter.sharedMesh.vertices;
                    var triangles = filter.sharedMesh.triangles;
                    report.AppendLine(filter.name + " " + MeshyPackIntegration.PlacedBounds(filter.gameObject));
                    var heights = new System.Collections.Generic.SortedDictionary<float, int>();
                    float minZ = float.MaxValue, maxZ = float.MinValue;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var a = filter.transform.TransformPoint(vertices[triangles[i]]);
                        var b = filter.transform.TransformPoint(vertices[triangles[i + 1]]);
                        var c = filter.transform.TransformPoint(vertices[triangles[i + 2]]);
                        var normal = Vector3.Cross(b - a, c - a).normalized;
                        var centre = (a + b + c) / 3f;
                        if (Mathf.Abs(centre.x) > 3.5f || normal.y < .9f) continue;
                        float height = Mathf.Round(centre.y * 1000f) / 1000f;
                        heights.TryGetValue(height, out int count);
                        heights[height] = count + 1;
                        minZ = Mathf.Min(minZ, a.z, b.z, c.z);
                        maxZ = Mathf.Max(maxZ, a.z, b.z, c.z);
                    }
                    foreach (var entry in heights) report.AppendLine("surface y=" + entry.Key + " triangles=" + entry.Value);
                    report.AppendLine("surface z=" + minZ + " to " + maxZ);
                }
            }
            finally { Object.DestroyImmediate(root); }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/GameplayCleanup-Road.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
