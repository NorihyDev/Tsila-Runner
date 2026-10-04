using UnityEditor;
using UnityEngine;

namespace TsilaRun.Editor
{
    public static class AndroidIconBuilder
    {
        private const string IconPath = "Assets/TsilaRun/Art/Icon/logo.png";

        [MenuItem("Tools/Tsila Run/Apply Android App Icon")]
        public static void Apply()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null)
            {
                throw new System.IO.FileNotFoundException("Android app icon is missing.", IconPath);
            }

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                throw new System.InvalidOperationException("Unity could not import the Android app icon.");
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });

            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_ANDROID_ICON_OK " + IconPath);
        }
    }
}
