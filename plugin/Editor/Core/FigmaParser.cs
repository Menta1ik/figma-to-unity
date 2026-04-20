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
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[Figma v2.4.0] Cannot run Sync without Token and File ID.");
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

            var allElements = rootCanvas.GetComponentsInChildren<FigmaElement>(true);
            EnsureUnpacked(rootCanvas.gameObject);
            _handlerContext.RootTransform = rootCanvas;
            _existingCache = allElements.Where(e => !string.IsNullOrEmpty(e.FigmaNodeId)).ToDictionary(e => e.FigmaNodeId, e => e);

            // NEW: Aggressively dismantle all legacy [Mask] containers before starting the sync.
            // This prevents "depth > 8" errors caused by old technical containers nesting recursively.
            DismantleAllMaskContainers(rootCanvas);

            Canvas canvas = rootCanvas.GetComponent<Canvas>();
            CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
            
            // Force reset Root Canvas position and pivot to ensure it's not shifted off-screen
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

                int total = topNodes.Sum(n => CountNodes(n));
                int current = 0;

                foreach (var node in topNodes)
                {
                    SyncRecursive(node, rootCanvas, rootCanvas.name, ref current, total, onProgress, ct, 0);
                }
                
                ApplyDeferredMasks();
                CleanupOrphanedContainers(rootCanvas);
                _auditReport.PrintReport();

                if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
                {
                    var imageService = new ImageSyncService(_accessToken, _fileId, Settings);
                    await imageService.SyncImagesAsync(rootCanvas.name, _handlerContext, _sessionCache, onProgress, ct);
                }

                ApplyCanvasScaler(rootCanvas);
                Canvas.ForceUpdateCanvases(); 
            }
            finally
            {
                if (canvas != null) canvas.enabled = wasCanvasEnabled;
                if (scaler != null) scaler.enabled = wasScalerEnabled;
                EditorUtility.ClearProgressBar();
            }

            HandleDeletedElements();
            CleanupOrphanedContainers(rootCanvas);
            
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
            
            _existingCache = new Dictionary<string, FigmaElement>();
            var allElements = target.GetComponentsInChildren<FigmaElement>(true);
            foreach (var e in allElements) if (!string.IsNullOrEmpty(e.FigmaNodeId)) _existingCache[e.FigmaNodeId] = e;

            DismantleAllMaskContainers(target);

            var reskinHandler = new ReskinHandler(_handlerContext);
            reskinHandler.ApplyReskin(target.gameObject, newNode);

            if (DownloadImages && _handlerContext.ImageNodesToDownload.Count > 0)
            {
                var imageService = new ImageSyncService(_accessToken, _fileId, Settings);
                await imageService.SyncImagesAsync(target.name, _handlerContext, _sessionCache, null, ct);
            }

            Debug.Log($"<color=cyan>[Figma v2.4.0] Reskin completed for {target.name}!</color>");
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

        private void SyncRecursive(FigmaNode node, Transform parent, string path, ref int current, int total, Action<int, int, string> onProgress, CancellationToken ct, int depth)
        {
            if (node == null) return;
            ct.ThrowIfCancellationRequested();

            current++;
            onProgress?.Invoke(current, total, node.name);

            FigmaElement element = null;
            if (_existingCache != null && _existingCache.ContainsKey(node.id))
            {
                element = _existingCache[node.id];
                // CRITICAL: Pull element out of any existing [Mask] containers and reset to the logical parent
                element.transform.SetParent(parent, false);
                UpdatedCount++;
            }
            else
            {
                EnsureUnpacked(parent.gameObject);
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
                catch (Exception e) { Debug.LogError($"[Figma v2.4.0] Error in {handler.GetType().Name} for {node.name}: {e.Message}"); }
            }

            if (node.isMask || node.clipsContent)
            {
                _deferredMasks.Add((node, element, depth));
            }

            if (node.children != null)
            {
                var previousParent = _handlerContext.ParentNode;
                _handlerContext.ParentNode = node;
                
                try
                {
                    foreach (var child in node.children) 
                    {
                        SyncRecursive(child, element.transform, path + "/" + node.name, ref current, total, onProgress, ct, depth + 1);
                    }
                }
                finally
                {
                    _handlerContext.ParentNode = previousParent;
                }
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
        /// Figma masks affect subsequent siblings, while Unity masks affect children.
        /// We solve this by creating a container for the mask and its siblings.
        /// </summary>
        private void ApplyDeferredMasks()
        {
            if (_deferredMasks == null || _deferredMasks.Count == 0) return;

            foreach (var (maskNode, maskElement, depth) in _deferredMasks)
            {
                if (maskElement == null || maskElement.gameObject == null) continue;

                var maskGo = maskElement.gameObject;
                var maskTransform = maskGo.transform;
                var parentTransform = maskTransform.parent;

                if (parentTransform == null) continue;

                // Handle standard Frame clipping (clipsContent)
                if (!maskNode.isMask && maskNode.clipsContent)
                {
                    if (maskGo.GetComponent<RectMask2D>() == null && maskGo.GetComponent<Mask>() == null)
                    {
                        maskGo.AddComponent<RectMask2D>();
                    }
                    continue;
                }

                // Handle Figma Mask (isMask: true)
                if (maskNode.isMask)
                {
                    int maskSiblingIndex = maskTransform.GetSiblingIndex();
                    
                    var containerGo = new GameObject($"[Mask] {maskGo.name}");
                    var containerRect = containerGo.AddComponent<RectTransform>();
                    containerGo.transform.SetParent(parentTransform, false);
                    containerGo.transform.SetSiblingIndex(maskSiblingIndex);

                    var maskRect = maskGo.GetComponent<RectTransform>();
                    if (maskRect != null)
                    {
                        containerRect.anchorMin        = maskRect.anchorMin;
                        containerRect.anchorMax        = maskRect.anchorMax;
                        containerRect.pivot            = maskRect.pivot;
                        containerRect.sizeDelta        = maskRect.sizeDelta;
                        containerRect.anchoredPosition = maskRect.anchoredPosition;
                    }

                    // HEURISTIC: Calculate current stencil depth in Unity hierarchy
                    int currentStencilDepth = 0;
                    Transform t = containerGo.transform.parent;
                    while (t != null)
                    {
                        if (t.GetComponent<Mask>() != null) currentStencilDepth++;
                        t = t.parent;
                    }

                    // Strict complexity check: ONLY VECTOR and BOOLEAN_OPERATION can use Stencil
                    // Everything else (Rectangle, Star, Polygon, RegularPolygon, Frame) uses RectMask2D
                    bool isComplex = (maskNode.type == "VECTOR" || maskNode.type == "BOOLEAN_OPERATION" || maskNode.type == "STAR");
                    
                    // Force RectMask2D if we reached a safe stencil limit (3 is safest for complex nested UI)
                    // or if it's a simple shape.
                    bool forceRectMask = (currentStencilDepth >= 3) || !isComplex;
                    
                    if (forceRectMask)
                    {
                        if (containerGo.GetComponent<Mask>() != null) UnityEngine.Object.DestroyImmediate(containerGo.GetComponent<Mask>());
                        if (containerGo.GetComponent<Image>() != null) containerGo.GetComponent<Image>().enabled = false;
                        
                        if (containerGo.GetComponent<RectMask2D>() == null)
                            containerGo.AddComponent<RectMask2D>();
                            
                        string reason = !isComplex ? "Simple Shape" : "Depth Limit Reached";
                        Debug.Log($"[Mask Optimization] '{maskGo.name}' (type: {maskNode.type}) using RectMask2D (Reason: {reason}, Current Depth: {currentStencilDepth})");
                    }
                    else
                    {
                        // CLEANUP: Ensure no conflicting components exist
                        if (containerGo.GetComponent<RectMask2D>() != null) UnityEngine.Object.DestroyImmediate(containerGo.GetComponent<RectMask2D>());
                        if (maskGo.GetComponent<RectMask2D>() != null) UnityEngine.Object.DestroyImmediate(maskGo.GetComponent<RectMask2D>());
                        if (maskGo.GetComponent<Mask>() != null) UnityEngine.Object.DestroyImmediate(maskGo.GetComponent<Mask>());
                        
                        // Use Stencil Mask for complex shapes
                        var maskImage = containerGo.GetComponent<Image>() ?? containerGo.AddComponent<Image>();
                        maskImage.enabled = true;
                        maskImage.color = new Color(1, 1, 1, 0.01f);
                        maskImage.raycastTarget = false;
                        
                        var sourceImage = maskGo.GetComponent<Image>();
                        if (sourceImage != null && sourceImage.sprite != null)
                        {
                            maskImage.sprite = sourceImage.sprite;
                            maskImage.type = sourceImage.type;
                        }
                        
                        if (containerGo.GetComponent<Mask>() == null)
                            containerGo.AddComponent<Mask>().showMaskGraphic = false;
                            
                        Debug.Log($"[Mask Optimization] '{maskGo.name}' using STENCIL Mask (Complex Shape and Depth: {currentStencilDepth})");
                    }

                    // Move original element and siblings into container
                    maskTransform.SetParent(containerRect, false);

                    var siblingsToMask = new List<Transform>();
                    for (int i = maskSiblingIndex + 1; i < parentTransform.childCount; i++)
                    {
                        var sibling = parentTransform.GetChild(i);
                        if (sibling != containerRect.transform)
                            siblingsToMask.Add(sibling);
                    }

                    foreach (var sibling in siblingsToMask)
                    {
                        EnsureUnpacked(sibling.gameObject);
                        sibling.SetParent(containerRect, true);
                    }
                }
            }
        }

        private void CleanupOrphanedContainers(Transform root)
        {
            var allCandidates = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = allCandidates.Length - 1; i >= 0; i--)
            {
                var rt = allCandidates[i];
                if (rt != null && rt.name.StartsWith("[Mask]") && rt.GetComponent<FigmaElement>() == null)
                {
                    bool anyManagedChild = false;
                    foreach (Transform child in rt) if (child.GetComponent<FigmaElement>() != null) anyManagedChild = true;
                    if (!anyManagedChild) UnityEngine.Object.DestroyImmediate(rt.gameObject);
                }
            }
        }

        private void DismantleAllMaskContainers(Transform root)
        {
            var all = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                var rt = all[i];
                if (rt != null && rt.name.StartsWith("[Mask]"))
                {
                    Transform parent = rt.parent;
                    if (parent != null)
                    {
                        int siblingIndex = rt.GetSiblingIndex();
                        var children = new List<Transform>();
                        foreach (Transform child in rt) children.Add(child);
                        
                        foreach (Transform child in children)
                        {
                            child.SetParent(parent, true);
                            child.SetSiblingIndex(siblingIndex++);
                        }
                    }
                    UnityEngine.Object.DestroyImmediate(rt.gameObject);
                }
            }
        }

        private void EnsureUnpacked(GameObject go)
        {
            if (go == null) return;
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root != null)
                {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
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
                    
                    // Auto-detect resolution if settings are default (X=0 and Y=0)
                    if (refRes.x <= 1 && refRes.y <= 1)
                    {
                        // Use the size of the first top node as the reference
                        var rootRt = rootCanvas.GetComponent<RectTransform>();
                        if (rootRt != null)
                        {
                            // We find the first figma element under root to get its size
                            var firstChild = rootCanvas.GetComponentInChildren<FigmaElement>();
                            if (firstChild != null)
                            {
                                refRes = new Vector2(firstChild.AbsoluteBox.width, firstChild.AbsoluteBox.height);
                                Debug.Log($"[Figma v2.4.0] Auto-detected Reference Resolution from Frame: {refRes.x}x{refRes.y}");
                            }
                        }
                    }
                    
                    // Final safety check to avoid division by zero in Unity
                    if (refRes.x <= 1) refRes.x = 1080;
                    if (refRes.y <= 1) refRes.y = 1920;

                    scaler.referenceResolution = refRes;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = Settings.matchWidthOrHeight;
                    break;
            }
            
            EditorUtility.SetDirty(scaler);
            Debug.Log($"[Figma v2.4.0] CanvasScaler updated to {Settings.canvasScaleMode}");
        }
    }
// [Figma-to-Unity] Stencil Mask Guard and Hierarchy Optimization v2.4.0.
}
