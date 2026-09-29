using System.IO;
using UnityEditor;
using UnityEngine;

namespace TsilaRun.Editor
{
    public static class UiShapeBuilder
    {
        public static Sprite Rounded()
        {
            const string path = MobilePrototypeBuilder.Root + "/UI/Rounded.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    var q = new Vector2(Mathf.Max(Mathf.Abs(x - 31.5f) - 13.5f, 0f), Mathf.Max(Mathf.Abs(y - 31.5f) - 13.5f, 0f));
                    float alpha = Mathf.Clamp01(18f - q.magnitude);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(20, 20, 20, 20); importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
