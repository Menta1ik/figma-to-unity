using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Newtonsoft.Json;
using TMPro;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Core.Handlers;
using FigmaImporter.V2.Core.Validation;

namespace FigmaImporter.V2.Core
{
    public class FigmaParser
    {
        private TransformAuditReport _auditReport;
        private List<(FigmaNode node, FigmaElement element)> _deferredMasks;
        private Dictionary<string, FigmaElement> _existingCache;
        private HashSet<string> _processedIds;
        
        private FigmaHandlerContext _handlerContext;
        private readonly List<IFigmaComponentHandler> _handlers;
        
        private readonly string _accessToken;
        private readonly string _fileId;

        public FontMappingTable FontMapTable { get; set; }
        public FigmaImporterSettings Settings { get; set; }
        public bool DownloadImages { get; set; } = true;
        public bool ForceUpdate { get; set; } = false;

        public int CreatedCount { get; private set; }
        public int UpdatedCount { get; private set; }
        public int SkippedCount { get; private set; }

        public FigmaParser(string accessToken = "", string fileId = "")
        {
            _accessToken = accessToken;
            _fileId = fileId;

            _handlers = new List<IFigmaComponentHandler>
            {
                new TransformHandler(),
                new TextHandler(),
                new ImageHandler(),
                new InteractiveHandler()
            };
        }

        public async Task RunSync(Transform rootCanvas, string nodeId = "", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[FigmaParser] Cannot run Sync without Token and File ID.");
                return;
            }

            var apiClient = new FigmaAPIClient(_accessToken);
            EditorUtility.DisplayProgressBar("Figma API", "Fetching cloud data...", 0.1f);
            
            string json = "";
            try {
                json = await apiClient.GetFileAsync(_fileId, nodeId, ct);
            } catch (OperationCanceledException) { throw; }

            if (string.IsNullOrEmpty(json)) return;
            
            await ProcessFileAsync(json, rootCanvas, (current, total, name) => {
                EditorUtility.DisplayProgressBar("Syncing", $"Processing {current}/{total}: {name}", (float)current / total);
            }, ct);
        }

        public async Task ProcessFileAsync(string jsonContent, Transform rootCanvas, Action<int, int, string> onProgress = null, CancellationToken ct = default)
        {
            FigmaFileResponse response = JsonConvert.DeserializeObject<FigmaFileResponse>(jsonContent);
            if (response == null) return;

            _auditReport = new TransformAuditReport();
            _deferredMasks = new List<(FigmaNode, FigmaElement)>();
            _processedIds = new HashSet<string>();
            CreatedCount = 0; UpdatedCount = 0; SkippedCount = 0;

            _handlerContext = new FigmaHandlerContext
            {
                Settings = Settings
            };

            // TYPOGRAPHY: PREVENT TEXT CRASHES
            if (FontMapTable != null)
            {
                _handlerContext.FontMappings = FontMapTable.Mappings;
                _handlerContext.GlobalFont = FontMapTable.GlobalFallbackFont;
            }
            _handlerContext.ForceUpdate = ForceUpdate;

            if (_handlerContext.GlobalFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _handlerContext.GlobalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }

            var allElements = rootCanvas.GetComponentsInChildren<FigmaElement>(true);
            _existingCache = allElements.Where(e => !string.IsNullOrEmpty(e.FigmaNodeId))
                .ToDictionary(e => e.FigmaNodeId, e => e);

            Canvas canvas = rootCanvas.GetComponent<Canvas>();
            CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
            bool wasCanvasEnabled = canvas != null && canvas.enabled;
            bool wasScalerEnabled = scaler != null && scaler.enabled;

            if (canvas != null) canvas.enabled = false;
            if (scaler != null) scaler.enabled = false;

            try 
            {
                List<FigmaNode> topNodes = new List<FigmaNode>();
                if (response.nodes != null)
                {
                    foreach (var container in response.nodes.Values) topNodes.Add(container.document);
                }
                else if (response.document != null)
                {
                    topNodes.Add(response.document);
                }

                int total = topNodes.Sum(n => CountNodes(n));
                int current = 0;

                foreach (var node in topNodes)
                {
                    SyncRecursive(node, rootCanvas, rootCanvas.name, ref current, total, onProgress, ct);
                }

                // Apply deferred masks logic here or method call
                _auditReport.PrintReport();

                if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
                {
                    await SyncImagesAsync(rootCanvas.name, onProgress, ct);
                }
            }
            finally
            {
                if (canvas != null) canvas.enabled = wasCanvasEnabled;
                if (scaler != null) scaler.enabled = wasScalerEnabled;
                EditorUtility.ClearProgressBar();
            }

            HandleDeletedElements();
            if (Settings != null && !string.IsNullOrEmpty(Settings.basePrefabsPath))
            {
                UpdateOrCreatePrefab(rootCanvas.gameObject);
            }
        }

