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

namespace FigmaImporter.V2.Core
{
    public class FigmaParser
    {
        private TransformAuditReport _auditReport;
        private List<(FigmaNode node, FigmaElement element)> _deferredMasks;
        private Dictionary<string, FigmaElement> _existingCache;
        private Dictionary<string, FigmaElement> _sessionCache; // Cache for current sync session
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
            _sessionCache = new Dictionary<string, FigmaElement>();
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

            if (_handlerContext.GlobalFont == null)
            {
                EditorUtility.DisplayDialog("Figma Import Error", "Global Fallback Font is not set! Please configure it in your Font Mapping Table or Settings.", "OK");
                return;
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

            // Initialize Root AbsoluteBox for position calculation
            var rootElement = rootCanvas.GetComponent<FigmaElement>();
            if (rootElement == null) rootElement = rootCanvas.gameObject.AddComponent<FigmaElement>();
            
            try 
            {
                List<FigmaNode> topNodes = new List<FigmaNode>();
                if (response.nodes != null)
                {
                    Debug.Log($"[Figma Debug] Response contains 'nodes' dictionary with {response.nodes.Count} entries.");
                    foreach (var container in response.nodes.Values) topNodes.Add(container.document);
                }
                else if (response.document != null)
                {
                    Debug.Log("[Figma Debug] Response contains 'document' root.");
                    topNodes.Add(response.document);
                }

                if (topNodes.Count == 0)
                {
                    Debug.LogWarning("[Figma Debug] No top-level nodes found to sync!");
                    return;
                }

                if (topNodes[0].absoluteBoundingBox != null)
                {
                    var bbox = topNodes[0].absoluteBoundingBox;
                    rootElement.AbsoluteBox = new Rect(bbox.x, bbox.y, bbox.width, bbox.height);
                    Debug.Log($"[Figma Debug] Root AbsoluteBox set to: {rootElement.AbsoluteBox}");
                }

                int total = topNodes.Sum(n => CountNodes(n));
                Debug.Log($"[Figma Debug] Starting sync for {total} nodes...");
                int current = 0;

                foreach (var node in topNodes)
                {
                    SyncRecursive(node, rootCanvas, rootCanvas.name, ref current, total, onProgress, ct);
                }

                Debug.Log($"[Figma Debug] Sync completed. Created: {CreatedCount}, Updated: {UpdatedCount}. Objects in scene: {rootCanvas.childCount}");
                
                // Apply deferred masks after full tree is built
                ApplyDeferredMasks();
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
            
            _sessionCache = new Dictionary<string, FigmaElement>();
            var allElements = target.GetComponentsInChildren<FigmaElement>(true);
            foreach (var e in allElements) if (!string.IsNullOrEmpty(e.FigmaNodeId)) _sessionCache[e.FigmaNodeId] = e;

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
                
                // CRITICAL: Ensure RectTransform for UI, but skip if it's a prefab instance with Transform
                if (go.GetComponent<RectTransform>() == null)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(go))
                    {
                        Debug.Log($"[FigmaImporter] Unpacking prefab instance '{go.name}' to add UI components.");
                        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                    }
                    go.AddComponent<RectTransform>();
                }
                
                element.FigmaNodeId = node.id;
                CreatedCount++;
            }

            if (element == null) return;

            _processedIds.Add(node.id);
            _sessionCache[node.id] = element; // Populate session cache
            
            bool shouldUpdateName = true;
            if (Settings != null && Settings.preserveUnityNames && !string.IsNullOrEmpty(element.name) && element.name != "GameObject" && element.name != "New Game Object")
            {
                shouldUpdateName = false;
            }
            
            if (shouldUpdateName) element.name = node.name;

