using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using FigmaImporter.V2;

namespace FigmaImporter.V2.Core
{
    public static class FigmaAssetDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Downloads image data asynchronously.
        /// </summary>
        public static async Task<byte[]> DownloadImageDataAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            return await _httpClient.GetByteArrayAsync(url);
        }

        /// <summary>
        /// Imports data as a sprite into the project. MUST be called on the main thread.
        /// </summary>
        public static Sprite ImportDataAsSprite(byte[] data, string targetPath)
        {
            string fullPath = Path.Combine(Application.dataPath, targetPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Save to disk first
            File.WriteAllBytes(fullPath, data);

            string assetPath = "Assets/" + targetPath;
            
            // Step 1: Initial file import to allow Unity to create .meta and basic TextureImporter
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            if (assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool needsReimport = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        needsReimport = true;
                    }

                    if (needsReimport)
                    {
                        // Save settings directly to the .meta file
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport(); // Triggers reimport
                        
                        // Step 2: HARD synchronous reimport, forcing Unity to wait and process .meta
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    }
                }
                else
                {
                    FigmaLog.Error($"{FigmaLog.VersionPrefix}TextureImporter is NULL for path {assetPath}");
                }
            }

            // Try to extract Sprite directly
            Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (loadedSprite != null) return loadedSprite;

            // Fallback search
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in allAssets)
            {
                if (obj is Sprite s) return s;
            }

            return null;
        }
    }
}
