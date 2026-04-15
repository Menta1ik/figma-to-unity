using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

namespace FrontStrike.EditorTools
{
    /// <summary>
    /// Монолитный скрипт для прямого импорта UI из Figma в Unity через REST API.
    /// v1.1.1 - Исправлена стабильность импорта ассетов и работа с шрифтами.
    /// </summary>
    public class FigmaDirectImporter : EditorWindow
    {
        public const string PluginVersion = "1.1.1";

        // Ввод данных
        private string figmaToken = "";
        private string fileId = "";
        private string nodeId = "";

        // Состояние
        private bool isProcessing = false;
        private bool isValidationPassed = false;
        private string logOutput = "Ready.\n";
        private Vector2 scrollPos;

        // Кэшированные данные
        private JObject cachedFigmaData = null;

        // Требуемые узлы для валидации
        private string requiredNodesString = "";
        private bool strictValidation = false;
        private bool importAutoLayout = false;

        public TMP_FontAsset globalFont;

        [MenuItem("Tools/Figma/Direct Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<FigmaDirectImporter>("Figma Direct Importer");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            figmaToken = EditorPrefs.GetString("FigmaImporter_Token", "");
            fileId = EditorPrefs.GetString("FigmaImporter_FileId", "");
            nodeId = EditorPrefs.GetString("FigmaImporter_NodeId", "");
            requiredNodesString = EditorPrefs.GetString("FigmaImporter_RequiredNodes", "");
            strictValidation = EditorPrefs.GetBool("FigmaImporter_StrictValidation", false);
            importAutoLayout = EditorPrefs.GetBool("FigmaImporter_AutoLayout", false);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("FigmaImporter_Token", figmaToken);
            EditorPrefs.SetString("FigmaImporter_FileId", fileId);
            EditorPrefs.SetString("FigmaImporter_NodeId", nodeId);
            EditorPrefs.SetString("FigmaImporter_RequiredNodes", requiredNodesString);
            EditorPrefs.SetBool("FigmaImporter_StrictValidation", strictValidation);
            EditorPrefs.SetBool("FigmaImporter_AutoLayout", importAutoLayout);
        }

        private void OnGUI()
        {
            GUILayout.Label($"Figma Connection Settings (v{PluginVersion})", EditorStyles.boldLabel);

            figmaToken = EditorGUILayout.TextField("Access Token", figmaToken);
            fileId = EditorGUILayout.TextField("File ID", fileId);
            nodeId = EditorGUILayout.TextField("Node ID (Optional)", nodeId);

            EditorGUILayout.Space();
            GUILayout.Label("Validation Settings", EditorStyles.boldLabel);
            strictValidation = EditorGUILayout.Toggle("Strict Validation", strictValidation);
            requiredNodesString = EditorGUILayout.TextField("Required Nodes (comma separated):", requiredNodesString);

            EditorGUILayout.Space();
            GUILayout.Label("Layout Settings", EditorStyles.boldLabel);
            importAutoLayout = EditorGUILayout.Toggle("Import Figma Auto Layout", importAutoLayout);

            EditorGUILayout.Space();
            GUILayout.Label("Appearance Settings", EditorStyles.boldLabel);
            globalFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Global Font (TMP)", globalFont, typeof(TMP_FontAsset), false);

            EditorGUILayout.Space();

            GUI.enabled = !isProcessing;

            if (GUILayout.Button("1. ANALYZE LAYOUT", GUILayout.Height(30)))
            {
                _ = AnalyzeLayoutAsync();
            }

            GUI.enabled = isValidationPassed && !isProcessing;

            if (GUILayout.Button("2. BUILD UI", GUILayout.Height(30)))
            {
                BuildUI();
            }

            if (GUILayout.Button("3. FORCE SYNC IMAGES", GUILayout.Height(30)))
            {
                _ = SyncAssetsAsync();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();
            GUILayout.Label("Logs", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, EditorStyles.helpBox);
            EditorGUILayout.TextArea(logOutput, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private void AppendLog(string message)
        {
            logOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            Repaint();
        }

        private async Task SyncAssetsAsync()
        {
            if (string.IsNullOrEmpty(figmaToken) || string.IsNullOrEmpty(fileId)) return;
            isProcessing = true;
            AppendLog("Starting safe image sync...");

            try
            {
                EditorUtility.DisplayProgressBar("Figma Sync", "Looking for image components...", 0f);

                Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Dictionary<string, Image> imageNodes = new Dictionary<string, Image>();

                foreach (var img in allImages) {
                    string goName = img.gameObject.name;
                    if (goName.StartsWith("[") && goName.Contains("]")) {
                        int start = 1; int end = goName.IndexOf("]");
                        imageNodes[goName.Substring(start, end - start)] = img;
                    }
                }

                if (imageNodes.Count == 0) { AppendLog("No images to sync."); return; }
                
                List<string> allIds = imageNodes.Keys.ToList();
                int batchSize = 40;
                List<string> downloadedRelativePaths = new List<string>();
                string saveDir = "Assets/UI/Sprites";
                if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

                int totalIds = allIds.Count;
                int processedImages = 0;

                for (int i = 0; i < allIds.Count; i += batchSize)
                {
                    var batch = allIds.Skip(i).Take(batchSize);
                    string url = $"https://api.figma.com/v1/images/{fileId}?ids={string.Join(",", batch)}&scale=3&format=png";

                    EditorUtility.DisplayProgressBar("Figma Sync", $"Requesting URLs...", 0.1f + 0.1f * ((float)i / totalIds));

                    using (UnityWebRequest request = UnityWebRequest.Get(url)) {
                        request.SetRequestHeader("X-Figma-Token", figmaToken);
                        var op = request.SendWebRequest();
                        while (!op.isDone) await Task.Delay(10);

                        if (request.result == UnityWebRequest.Result.Success) {
                            JObject json = JObject.Parse(request.downloadHandler.text);
                            var images = json["images"];
                            if (images != null) {
                                foreach (JProperty imgProp in images) {
                                    processedImages++;
                                    string id = imgProp.Name; string imgUrl = imgProp.Value?.ToString();
                                    
                                    EditorUtility.DisplayProgressBar("Figma Sync", $"Downloading {processedImages}/{totalIds}...", 0.2f + 0.6f * ((float)processedImages / totalIds));

                                    if (!string.IsNullOrEmpty(imgUrl)) {
                                        string safeId = id.Replace(":", "_");
                                        string relativePath = $"{saveDir}/spr_{safeId}.png";
                                        string absolutePath = Path.Combine(Application.dataPath, relativePath.Replace("Assets/", ""));
                                        
                                        using (UnityWebRequest dl = UnityWebRequest.Get(imgUrl)) {
                                            var dlOp = dl.SendWebRequest();
                                            while (!dlOp.isDone) await Task.Delay(10);
                                            if (dl.result == UnityWebRequest.Result.Success) {
                                                File.WriteAllBytes(absolutePath, dl.downloadHandler.data);
                                                downloadedRelativePaths.Add(relativePath);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("Figma Sync", "Finalizing assets...", 0.9f);
                AssetDatabase.Refresh();

                // Оптимизированный импорт
                AssetDatabase.StartAssetEditing();
                try {
                    foreach (string path in downloadedRelativePaths) {
                        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer != null) {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.alphaIsTransparency = true;
                            importer.SaveAndReimport();
                        }
                    }
                } finally {
                    AssetDatabase.StopAssetEditing();
                }

                // Применение спрайтов
                foreach (string path in downloadedRelativePaths) {
                    string originalId = Path.GetFileNameWithoutExtension(path).Replace("spr_", "").Replace("_", ":");
                    if (imageNodes.TryGetValue(originalId, out Image targetImage)) {
                        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (spr != null) { targetImage.sprite = spr; targetImage.color = Color.white; }
                    }
                }
                
                AppendLog("Image sync finished successfully!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isProcessing = false;
            }
        }

        private async Task AnalyzeLayoutAsync()
        {
            if (string.IsNullOrEmpty(figmaToken) || string.IsNullOrEmpty(fileId))
            {
                AppendLog("Error: Token and File ID are required.");
                return;
            }

            isProcessing = true;
            isValidationPassed = false;
            cachedFigmaData = null;
            logOutput = "Starting analysis...\n";
            AppendLog("Connecting to Figma API...");

            string jsonResponse = await FetchFigmaDataWithBackoff();

            if (string.IsNullOrEmpty(jsonResponse))
            {
                AppendLog("Analysis failed: Empty response.");
                isProcessing = false;
                return;
            }

            try
            {
                cachedFigmaData = JObject.Parse(jsonResponse);
                ValidateData(cachedFigmaData);
            }
            catch (Exception ex)
            {
                AppendLog($"Error parsing JSON: {ex.Message}");
                isValidationPassed = false;
            }

            isProcessing = false;
            Repaint();
        }

        private async Task<string> FetchFigmaDataWithBackoff()
        {
            int maxRetries = 5;
            int delayMs = 1000;
            string url = $"https://api.figma.com/v1/files/{fileId}";
            if (!string.IsNullOrEmpty(nodeId)) url += $"?ids={nodeId}";

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("X-Figma-Token", figmaToken);
                    var operation = request.SendWebRequest();
                    while (!operation.isDone) await Task.Delay(10);

                    if (request.result == UnityWebRequest.Result.Success) return request.downloadHandler.text;
                    if (request.responseCode == 429)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                        continue;
                    }
                    return null;
                }
            }
            return null;
        }

        private void ValidateData(JObject figmaData)
        {
            var nodeCounts = new Dictionary<string, int>();
            CountNodeTypes(figmaData, nodeCounts);

            if (!strictValidation) { isValidationPassed = true; return; }

            string[] requiredNodes = string.IsNullOrWhiteSpace(requiredNodesString) 
                ? new string[0] 
                : requiredNodesString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

            foreach (string rn in requiredNodes) {
                if (!FindNodeByName(figmaData, rn)) { isValidationPassed = false; return; }
            }
            isValidationPassed = true;
        }

        private void CountNodeTypes(JToken node, Dictionary<string, int> counts)
        {
            if (node == null) return;
            string type = node["type"]?.ToString();
            if (!string.IsNullOrEmpty(type)) {
                if (counts.ContainsKey(type)) counts[type]++;
                else counts[type] = 1;
            }
            var children = node["children"];
            if (children != null) foreach (var child in children) CountNodeTypes(child, counts);
            var nodes = node["nodes"];
            if (nodes != null) foreach (JProperty child in nodes) CountNodeTypes(child.Value["document"], counts);
            var doc = node["document"];
            if (doc != null) CountNodeTypes(doc, counts);
        }

        private bool FindNodeByName(JToken node, string targetName)
        {
            if (node == null) return false;
            if (node["name"]?.ToString() == targetName) return true;
            var children = node["children"];
            if (children != null) foreach (var child in children) if (FindNodeByName(child, targetName)) return true;
            var nodes = node["nodes"];
            if (nodes != null) foreach (JProperty child in nodes) if (FindNodeByName(child.Value["document"], targetName)) return true;
            var doc = node["document"];
            if (doc != null && FindNodeByName(doc, targetName)) return true;
            return false;
        }

        private void BuildUI()
        {
            if (cachedFigmaData == null) return;
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            JToken rootNode = null;
            if (!string.IsNullOrEmpty(nodeId) && cachedFigmaData["nodes"] != null) rootNode = cachedFigmaData["nodes"]?[nodeId]?["document"];
            if (rootNode == null) rootNode = FindTargetNode(cachedFigmaData["document"] ?? cachedFigmaData, nodeId);

            if (rootNode != null) CreateUINode(rootNode, canvas.transform, GetBoundingBox(rootNode));
        }

        private JToken FindTargetNode(JToken node, string targetId)
        {
            if (node == null) return null;
            if (!string.IsNullOrEmpty(targetId) && node["id"]?.ToString() == targetId) return node;
            if (string.IsNullOrEmpty(targetId) && node["type"]?.ToString() == "FRAME") return node;
            var children = node["children"];
            if (children != null) foreach (var child in children) {
                JToken found = FindTargetNode(child, targetId);
                if (found != null) return found;
            }
            return null;
        }

        private bool HasTextChild(JToken node)
        {
            if (node == null) return false;
            if (node["type"]?.ToString() == "TEXT") return true;
            var children = node["children"];
            if (children != null) foreach (var child in children) if (HasTextChild(child)) return true;
            return false;
        }

        private void CreateUINode(JToken node, Transform parent, Rect parentGlobalBounds)
        {
            if (node == null || (node["visible"] != null && !(bool)node["visible"])) return;

            string id = node["id"]?.ToString();
            string name = node["name"]?.ToString();
            string type = node["type"]?.ToString();

            GameObject go = new GameObject($"[{id}] {name}");
            go.transform.SetParent(parent, false);
            RectTransform rectTransform = go.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            Rect globalBounds = GetBoundingBox(node);
            rectTransform.anchoredPosition = new Vector2(globalBounds.x - parentGlobalBounds.x, -(globalBounds.y - parentGlobalBounds.y));
            rectTransform.sizeDelta = new Vector2(globalBounds.width, globalBounds.height);

            bool skipChildren = false;

            if (type == "TEXT")
            {
                TextMeshProUGUI textComponent = go.AddComponent<TextMeshProUGUI>();
                textComponent.text = node["characters"]?.ToString();
                if (globalFont != null) textComponent.font = globalFont;

                var style = node["style"];
                if (style != null)
                {
                    string fw = style["fontWeight"]?.ToString();
                    if (!string.IsNullOrEmpty(fw)) textComponent.fontWeight = (fw.ToLower() == "bold" || (int.TryParse(fw, out int wn) && wn >= 700)) ? FontWeight.Bold : FontWeight.Regular;
                    if (float.TryParse(style["fontSize"]?.ToString(), out float fs)) textComponent.fontSize = fs;
                    
                    string tc = style["textCase"]?.ToString();
                    if (tc == "UPPER") textComponent.text = textComponent.text.ToUpper();
                    else if (tc == "LOWER") textComponent.text = textComponent.text.ToLower();

                    string align = style["textAlignHorizontal"]?.ToString();
                    textComponent.alignment = (align == "CENTER") ? TextAlignmentOptions.TopGeoAligned : (align == "RIGHT" ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
                    textComponent.enableWordWrapping = false;
                    textComponent.overflowMode = TextOverflowModes.Overflow;
                }
                Color? sc = GetNativeSolidColor(node);
                if (sc != null) textComponent.color = sc.Value;
                go.name = name;
            }
            else if (type == "FRAME" || type == "GROUP" || type == "COMPONENT" || type == "VECTOR" || type == "RECTANGLE" || type == "ELLIPSE" || type == "STAR" || type == "REGULAR_POLYGON" || type == "BOOLEAN_OPERATION" || type == "INSTANCE")
            {
                if (importAutoLayout && (type == "FRAME" || type == "GROUP" || type == "COMPONENT" || type == "INSTANCE"))
                {
                    string lm = node["layoutMode"]?.ToString();
                    if (lm == "HORIZONTAL" || lm == "VERTICAL")
                    {
                        HorizontalOrVerticalLayoutGroup lg = (lm == "HORIZONTAL") ? go.AddComponent<HorizontalLayoutGroup>() : go.AddComponent<VerticalLayoutGroup>();
                        lg.childForceExpandWidth = lg.childForceExpandHeight = lg.childControlWidth = lg.childControlHeight = false;
                        if (float.TryParse(node["itemSpacing"]?.ToString(), out float sp)) lg.spacing = sp;
                    }
                }

                bool isStructural = (type == "FRAME" || type == "GROUP" || type == "COMPONENT" || type == "INSTANCE");
                bool containsText = HasTextChild(node);
                Color? sc = GetNativeSolidColor(node);

                bool paintNatively = (type == "RECTANGLE" && sc != null) || (isStructural && sc != null && containsText);

                if (paintNatively) {
                    go.AddComponent<Image>().color = sc.Value;
                    go.name = name;
                } else if (type != "TEXT") {
                    if (isStructural && containsText) go.name = name;
                    else {
                        go.AddComponent<Image>().color = new Color(1, 1, 1, 0);
                        if (isStructural) skipChildren = true;
                    }
                }
            }

            if (node["clipsContent"] != null && (bool)node["clipsContent"]) go.AddComponent<RectMask2D>();
            if (node["isMask"] != null && (bool)node["isMask"]) {
                if (go.GetComponent<Image>() == null) go.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
                go.AddComponent<Mask>().showMaskGraphic = false;
            }

            var children = node["children"];
            if (children != null && !skipChildren) foreach (var child in children) CreateUINode(child, go.transform, globalBounds);
        }

        private Color? GetNativeSolidColor(JToken node)
        {
            var fills = node["fills"];
            if (fills == null || !fills.HasValues) return null;
            foreach (var fill in fills) {
                if (fill["visible"] != null && !(bool)fill["visible"]) continue;
                if (fill["type"]?.ToString() == "IMAGE") return null;
                JToken cn = (fill["type"]?.ToString() == "SOLID") ? fill["color"] : fill["gradientStops"]?[0]?["color"];
                if (cn != null) {
                    float r = cn["r"]?.Value<float>() ?? 1f, g = cn["g"]?.Value<float>() ?? 1f, b = cn["b"]?.Value<float>() ?? 1f, a = cn["a"]?.Value<float>() ?? 1f;
                    return new Color(r, g, b, a * (fill["opacity"]?.Value<float>() ?? 1f) * (node["opacity"]?.Value<float>() ?? 1f));
                }
            }
            return null;
        }

        private Rect GetBoundingBox(JToken node)
        {
            var bbox = node["absoluteBoundingBox"];
            if (bbox == null) return new Rect(0, 0, 0, 0);
            return new Rect(bbox["x"]?.Value<float>() ?? 0, bbox["y"]?.Value<float>() ?? 0, bbox["width"]?.Value<float>() ?? 0, bbox["height"]?.Value<float>() ?? 0);
        }
    }
}