            foreach (var handler in _handlers)
            {
                try 
                {
                    if (handler != null && handler.CanHandle(node))
                        handler.Apply(node, element, _handlerContext);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FigmaImporter] Error in handler {handler.GetType().Name} for node {node.name}: {e.Message}");
                }
            }

            // Collect mask candidates for deferred application
            if (node.isMask || node.clipsContent)
            {
                _deferredMasks.Add((node, element));
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

            // v2.2.1: Parallel download with throttling
            var semaphore = new SemaphoreSlim(10);
            int completed = 0;
            int totalDownloads = _handlerContext.ImageNodesToDownload.Count(n => links.ContainsKey(n.id));

            // Phase 1: Download all image data in parallel
            var downloadTasks = new List<Task<(FigmaNode node, byte[] data)>>();
            foreach (var node in _handlerContext.ImageNodesToDownload)
            {
                if (!links.ContainsKey(node.id)) continue;
                string url = links[node.id];
                
                var task = DownloadWithThrottle(semaphore, node, url, ct);
                downloadTasks.Add(task);
            }

            var results = await Task.WhenAll(downloadTasks);

            // Phase 2: Import sprites on main thread (Unity API requirement)
            foreach (var (node, data) in results)
            {
                if (data == null) continue;
                completed++;
                onProgress?.Invoke(completed, totalDownloads, $"Importing image {completed}/{totalDownloads}");
                
                string fileName = $"{prefix}_{node.name}_{node.id.Replace(":", "_")}.png";
                string relativePath = Path.Combine(spriteFolder, fileName);
                Sprite sprite = FigmaAssetDownloader.ImportDataAsSprite(data, relativePath);
                
                if (sprite != null && _sessionCache.ContainsKey(node.id))
                {
                    var img = _sessionCache[node.id].GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = sprite;
                        img.color = Color.white;
                        
                        // 9-Slice auto-detection: if layer name ends with "_9slice"
                        if (node.name.EndsWith("_9slice") || node.name.EndsWith("_9Slice"))
                        {
                            Apply9SliceBorder(sprite, relativePath);
                            img.type = Image.Type.Sliced;
                            Debug.Log($"[FigmaImporter] Applied 9-Slice to '{node.name}'");
                        }
                    }
                }
            }

            Debug.Log($"<color=cyan>[FigmaImporter] Downloaded {completed}/{totalDownloads} images in parallel.</color>");
        }

        private static async Task<(FigmaNode node, byte[] data)> DownloadWithThrottle(
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

        /// <summary>
        /// Programmatically sets sprite border for 9-slice. Uses 25% of the shorter dimension as inset.
        /// </summary>
        private void Apply9SliceBorder(Sprite sprite, string assetRelativePath)
        {
            string assetPath = "Assets/" + assetRelativePath;
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            // Calculate border as 25% of the shorter side
            float w = sprite.texture.width;
            float h = sprite.texture.height;
            float inset = Mathf.Min(w, h) * 0.25f;
            inset = Mathf.Max(inset, 4f); // minimum 4px border

            var spriteSheet = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSheet);
            spriteSheet.spriteBorder = new Vector4(inset, inset, inset, inset);
            importer.SetTextureSettings(spriteSheet);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private void HandleDeletedElements() 
        {
            if (_existingCache == null) return;
            foreach (var kvp in _existingCache)
            {
                if (!_processedIds.Contains(kvp.Key) && kvp.Value != null)
                {
                    GameObject go = kvp.Value.gameObject;
                    
                    // SAFE DELETE MODE (v2.2.1)
                    go.SetActive(false);
                    var orphan = go.GetComponent<FigmaOrphanedElement>();
                    if (orphan == null) orphan = go.AddComponent<FigmaOrphanedElement>();
                    orphan.Initialize(kvp.Key);
                    
                    Debug.Log($"[FigmaImporter] Element '{go.name}' (ID: {kvp.Key}) marked as Orphaned instead of deleted.");
                }
            }
        }

        private void UpdateOrCreatePrefab(GameObject go) 
        {
            if (Settings == null || string.IsNullOrEmpty(Settings.basePrefabsPath)) return;

            string folderPath = Path.Combine("Assets", Settings.basePrefabsPath);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string prefabPath = Path.Combine(folderPath, go.name + ".prefab").Replace("\\", "/");
            
            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction, out success);
            
            if (success) Debug.Log($"<color=green>[FigmaImporter] Prefab saved and connected: {prefabPath}</color>");
            else Debug.LogError($"[FigmaImporter] Failed to save prefab at {prefabPath}");
        }

        /// <summary>
        /// Updates visual properties (sprites, colors, text) on an existing hierarchy
        /// without rebuilding the Transform tree. Used for variant switching.
        /// </summary>
        private void ReskinRecursive(FigmaNode node, Transform target)
        {
            if (node == null || target == null) return;

            var element = target.GetComponent<FigmaElement>();
            if (element == null) return;

            // Update text content
            if (node.type == "TEXT" && !string.IsNullOrEmpty(node.characters))
            {
                var tmp = target.GetComponent<TMPro.TMP_Text>();
                if (tmp != null) tmp.text = node.characters;
                
                // Update text color from fills
                if (node.fills != null && node.fills.Count > 0 && node.fills[0].color != null)
                {
                    if (tmp != null) tmp.color = node.fills[0].color.ToUnityColor(node.fills[0].opacity);
                }
            }

            // Update image color tint
            var img = target.GetComponent<Image>();
            if (img != null && node.fills != null && node.fills.Count > 0)
            {
                var fill = node.fills[0];
                if (fill.type == "SOLID" && fill.color != null)
                {
                    img.color = fill.color.ToUnityColor(fill.opacity);
                }
            }

            // Recurse into children by matching FigmaNodeId
            if (node.children != null)
            {
                foreach (var childNode in node.children)
                {
                    // Find matching child by Figma ID
                    Transform matchedChild = null;
                    foreach (Transform t in target)
                    {
                        var childElement = t.GetComponent<FigmaElement>();
                        if (childElement != null && childElement.FigmaNodeId == childNode.id)
                        {
                            matchedChild = t;
                            break;
                        }
                    }
                    if (matchedChild != null) ReskinRecursive(childNode, matchedChild);
                }
            }
        }

        /// <summary>
        /// Applies mask components to elements that were collected during sync.
        /// Uses clipsContent for RectMask2D and isMask for legacy Mask component.
        /// Must be called AFTER the full tree is built.
        /// </summary>
        private void ApplyDeferredMasks()
        {
            if (_deferredMasks == null || _deferredMasks.Count == 0) return;

            int appliedCount = 0;
            foreach (var (node, element) in _deferredMasks)
            {
                if (element == null) continue;
                var go = element.gameObject;

                // isMask: This node is a Figma mask shape — apply Unity Mask to its PARENT
                if (node.isMask)
                {
                    var parentTransform = go.transform.parent;
                    if (parentTransform != null)
                    {
                        var parentGo = parentTransform.gameObject;
                        if (parentGo.GetComponent<Mask>() == null && parentGo.GetComponent<RectMask2D>() == null)
                        {
                            // Add Image if missing (Mask requires Image)
                            if (parentGo.GetComponent<Image>() == null)
                                parentGo.AddComponent<Image>().color = new Color(1, 1, 1, 0);
                            
                            var mask = parentGo.AddComponent<Mask>();
                            mask.showMaskGraphic = false;
                            appliedCount++;
                        }
                    }
                }
                // clipsContent: This frame clips its own children — use RectMask2D
                else if (node.clipsContent)
                {
                    if (go.GetComponent<RectMask2D>() == null && go.GetComponent<Mask>() == null)
                    {
                        go.AddComponent<RectMask2D>();
                        appliedCount++;
                    }
                }
            }

            if (appliedCount > 0)
                Debug.Log($"<color=yellow>[FigmaImporter] Applied {appliedCount} mask(s) to the UI hierarchy.</color>");
        }
    }
}
