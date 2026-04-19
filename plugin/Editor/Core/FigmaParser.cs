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
using FigmaImporter.V2.Runtime;
using FigmaImporter.V2.Core.Services;

namespace FigmaImporter.V2.Core
{
    public class FigmaParser
    {
        private TransformAuditReport _auditReport;
        private List<(FigmaNode node, FigmaElement element)> _deferredMasks;
        private Dictionary<string, FigmaElement> _existingCache;
        private Dictionary<string, FigmaElement> _sessionCache; 
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
                new LayoutHandler(),
                new TextHandler(),
                new ImageHandler(),
                new InteractiveHandler()
            };
        }

        public async Task RunSync(Transform rootCanvas, string nodeId = "", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[Figma v2.3.0] Cannot run Sync without Token and File ID.");
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
            _sessionCache = new Dictionary<string, FigmaElement>();
            CreatedCount = 0; UpdatedCount = 0; SkippedCount = 0;

            _handlerContext = new FigmaHandlerContext { Settings = Settings, ForceUpdate = ForceUpdate };

            if (FontMapTable != null)
            {
                _handlerContext.FontMappings = FontMapTable.Mappings;
                _handlerContext.GlobalFont = FontMapTable.GlobalFallbackFont;
            }

            if (_handlerContext.GlobalFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _handlerContext.GlobalFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }

            if (_handlerContext.GlobalFont == null)
            {
                EditorUtility.DisplayDialog("Figma Import Error", "Global Fallback Font is not set!", "OK");
                return;
            }

            var allElements = rootCanvas.GetComponentsInChildren<FigmaElement>(true);
            _existingCache = allElements.Where(e => !string.IsNullOrEmpty(e.FigmaNodeId)).ToDictionary(e => e.FigmaNodeId, e => e);

            Canvas canvas = rootCanvas.GetComponent<Canvas>();
            CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
            bool wasCanvasEnabled = canvas != null && canvas.enabled;
            bool wasScalerEnabled = scaler != null && scaler.enabled;

            if (canvas != null) canvas.enabled = false;
            if (scaler != null) scaler.enabled = false;

            var rootElement = rootCanvas.GetComponent<FigmaElement>() ?? rootCanvas.gameObject.AddComponent<FigmaElement>();
            
            try 
            {
                List<FigmaNode> topNodes = new List<FigmaNode>();
                if (response.nodes != null) foreach (var container in response.nodes.Values) topNodes.Add(container.document);
                else if (response.document != null) topNodes.Add(response.document);

                if (topNodes.Count == 0) return;

                if (topNodes[0].absoluteBoundingBox != null)
                {
                    var bbox = topNodes[0].absoluteBoundingBox;
                    rootElement.AbsoluteBox = new Rect(bbox.x, bbox.y, bbox.width, bbox.height);
                }

                int total = topNodes.Sum(n => CountNodes(n));
                int current = 0;

                foreach (var node in topNodes)
                {
                    SyncRecursive(node, rootCanvas, rootCanvas.name, ref current, total, onProgress, ct);
                }
                
                ApplyDeferredMasks();
                _auditReport.PrintReport();

                if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
                {
                    var imageService = new ImageSyncService(_accessToken, _fileId, Settings);
                    await imageService.SyncImagesAsync(rootCanvas.name, _handlerContext, _sessionCache, onProgress, ct);
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
                new PrefabManager(Settings).UpdateOrCreatePrefab(rootCanvas.gameObject);
            }
        }

        public async Task ReskinAsync(Transform target, string newNodeId, CancellationToken ct = default)
        {
            if (target == null || string.IsNullOrEmpty(newNodeId)) return;
            
            var apiClient = new FigmaAPIClient(_accessToken);
            string json = await apiClient.GetFileAsync(_fileId, newNodeId, ct);
            if (string.IsNullOrEmpty(json)) return;

            var response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
            if (response == null || response.nodes == null || !response.nodes.ContainsKey(newNodeId)) return;

            FigmaNode newNode = response.nodes[newNodeId].document;
            _handlerContext = new FigmaHandlerContext { Settings = Settings };
            if (FontMapTable != null) { _handlerContext.FontMappings = FontMapTable.Mappings; _handlerContext.GlobalFont = FontMapTable.GlobalFallbackFont; }
            
            _sessionCache = new Dictionary<string, FigmaElement>();
            var allElements = target.GetComponentsInChildren<FigmaElement>(true);
            foreach (var e in allElements) if (!string.IsNullOrEmpty(e.FigmaNodeId)) _sessionCache[e.FigmaNodeId] = e;

            var reskinHandler = new ReskinHandler(_handlerContext);
            reskinHandler.ApplyReskin(target.gameObject, newNode);

            if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
            {
                var imageService = new ImageSyncService(_accessToken, _fileId, Settings);
                await imageService.SyncImagesAsync(target.name, _handlerContext, _sessionCache, null, ct);
            }

            Debug.Log($"<color=cyan>[Figma v2.3.0] Reskin completed for {target.name}!</color>");
        }

        public async Task RunFontAudit(string nodeId = "", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId)) return;

            var apiClient = new FigmaAPIClient(_accessToken);
            EditorUtility.DisplayProgressBar("Figma API", "Fetching fonts...", 0.3f);
            try
            {
                bool onlySelectedNode = !string.IsNullOrEmpty(nodeId) && (Settings == null || !Settings.AuditEntireFile);
                var json = await apiClient.GetFileAsync(_fileId, onlySelectedNode ? nodeId : "", ct);
                if (string.IsNullOrEmpty(json)) return;
                var response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
                RunFontAudit(response);
            }
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
                if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
                element.FigmaNodeId = node.id;
                CreatedCount++;
            }

            _processedIds.Add(node.id);
            _sessionCache[node.id] = element; 
            
            if (Settings == null || !Settings.preserveUnityNames) element.name = node.name;

            foreach (var handler in _handlers)
            {
                try { if (handler.CanHandle(node)) handler.Apply(node, element, _handlerContext); }
                catch (Exception e) { Debug.LogError($"[Figma v2.3.0] Error in {handler.GetType().Name} for {node.name}: {e.Message}"); }
            }

            if (node.isMask || node.clipsContent) _deferredMasks.Add((node, element));

            if (node.children != null)
            {
                foreach (var child in node.children) SyncRecursive(child, element.transform, path + "/" + node.name, ref current, total, onProgress, ct);
            }
        }

        private int CountNodes(FigmaNode node)
        {
            int count = 1;
            if (node.children != null) foreach (var child in node.children) count += CountNodes(child);
            return count;
        }

        private void RunFontAudit(FigmaFileResponse response)
        {
            var figmaFonts = new HashSet<(string family, string postScript, int weight)>();
            if (response.nodes != null) foreach (var container in response.nodes.Values) CollectFontsRecursive(container.document, figmaFonts);
            else if (response.document != null) CollectFontsRecursive(response.document, figmaFonts);

            if (figmaFonts.Count == 0) return;

            List<string> mapped = new List<string>(), missing = new List<string>();

            foreach (var f in figmaFonts)
            {
                bool match = false;
                if (FontMapTable != null)
                {
                    string norm = (f.family ?? "").Replace(" ", "").ToLower();
                    match = FontMapTable.Mappings.Any(m => m.fontPostScriptName == f.postScript || ((m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == norm && (m.figmaFontWeight == 0 || m.figmaFontWeight == f.weight)));
                }

                string desc = $"'{f.family}' ({f.weight})";
                if (match) mapped.Add(desc); else missing.Add(desc);
            }

            if (mapped.Count > 0) Debug.Log($"<color=green>✅ Mapped ({mapped.Count}):</color> {string.Join(", ", mapped)}");
            if (missing.Count > 0) Debug.LogError($"<color=red>❌ MISSING ({missing.Count}):</color> {string.Join(", ", missing)}");
        }

        private void CollectFontsRecursive(FigmaNode node, HashSet<(string, string, int)> fonts)
        {
            if (node.type == "TEXT" && node.style != null) fonts.Add((node.style.fontFamily, node.style.fontPostScriptName, node.style.fontWeight));
            if (node.children != null) foreach (var child in node.children) CollectFontsRecursive(child, fonts);
        }

        private void HandleDeletedElements() 
        {
            if (_existingCache == null) return;
            foreach (var kvp in _existingCache)
            {
                if (!_processedIds.Contains(kvp.Key) && kvp.Value != null)
                {
                    GameObject go = kvp.Value.gameObject;
                    go.SetActive(false);
                    var orphan = go.GetComponent<FigmaOrphanedElement>() ?? go.AddComponent<FigmaOrphanedElement>();
                    orphan.Initialize(kvp.Key);
                }
            }
        }

        /// <summary>
        /// Finalizes mask components after the entire hierarchy is created.
        /// Figma masks affect siblings, while Unity masks affect children.
        /// current logic approximates this by putting the mask on the parent.
        /// </summary>
        private void ApplyDeferredMasks()
        {
            if (_deferredMasks == null || _deferredMasks.Count == 0) return;

            foreach (var (node, element) in _deferredMasks)
            {
                if (element == null) continue;
                var go = element.gameObject;

                // Case 1: Figma "isMask" property
                // We apply Unity Mask to the PARENT, which masks all its children.
                if (node.isMask)
                {
                    var parent = go.transform.parent;
                    if (parent != null)
                    {
                        var pgo = parent.gameObject;
                        if (pgo.GetComponent<Mask>() == null && pgo.GetComponent<RectMask2D>() == null)
                        {
                            // Mask needs an Image component to define the area (even if invisible)
                            if (pgo.GetComponent<Image>() == null) pgo.AddComponent<Image>().color = new Color(1, 1, 1, 0);
                            var mask = pgo.AddComponent<Mask>();
                            mask.showMaskGraphic = false;
                        }
                    }
                }
                // Case 2: Figma "clipsContent" (standard Frame clipping)
                else if (node.clipsContent && go.GetComponent<RectMask2D>() == null && go.GetComponent<Mask>() == null)
                {
                    go.AddComponent<RectMask2D>();
                }
            }
        }
    }
}
