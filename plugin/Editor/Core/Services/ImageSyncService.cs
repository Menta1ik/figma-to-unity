using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Core.Handlers;

namespace FigmaImporter.V2.Core.Services
{
    public class ImageSyncService
    {
        private readonly string _accessToken;
        private readonly string _fileId;
        private readonly FigmaImporterSettings _settings;

        public ImageSyncService(string accessToken, string fileId, FigmaImporterSettings settings)
        {
            _accessToken = accessToken;
            _fileId = fileId;
            _settings = settings;
        }

        public async Task SyncImagesAsync(
            string prefix, 
            FigmaHandlerContext context, 
            Dictionary<string, FigmaElement> sessionCache,
            Action<int, int, string> onProgress, 
            CancellationToken ct)
        {
            if (context.ImageNodesToDownload.Count == 0) return;

            var apiClient = new FigmaAPIClient(_accessToken);
            var nodeIds = context.ImageNodesToDownload.Select(n => n.id).ToList();
            
            var links = await apiClient.GetImageLinksAsync(_fileId, nodeIds, 3f, "png", ct);
            if (links == null) return;

            string spriteFolder = _settings != null ? _settings.baseSpritesPath : "UI/Generated/Sprites";

            var semaphore = new SemaphoreSlim(10);
            int completed = 0;
            int totalDownloads = context.ImageNodesToDownload.Count(n => links.ContainsKey(n.id));

            var downloadTasks = new List<Task<(FigmaNode node, byte[] data)>>();
            foreach (var node in context.ImageNodesToDownload)
            {
                if (!links.ContainsKey(node.id)) continue;
                string url = links[node.id];
                
                var task = DownloadWithThrottle(semaphore, node, url, ct);
                downloadTasks.Add(task);
            }

            var results = await Task.WhenAll(downloadTasks);

            foreach (var (node, data) in results)
            {
                if (data == null) continue;
                completed++;
                onProgress?.Invoke(completed, totalDownloads, $"Importing image {completed}/{totalDownloads}");
                
                string fileName = $"{prefix}_{node.name}_{node.id.Replace(":", "_")}.png";
                string relativePath = Path.Combine(spriteFolder, fileName);
                Sprite sprite = FigmaAssetDownloader.ImportDataAsSprite(data, relativePath);
                
                if (sprite != null && sessionCache.ContainsKey(node.id))
                {
                    var img = sessionCache[node.id].GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = sprite;
                        img.color = Color.white;
                        
                        if (node.name.EndsWith("_9slice", StringComparison.OrdinalIgnoreCase))
                        {
                            Apply9SliceBorder(sprite, relativePath);
                            img.type = Image.Type.Sliced;
                            Debug.Log($"[FigmaImporter] Applied 9-Slice to '{node.name}'");
                        }
                    }
                }
            }

            Debug.Log($"<color=cyan>[FigmaImporter] Downloaded {completed}/{totalDownloads} images.</color>");
        }

        private async Task<(FigmaNode node, byte[] data)> DownloadWithThrottle(
            SemaphoreSlim semaphore, FigmaNode node, string url, CancellationToken ct)
        {
            await semaphore.WaitAsync(ct);
            try
            {
                byte[] data = await FigmaAssetDownloader.DownloadImageDataAsync(url);
                return (node, data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FigmaImporter] Failed to download image for '{node.name}': {e.Message}");
                return (node, null);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void Apply9SliceBorder(Sprite sprite, string assetRelativePath)
        {
            string assetPath = "Assets/" + assetRelativePath;
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            float w = sprite.texture.width;
            float h = sprite.texture.height;
            float inset = Mathf.Min(w, h) * 0.25f;
            inset = Mathf.Max(inset, 4f);

            var spriteSheet = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSheet);
            spriteSheet.spriteBorder = new Vector4(inset, inset, inset, inset);
            importer.SetTextureSettings(spriteSheet);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
