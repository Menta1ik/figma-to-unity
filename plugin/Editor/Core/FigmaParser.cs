using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string _fontMappingHash;
        
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

        public async System.Threading.Tasks.Task ProcessFileAsync(string jsonContent, Transform rootCanvas, Action<int, int, string> onProgress = null)
        {
            if (string.IsNullOrEmpty(jsonContent)) return;
            var response = JsonConvert.DeserializeObject<FigmaFileResponse>(jsonContent);
            if (response == null || (response.nodes == null && response.document == null)) return;

            // REQUIREMENT 1: FREEZE CANVAS
            var canvas = rootCanvas.GetComponentInParent<Canvas>();
            var scaler = rootCanvas.GetComponentInParent<CanvasScaler>();
            bool wasCanvasEnabled = canvas != null && canvas.enabled;
            bool wasScalerEnabled = scaler != null && scaler.enabled;

            if (canvas != null) canvas.enabled = false;
            if (scaler != null) scaler.enabled = false;

            _auditReport = new TransformAuditReport();
            _deferredMasks = new List<(FigmaNode, FigmaElement)>();
            _processedIds = new HashSet<string>();
            _handlerContext = new FigmaHandlerContext { Settings = Settings };
            _fontMappingHash = FontMapTable != null ? FontMapTable.GetTableHash() : "";
            
            CreatedCount = 0;
            UpdatedCount = 0;
            SkippedCount = 0;

            // RUN FONT AUDIT BEFORE STARTING
            RunFontAudit(response);

            // TYPOGRAPHY: PREVENT TEXT CRASHES
            if (FontMapTable != null)
            {
                _handlerContext.FontMappings = FontMapTable.Mappings;
                _handlerContext.GlobalFont = FontMapTable.GlobalFallbackFont;
            }
            _handlerContext.ForceUpdate = ForceUpdate;

            if (_handlerContext.GlobalFont == null)
            {
                // Look for any TMP_FontAsset as a fallback
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    _handlerContext.GlobalFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }

            var allElements = rootCanvas.GetComponentsInChildren<FigmaElement>(true);
            _existingCache = allElements.Where(e => !string.IsNullOrEmpty(e.FigmaNodeId))
                .ToDictionary(e => e.FigmaNodeId, e => e);

            try 
            {
                if (response.nodes != null)
                {
                    foreach (var container in response.nodes.Values)
                    {
                        SyncRecursive(container.document, rootCanvas, rootCanvas.name);
                    }
                }
                else if (response.document != null)
                {
                    SyncRecursive(response.document, rootCanvas, rootCanvas.name);
                }

                // REQUIREMENT 4 & 5: DEFERRED MASKS
                ApplyDeferredMasks();

                _auditReport.PrintReport();

                if (_handlerContext.ImageNodesToDownload.Count > 0)
                {
                    await SyncImagesAsync(rootCanvas.name, onProgress);
                }
            }
            finally
            {
                // GUARANTEED UNFREEZE (DESERIALIZATION COMPLETE)
                if (canvas != null) canvas.enabled = wasCanvasEnabled;
                if (scaler != null) scaler.enabled = wasScalerEnabled;
                
                EditorUtility.ClearProgressBar();
                EditorUtility.UnloadUnusedAssetsImmediate();
            }

            HandleDeletedElements();
            
            // SAVE AS PREFAB IF DIRECTED
            if (Settings != null && !string.IsNullOrEmpty(Settings.basePrefabsPath))
            {
                UpdateOrCreatePrefab(rootCanvas.gameObject);
            }
            
            Debug.Log($"<color=green>[Figma v2.1] Sync Completed!</color> " +
                      $"Created: {CreatedCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}");
        }

        private void UpdateOrCreatePrefab(GameObject root)
        {
            if (root == null || root.transform.childCount == 0) return;

            string folderPath = Path.Combine("Assets", Settings.basePrefabsPath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Iterate through all top-level children of the root
            for (int i = 0; i < root.transform.childCount; i++)
            {
                GameObject child = root.transform.GetChild(i).gameObject;
                FigmaElement element = child.GetComponent<FigmaElement>();
                
                // Only save as prefab if this element was processed in the current session
                if (element != null && _processedIds.Contains(element.FigmaNodeId))
                {
                    string prefabPath = Path.Combine(folderPath, $"{child.name}.prefab");
                    bool prefabExists = File.Exists(prefabPath);
                    
                    GameObject prefabVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(child, prefabPath, InteractionMode.AutomatedAction);
                    
                    if (prefabVariant != null)
                    {
                        Debug.Log($"<color=cyan>[Figma v2.1]</color> Prefab {(prefabExists ? "Updated" : "Created")}: {prefabPath}");
                    }
                }
            }
        }

        public async System.Threading.Tasks.Task ReskinAsync(Transform target, string newNodeId)
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[Figma Reskin] Access Token and File ID are required for Reskin!");
                return;
            }

            Debug.Log($"[Figma Reskin] Fetching data for node: {newNodeId}");
            var apiClient = new FigmaAPIClient(_accessToken);
            string json = await apiClient.GetFileNodesAsync(_fileId, new List<string> { newNodeId });

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[Figma Reskin] Could not fetch data for node: {newNodeId}");
                return;
            }

            var response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
            var nodeData = response?.nodes?.Values.FirstOrDefault()?.document;

            if (nodeData == null)
            {
                Debug.LogError($"[Figma Reskin] Could not find node with ID: {newNodeId} in response");
                return;
            }

            _handlerContext = new FigmaHandlerContext { Settings = Settings };
            var reskinHandler = new ReskinHandler(_handlerContext);
            
            reskinHandler.ApplyReskin(target.gameObject, nodeData);

            if (_handlerContext.ImageNodesToDownload.Count > 0)
            {
                await SyncImagesAsync(target.name, null);
            }

            Debug.Log($"<color=cyan>[Figma Reskin] Reskin completed for {target.name}!</color>");
        }

        public async System.Threading.Tasks.Task RunFontAudit(string nodeId = "")
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[FigmaParser] Cannot run Font Audit without Token and File ID.");
                return;
            }

            var apiClient = new FigmaAPIClient(_accessToken);
            UnityEditor.EditorUtility.DisplayProgressBar("Figma API", "Fetching fonts from Figma...", 0.3f);
            try
            {
                bool onlySelectedNode = !string.IsNullOrEmpty(nodeId) && (Settings == null || !Settings.AuditEntireFile);
                var json = await apiClient.GetFileAsync(_fileId, onlySelectedNode ? nodeId : "");
                var response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
                RunFontAudit(response);
            }
            finally
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }

        private void RunFontAudit(FigmaFileResponse response)
        {
            var figmaFonts = new HashSet<(string family, string postScript, int weight)>();
            if (response.nodes != null)
            {
                foreach (var container in response.nodes.Values)
                {
                    CollectFontsRecursive(container.document, figmaFonts);
                }
            }
            else if (response.document != null)
            {
                CollectFontsRecursive(response.document, figmaFonts);
            }

            if (figmaFonts.Count == 0) return;

            var mapped = new List<string>();
            var partiallyMapped = new List<string>(); // Family exists, weight does not
            var missing = new List<string>();

            foreach (var f in figmaFonts)
            {
                bool exactMatch = false;
                bool familyMatch = false;

                if (FontMapTable != null)
                {
                    string normalizedSearchFamily = (f.family ?? "").Replace(" ", "").ToLower();
                    
                    exactMatch = FontMapTable.Mappings.Any(m => 
                        m.fontPostScriptName == f.postScript || 
                        ((m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == normalizedSearchFamily && (m.figmaFontWeight == 0 || m.figmaFontWeight == f.weight)));
                    
                    familyMatch = FontMapTable.Mappings.Any(m => 
                        (m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == normalizedSearchFamily);
                }

                string desc = $"'{f.family}' ({f.weight})";
                if (!string.IsNullOrEmpty(f.postScript)) desc += $" [{f.postScript}]";

                if (exactMatch) mapped.Add(desc);
                else if (familyMatch) partiallyMapped.Add(desc);
                else missing.Add(desc);
            }

            Debug.Log($"<b>[Typography Audit]</b> Found unique fonts in Figma: {figmaFonts.Count}");
            if (mapped.Count > 0) Debug.Log($"<color=green>✅ Font mapping configured ({mapped.Count}):</color> {string.Join(", ", mapped)}");
            if (partiallyMapped.Count > 0) Debug.Log($"<color=orange>⚠️ PARTIAL MAPPING (family found, weight missing): ({partiallyMapped.Count}):</color> {string.Join(", ", partiallyMapped)}");
            if (missing.Count > 0) Debug.LogError($"<color=red>❌ MISSING FONT MAPPING ({missing.Count}):</color> {string.Join(", ", missing)}");
        }

        private void CollectFontsRecursive(FigmaNode node, HashSet<(string, string, int)> fonts)
        {
            if (node.type == "TEXT" && node.style != null)
            {
                fonts.Add((node.style.fontFamily, node.style.fontPostScriptName, node.style.fontWeight));
            }
            if (node.children != null)
            {
                foreach (var child in node.children) CollectFontsRecursive(child, fonts);
            }
        }

        private void SyncRecursive(FigmaNode node, Transform parent, string currentPath)
        {
            if (node == null) return;
            string nodePath = $"{currentPath}/{node.name}";

            // STAGE 1: PREFLIGHT
            if (!GeometryValidator.ValidatePreflight(node, nodePath, _auditReport))
            {
                Debug.LogWarning($"<color=orange>[Figma v2.1] Branch skipped (Preflight):</color> {nodePath}.");
                return;
            }

            FigmaElement element = GetOrCreateElement(node, parent);
            _processedIds.Add(node.id);
            
            RectTransform rt = element.GetComponent<RectTransform>();

            // STAGE 2: AFTER PARENTING
            if (!GeometryValidator.ValidateAfterParenting(rt, nodePath, _auditReport))
            {
                Debug.LogWarning($"<color=orange>[Figma v2.1] Branch skipped (AfterParenting):</color> {nodePath}.");
                return;
            }

            // MAP LAYOUT & VISUALS
            node.computedHash = node.ComputeHash();
            bool isText = string.Equals(node.type, "TEXT", StringComparison.OrdinalIgnoreCase);
            
            // Combine text hash with font mapping table hash
            string finalHash = node.computedHash;
            if (isText && !string.IsNullOrEmpty(_fontMappingHash))
            {
                finalHash += "_" + _fontMappingHash;
            }
            
            bool isNew = string.IsNullOrEmpty(element.LastUpdateHash);
            
            if (ForceUpdate || element.LastUpdateHash != finalHash)
            {
                if (isText && !ForceUpdate) Debug.Log($"[FigmaImporter] Updating text: {node.name} (HashMismatch)");
                
                // DIAGNOSTIC: Log root node positions
                if (parent == rt.GetComponentInParent<Canvas>().transform || parent.name.Contains("Canvas"))
                {
                    Debug.Log($"[Diagnostic] Root Node: {node.name} at Figma({node.absoluteBoundingBox.x}, {node.absoluteBoundingBox.y})");
                }

                UpdateElement(element, node, finalHash);
                
                if (isNew) CreatedCount++;
                else UpdatedCount++;
            }
            else
            {
                SkippedCount++;
            }

            // STAGE 3: AFTER LAYOUT
            if (!GeometryValidator.ValidateAfterLayout(rt, nodePath, _auditReport))
            {
                Debug.LogWarning($"<color=orange>[Figma v2.1] Branch skipped (AfterLayout):</color> {nodePath}.");
                return;
            }

            // STAGE 4: GRAPHIC LAYER
            GeometryValidator.ValidateGraphic(element.gameObject, nodePath, _auditReport);

            // Register for deferred mask processing
            if (node.clipsContent)
            {
                _deferredMasks.Add((node, element));
            }

            // REQUIREMENT: If the node is marked as an image (icon/group/vector),
            // we do not recurse into its children to avoid duplicate rendering.
            bool isHandledAsImage = _handlerContext.ImageNodesToDownload.Any(n => n.id == node.id);
            
            string uType = node.type.ToUpper();
            bool isTerminalVector = uType == "VECTOR" || uType == "BOOLEAN_OPERATION" || 
                                   uType == "STAR" || uType == "REGULAR_POLYGON" || 
                                   uType == "POLYGON" || uType == "ELLIPSE";

            // If it's a vector (even if not downloadable) - it has no useful children for Unity UI.
            // If it's a container marked as icon - we downloaded it as flat sprite, children not needed.
            bool stopRecursion = isHandledAsImage || isTerminalVector;

            if (node.children != null && !stopRecursion)
            {
                foreach (var childNode in node.children)
                {
                    SyncRecursive(childNode, element.transform, nodePath);
                }
            }
            else if (stopRecursion && node.children != null && node.children.Count > 0)
            {
                // If we decided to download this as an image OR it's a vector,
                // remove old children to prevent them from hanging under the sprite.
                // NOTE: If manual components exist, we preserve them.
                for(int i = element.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = element.transform.GetChild(i);
                    if (Settings != null && Settings.preserveManualComponents && HasManualComponents(child.gameObject))
                    {
                        Debug.Log($"[FigmaImporter] Recursion stopped for {node.name}, but child {child.name} preserved due to manual components.");
                        continue;
                    }
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private void UpdateElement(FigmaElement element, FigmaNode node, string finalHash)
        {
            int imagesInQueueBefore = _handlerContext.ImageNodesToDownload.Count;
            
            // REQUIREMENT 1 (Fixing loop): Fix foreach logic
            foreach (var h in _handlers)
            {
                if (h.CanHandle(node))
                {
                    h.Apply(node, element, _handlerContext);
                }
            }
            
            if (_handlerContext.ImageNodesToDownload.Count > imagesInQueueBefore)
            {
                element.LastUpdateHash = ""; 
            }
            else
            {
                element.LastUpdateHash = finalHash;
            }
        }

        private void ApplyDeferredMasks()
        {
            foreach (var item in _deferredMasks)
            {
                if (item.element == null) continue;
                
                bool needsMask = GeometryValidator.ShouldApplyMask(item.node, out bool useAlphaMask);
                var rectMask = item.element.GetComponent<RectMask2D>();
                var alphaMask = item.element.GetComponent<Mask>();
                
                if (needsMask)
                {
                    if (useAlphaMask)
                    {
                        // Kill RectMask if existed, set standard Mask
                        if (rectMask != null) Undo.DestroyObjectImmediate(rectMask);
                        if (alphaMask == null) item.element.gameObject.AddComponent<Mask>();
                        
                        // Mask requires Image component to function
                        if (item.element.GetComponent<Image>() == null) item.element.gameObject.AddComponent<Image>();
                    }
                    else
                    {
                        // Kill Mask if existed, set RectMask2D
                        if (alphaMask != null) Undo.DestroyObjectImmediate(alphaMask);
                        if (rectMask == null) item.element.gameObject.AddComponent<RectMask2D>();
                    }
                }
                else
                {
                    if (rectMask != null) Undo.DestroyObjectImmediate(rectMask);
                    if (alphaMask != null) Undo.DestroyObjectImmediate(alphaMask);
                }
            }
        }

        private FigmaElement GetOrCreateElement(FigmaNode node, Transform parent)
        {
            if (_existingCache.TryGetValue(node.id, out var existing))
            {
                if (existing.gameObject.name.StartsWith(FigmaElement.DeletedPrefix))
                {
                    existing.gameObject.name = existing.gameObject.name.Replace(FigmaElement.DeletedPrefix, "");
                    existing.gameObject.SetActive(true);
                }
                
                // POLICY 1: NON-DESTRUCTIVE NAMES
                bool shouldRename = Settings == null || !Settings.preserveUnityNames;
                if (shouldRename && existing.gameObject.name != node.name)
                {
                    existing.gameObject.name = node.name;
                }

                if (existing.transform.parent != parent) existing.transform.SetParent(parent, false);
                return existing;
            }

            GameObject go = new GameObject(node.name, typeof(RectTransform));
            go.layer = 5; // Force UI Layer
            go.transform.SetParent(parent, false);

            FigmaElement element = go.AddComponent<FigmaElement>();
            element.FigmaNodeId = node.id;
            element.NodeType = node.type;
            _existingCache[node.id] = element;
            
            return element;
        }

        private async System.Threading.Tasks.Task SyncImagesAsync(string screenName, Action<int, int, string> onProgress)
        {
            var apiClient = new FigmaAPIClient(_accessToken);
            var allNodes = _handlerContext.ImageNodesToDownload;
            int total = allNodes.Count;
            int current = 0;

            async System.Threading.Tasks.Task ProcessBatch(List<FigmaNode> batchNodes, float scale, string format)
            {
                if (batchNodes.Count == 0) return;

                // SMART CACHING: Check what's already on disk
                var nodesToDownload = new List<FigmaNode>();
                foreach (var node in batchNodes)
                {
                    string cleanName = string.Join("_", node.name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                    if (cleanName.Length > 50) cleanName = cleanName.Substring(0, 50);
                    
                    string baseSpritesPath = Settings != null ? Settings.baseSpritesPath : "FigmaImporter/Sprites";
                    string targetRelPath = $"{baseSpritesPath}/{screenName}/{cleanName}_{node.id.Replace(":", "_")}.{format}";
                    string fullPath = Path.Combine(Application.dataPath, targetRelPath);

                    if (File.Exists(fullPath))
                    {
                        // File already exists, simply loading from project
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{targetRelPath}");
                        if (sprite != null && _existingCache.TryGetValue(node.id, out var element) && element != null)
                        {
                            AssignSprite(element, sprite, node);
                            current++;
                        }
                    }
                    else if (DownloadImages)
                    {
                        if (!string.IsNullOrEmpty(_accessToken) && !string.IsNullOrEmpty(_fileId))
                        {
                            nodesToDownload.Add(node);
                        }
                        else
                        {
                            // If no token and no file, log as warning
                            Debug.LogWarning($"<color=orange>[Figma Cache]</color> File not found in cache and no token available for download: {node.name} ({node.id})");
                        }
                    }
                }

                if (nodesToDownload.Count == 0) return;

                int batchSize = 100; // Increased from 10 to 100 to reduce request count
                for (int i = 0; i < nodesToDownload.Count; i += batchSize)
                {
                    // Add a small delay between batches to respect Figma API Rate Limits
                    if (i > 0) await System.Threading.Tasks.Task.Delay(300);

                    var batch = nodesToDownload.Skip(i).Take(batchSize).ToList();
                    Debug.Log($"<color=cyan>[Figma v2.1]</color> Requesting image links for batch {i/batchSize + 1} ({batch.Count} nodes)...");
                    
                    var links = await apiClient.GetImageLinksAsync(_fileId, batch.Select(n => n.id).ToList(), scale, format);
                    if (links == null) continue;

                    foreach (var link in links)
                    {
                        var node = batch.FirstOrDefault(n => n.id == link.Key);
                        if (node == null) continue;

                        current++;
                        onProgress?.Invoke(current, total, $"[DOWNLOADING {format.ToUpper()}] {node.name}");

                        string cleanName = string.Join("_", node.name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                        if (cleanName.Length > 50) cleanName = cleanName.Substring(0, 50);
                        
                        string baseSpritesPath = Settings != null ? Settings.baseSpritesPath : "FigmaImporter/Sprites";
                        string targetPath = $"{baseSpritesPath}/{screenName}/{cleanName}_{node.id.Replace(":", "_")}.{format}";
                        
                        var data = await FigmaAssetDownloader.DownloadImageDataAsync(link.Value);
                        if (data == null) continue;

                        var sprite = FigmaAssetDownloader.ImportDataAsSprite(data, targetPath);
                        if (sprite != null && _existingCache.TryGetValue(node.id, out var element) && element != null)
                        {
                            AssignSprite(element, sprite, node);
                        }
                    }
                }
            }

            // Helper method for sprite assignment
            void AssignSprite(FigmaElement element, Sprite sprite, FigmaNode node)
            {
                var img = element.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = sprite;
                    img.color = new Color(1, 1, 1, node.opacity);
                    element.LastUpdateHash = node.computedHash;
                }
            }

            // Requirement: All elements (including vectors) pull as PNG 2x
            await ProcessBatch(allNodes, 2f, "png");
        }

        private void HandleDeletedElements()
        {
            foreach (var kvp in _existingCache)
            {
                if (kvp.Value != null && !_processedIds.Contains(kvp.Key)) 
                {
                    // If manual components exist, preserve the object and mark it.
                    // Otherwise, just mark as deleted (soft delete).
                    kvp.Value.MarkAsDeleted();
                }
            }
        }

        private bool HasManualComponents(GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue;
                System.Type t = c.GetType();
                
                // Standard components that we ignore (considered "system" for import)
                if (t == typeof(RectTransform) || 
                    t == typeof(CanvasRenderer) || 
                    t == typeof(FigmaElement) ||
                    t == typeof(UnityEngine.UI.Image) ||
                    t == typeof(UnityEngine.UI.RawImage) ||
                    t == typeof(TMPro.TextMeshProUGUI) ||
                    t == typeof(UnityEngine.UI.Button) ||
                    t == typeof(UnityEngine.UI.ScrollRect) ||
                    t == typeof(UnityEngine.UI.Toggle) ||
                    t == typeof(TMPro.TMP_InputField))
                {
                    continue;
                }

                return true; // Custom or third-party component found
            }
            return false;
        }
    }
}