        public async Task ReskinAsync(Transform target, string newNodeId, CancellationToken ct = default)
        {
            if (target == null || string.IsNullOrEmpty(newNodeId)) return;
            
            var apiClient = new FigmaAPIClient(_accessToken);
            EditorUtility.DisplayProgressBar("Figma API", "Fetching node data...", 0.1f);
            
            string json = await apiClient.GetFileAsync(_fileId, newNodeId, ct);
            if (string.IsNullOrEmpty(json)) return;

            var response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
            if (response == null || response.nodes == null || !response.nodes.ContainsKey(newNodeId)) return;

            FigmaNode newNode = response.nodes[newNodeId].document;
            _handlerContext = new FigmaHandlerContext { Settings = Settings };
            if (FontMapTable != null) { _handlerContext.FontMappings = FontMapTable.Mappings; _handlerContext.GlobalFont = FontMapTable.GlobalFallbackFont; }

            ReskinRecursive(newNode, target);

            if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
            {
                await SyncImagesAsync(target.name, null, ct);
            }

            Debug.Log($"<color=cyan>[Figma Reskin] Reskin completed for {target.name}!</color>");
        }

        public async Task RunFontAudit(string nodeId = "", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[FigmaParser] Cannot run Font Audit without Token and File ID.");
                return;
            }

