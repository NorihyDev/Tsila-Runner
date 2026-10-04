using UnityEditor;
using UnityEngine;

namespace TsilaRun.Editor
{
    public static class BuildReadinessBuilder
    {
        [MenuItem("Tools/Tsila Run/Apply APK Build Readiness Fixes")]
        public static void Apply()
        {
            AndroidIconBuilder.Apply();
            CharacterMotionBuilder.Build();
            MobilePrototypeBuilder.GenerateBatch();
            // Generation creates the base road; restore the imported landscape and cast.
            MeshyPackIntegration.RefreshLayoutBatch();
            MethodMusicBuilder.ApplyBatch();
            GraphicsPolishBuilder.ApplyBatch();
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_APK_READY_FIXES_OK");
        }
    }
}
