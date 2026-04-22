using System;
using System.Collections.Generic;
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
using FigmaImporter.V2;

namespace FigmaImporter.V2.Core
{
    public class FigmaParser
    {
        private TransformAuditReport _auditReport;
        private List<(FigmaNode node, FigmaElement element, int depth)> _deferredMasks;
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
            if (Settings != null) FigmaLog.SetLevel(Settings.logLevel);

            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                FigmaLog.Error($"{FigmaLog.VersionPrefix}Cannot run Sync without Token and File ID.");
                return;
            }

            var apiClient = new FigmaAPIClient(_accessToken);
            var cache = new FigmaResponseCache();

            // Step 1: Check file version (lightweight API call)
            EditorUtility.DisplayProgressBar("Figma API", "Checking file version...", 0.05f);
            string version = null;
            try { version = await apiClient.GetFileVersionAsync(_fileId, ct); }
            catch (OperationCanceledException) { throw; }

            // Step 2: Try cache
            string json = null;
            if (!string.IsNullOrEmpty(version))
                json = cache.TryLoadCached(_fileId, nodeId, version);

            // Step 3: If cache miss, fetch full file
            if (string.IsNullOrEmpty(json))
            {
                EditorUtility.DisplayProgressBar("Figma API", "Fetching cloud data...", 0.1f);
                try { json = await apiClient.GetFileAsync(_fileId, nodeId, ct); }
                catch (OperationCanceledException) { throw; }

                // Save to cache for next time
                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(version))
                    cache.SaveToCache(_fileId, nodeId, version, json);
            }

            if (string.IsNullOrEmpty(json)) return;

            await ProcessFileAsync(json, rootCanvas, (current, total, name) => {
                EditorUtility.DisplayProgressBar("Syncing", $"Processing {current}/{total}: {name}", (float)current / total);
            }, ct);
        }

        public async Task ProcessFileAsync(string jsonContent, Transform rootCanvas, Action<int, int, string> onProgress = null, CancellationToken ct = default)
        {
            if (Settings != null) FigmaLog.SetLevel(Settings.logLevel);

            FigmaFileResponse response = JsonConvert.DeserializeObject<FigmaFileResponse>(jsonContent);
            if (response == null) return;

            string importTargetName = "Canvas";
            if (response.nodes != null && response.nodes.Count > 0)
            {
                foreach (var node in response.nodes.Values)
                {
                    importTargetName = node.document.name;
                    break;
                }
            }
            else if (response.document != null)
            {
                importTargetName = response.document.name;
            }

            _auditReport = new TransformAuditReport();
            _deferredMasks = new List<(FigmaNode, FigmaElement, int)>();
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

            FigmaParserUtils.EnsureUnpacked(rootCanvas.gameObject);
            _handlerContext.RootTransform = rootCanvas;
            
            FigmaMaskResolver.DismantleAll(rootCanvas);

            // Re-fetch elements after dismantling masks to have a clean slate for existing objects
            var allElements = rootCanvas.GetComponentsInChildren<FigmaElement>(true);
            _existingCache = allElements
                .Where(e => e != null && !string.IsNullOrEmpty(e.FigmaNodeId))
                .ToDictionary(e => e.FigmaNodeId, e => e);

            Canvas canvas = rootCanvas.GetComponent<Canvas>();
            CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();

            RectTransform rootRt = rootCanvas.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                Undo.RecordObject(rootRt, "Reset Root Canvas Position");
                rootRt.anchoredPosition = Vector2.zero;
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.pivot = new Vector2(0.5f, 0.5f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
                rootRt.localScale = Vector3.one;
            }
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

                // Delegate tree walking to FigmaTreeWalker
                var walker = new FigmaTreeWalker(_handlers, _handlerContext, _existingCache, _sessionCache, _processedIds, _deferredMasks);
                walker.SyncAll(topNodes, rootCanvas, onProgress, ct);
                CreatedCount = walker.CreatedCount;
                UpdatedCount = walker.UpdatedCount;

                FigmaMaskResolver.ApplyDeferred(_deferredMasks);
                FigmaMaskResolver.CleanupOrphaned(rootCanvas);
                _auditReport.PrintReport();

                ApplyCanvasScaler(rootCanvas);
                Canvas.ForceUpdateCanvases();

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

            bool isSingleNodeSync = response.nodes != null && response.nodes.Count > 0;
            FigmaOrphanManager.MarkOrphans(_existingCache, _processedIds, isSingleNodeSync);
            FigmaMaskResolver.CleanupOrphaned(rootCanvas);

            if (Settings != null && !string.IsNullOrEmpty(Settings.basePrefabsPath))
            {
                GameObject targetToPrefab = rootCanvas.gameObject;

                foreach (Transform child in rootCanvas)
                {
                    var figmaElem = child.GetComponent<FigmaElement>();
                    if (figmaElem == null) continue;

                    bool idMatch = response.nodes != null && response.nodes.ContainsKey(figmaElem.FigmaNodeId);
                    bool nameMatch = child.name == importTargetName;

                    if (idMatch || nameMatch)
                    {
                        targetToPrefab = child.gameObject;
                        break;
                    }
                }

                if (targetToPrefab == rootCanvas.gameObject && response.nodes != null && response.nodes.Count == 1)
                {
                    foreach (Transform child in rootCanvas)
                    {
                        if (child.GetComponent<FigmaElement>() != null)
                        {
                            targetToPrefab = child.gameObject;
                            break;
                        }
                    }
                }

                new PrefabManager(Settings).UpdateOrCreatePrefab(targetToPrefab, importTargetName);
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

            _existingCache = new Dictionary<string, FigmaElement>();
            var allElements = target.GetComponentsInChildren<FigmaElement>(true);
            foreach (var e in allElements) if (!string.IsNullOrEmpty(e.FigmaNodeId)) _existingCache[e.FigmaNodeId] = e;

            FigmaMaskResolver.DismantleAll(target);

            var reskinHandler = new ReskinHandler(_handlerContext);
            reskinHandler.ApplyReskin(target.gameObject, newNode);

            if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
            {
                var imageService = new ImageSyncService(_accessToken, _fileId, Settings);
                await imageService.SyncImagesAsync(target.name, _handlerContext, _sessionCache, null, ct);
            }

            FigmaLog.Info($"<color=cyan>{FigmaLog.VersionPrefix}Reskin completed for {target.name}!</color>");
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
                new FigmaFontAuditor(FontMapTable).Audit(response);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private void ApplyCanvasScaler(Transform rootCanvas)
        {
            if (Settings == null || Settings.canvasScaleMode == FigmaImporterSettings.CanvasScaleMode.None) return;

            var scaler = rootCanvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;

            Undo.RecordObject(scaler, "Apply Figma Canvas Scaler");

            switch (Settings.canvasScaleMode)
            {
                case FigmaImporterSettings.CanvasScaleMode.ConstantPixelSize:
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    break;
                case FigmaImporterSettings.CanvasScaleMode.ScaleWithScreenSize:
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                    Vector2 refRes = Settings.referenceResolution;

                    if (refRes.x <= 1 && refRes.y <= 1)
                    {
                        var rootRt = rootCanvas.GetComponent<RectTransform>();
                        if (rootRt != null)
                        {
                            var firstChild = rootCanvas.GetComponentInChildren<FigmaElement>();
                            if (firstChild != null)
                            {
                                refRes = new Vector2(firstChild.AbsoluteBox.width, firstChild.AbsoluteBox.height);
                                FigmaLog.Info($"{FigmaLog.VersionPrefix}Auto-detected Reference Resolution from Frame: {refRes.x}x{refRes.y}");
                            }
                        }
                    }

                    if (refRes.x <= 1) refRes.x = 1080;
                    if (refRes.y <= 1) refRes.y = 1920;

                    scaler.referenceResolution = refRes;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = Settings.matchWidthOrHeight;
                    break;
            }

            EditorUtility.SetDirty(scaler);
            FigmaLog.Info($"{FigmaLog.VersionPrefix}CanvasScaler updated to {Settings.canvasScaleMode}");
        }
    }
}