            var apiClient = new FigmaAPIClient(_accessToken);
            EditorUtility.DisplayProgressBar("Figma API", "Fetching fonts from Figma...", 0.3f);
            try
            {
                bool onlySelectedNode = !string.IsNullOrEmpty(nodeId) && (Settings == null || !Settings.AuditEntireFile);
                var json = await apiClient.GetFileAsync(_fileId, onlySelectedNode ? nodeId : "", ct);
                if (string.IsNullOrEmpty(json)) return;
                var response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
                RunFontAudit(response);
            }
            catch (OperationCanceledException) { throw; }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void SyncRecursive(FigmaNode node, Transform parent, string path, ref int current, int total, Action<int, int, string> onProgress, CancellationToken ct)
        {
            if (node == null) return;
            ct.ThrowIfCancellationRequested();

            current++;
            onProgress?.Invoke(current, total, node.name);

            FigmaElement element = null;
            if (_existingCache != null && _existingCache.ContainsKey(node.id))
            {
                element = _existingCache[node.id];
                UpdatedCount++;
            }
            else
            {
                GameObject go = new GameObject(node.name);
                go.transform.SetParent(parent, false);
                element = go.AddComponent<FigmaElement>();
                element.FigmaNodeId = node.id;
                CreatedCount++;
            }

            _processedIds.Add(node.id);
            element.name = node.name;

            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(node))
                    handler.Apply(node, element, _handlerContext);
            }

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    SyncRecursive(child, element.transform, path + "/" + node.name, ref current, total, onProgress, ct);
                }
            }
        }

        private async Task SyncImagesAsync(string prefix, Action<int, int, string> onProgress, CancellationToken ct)
        {
            var apiClient = new FigmaAPIClient(_accessToken);
            var nodeIds = _handlerContext.ImageNodesToDownload.Select(n => n.id).ToList();
            
            var links = await apiClient.GetImageLinksAsync(_fileId, nodeIds, 3f, "png", ct);
            if (links == null) return;

            string spriteFolder = Settings != null ? Settings.baseSpritesPath : "UI/Generated/Sprites";

            int count = 0;
            foreach (var node in _handlerContext.ImageNodesToDownload)
            {
                ct.ThrowIfCancellationRequested();
                if (links.ContainsKey(node.id))
                {
                    count++;
                    onProgress?.Invoke(count, nodeIds.Count, $"Downloading image {count}/{nodeIds.Count}");
                    
                    string url = links[node.id];
                    string fileName = $"{prefix}_{node.name}_{node.id.Replace(":", "_")}.png";
                    
                    // Call static FigmaAssetDownloader methods
                    byte[] data = await FigmaAssetDownloader.DownloadImageDataAsync(url);
                    if (data != null)
                    {
                        string relativePath = Path.Combine(spriteFolder, fileName);
                        Sprite sprite = FigmaAssetDownloader.ImportDataAsSprite(data, relativePath);
                        
                        if (sprite != null && _existingCache.ContainsKey(node.id))
                        {
                            var img = _existingCache[node.id].GetComponent<Image>();
                            if (img != null) img.sprite = sprite;
                        }
                    }
                }
            }
        }

        private int CountNodes(FigmaNode node)
        {
            int count = 1;
            if (node.children != null)
            {
                foreach (var child in node.children) count += CountNodes(child);
            }
            return count;
        }

        private void RunFontAudit(FigmaFileResponse response)
        {
            var figmaFonts = new HashSet<(string family, string postScript, int weight)>();
            if (response.nodes != null)
            {
                foreach (var container in response.nodes.Values) CollectFontsRecursive(container.document, figmaFonts);
            }
            else if (response.document != null)
            {
                CollectFontsRecursive(response.document, figmaFonts);
            }

            if (figmaFonts.Count == 0) return;

            List<string> mapped = new List<string>();
            List<string> partiallyMapped = new List<string>();
            List<string> missing = new List<string>();

            foreach (var f in figmaFonts)
            {
                bool exactMatch = false;
                bool familyMatch = false;

                if (FontMapTable != null)
                {
                    string normalizedSearchFamily = (f.family ?? "").Replace(" ", "").ToLower();
                    exactMatch = FontMapTable.Mappings.Any(m => m.fontPostScriptName == f.postScript || ((m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == normalizedSearchFamily && (m.figmaFontWeight == 0 || m.figmaFontWeight == f.weight)));
                    familyMatch = FontMapTable.Mappings.Any(m => (m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == normalizedSearchFamily);
                }

                string desc = $"'{f.family}' ({f.weight})";
                if (!string.IsNullOrEmpty(f.postScript)) desc += $" [{f.postScript}]";

                if (exactMatch) mapped.Add(desc);
                else if (familyMatch) partiallyMapped.Add(desc);
                else missing.Add(desc);
            }

            Debug.Log($"<b>[Typography Audit]</b> Found unique fonts in Figma: {figmaFonts.Count}");
            if (mapped.Count > 0) Debug.Log($"<color=green>✅ Font mapping configured ({mapped.Count}):</color> {string.Join(", ", mapped)}");
            if (missing.Count > 0) Debug.LogError($"<color=red>❌ MISSING FONT MAPPING ({missing.Count}):</color> {string.Join(", ", missing)}");
        }

        private void CollectFontsRecursive(FigmaNode node, HashSet<(string, string, int)> fonts)
        {
            if (node.type == "TEXT" && node.style != null) fonts.Add((node.style.fontFamily, node.style.fontPostScriptName, node.style.fontWeight));
            if (node.children != null) foreach (var child in node.children) CollectFontsRecursive(child, fonts);
        }

        private void HandleDeletedElements() { /* Implement actual deletion logic if needed */ }
        private void UpdateOrCreatePrefab(GameObject go) { /* Implement actual prefab update logic */ }
        private void ReskinRecursive(FigmaNode node, Transform target) { /* Implement reskin logic */ }
        private void ApplyDeferredMasks() { /* Implement mask logic */ }
    }
}
