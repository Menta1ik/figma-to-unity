using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Editor.Utils;

namespace FigmaImporter.V2.UI
{
    public class FigmaImporterWindow : EditorWindow
    {
        private string _fileId = "VTzGVHnsRpELqG3pYTFE3M";
        private string _nodeId = ""; // NEW: Field for specific frame/node sync
        private string _accessToken = "";
        
        private bool _useLocalJson = false;
        private bool _downloadImages = true; // Image toggle
        private bool _forceUpdate = false; // Skip hashes
        private Transform _rootCanvas;
        private FontMappingTable _fontMapping;
        private FigmaImporterSettings _settings;
        private Vector2 _scrollPos;
        private bool _devMode = false;

        private bool _isProcessing = false;
        private CancellationTokenSource _cts;

        // Reskin variables
        private GameObject _reskinTarget;
        private string _reskinNodeId = "";

        [MenuItem("Figma Importer/Sync & Reskin Dashboard")]
        public static void ShowWindow()
        {
            FigmaImporterWindow window = GetWindow<FigmaImporterWindow>("Figma v2.3.0");
            window.minSize = new Vector2(350, 450);
        }

        private void OnGUI()
        {
            if (GUI.skin == null) return;
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUIUtility.labelWidth = 160f;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🚀 Antigravity Figma Importer v2.3.0", FigmaEditorStyles.BoldLabel);
            
            // --- SECTION 1: CONNECTION ---
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Step 1: Connection & Config", FigmaEditorStyles.BoldLabel);
            EditorGUILayout.HelpBox("Provide the Figma URL or File ID. The plugin will automatically extract the Node ID. The Access Token (PAT) is from your Figma account settings.", MessageType.None);
            
            string newFileId = EditorGUILayout.TextField("Figma URL / File ID", _fileId);
            if (newFileId != _fileId)
            {
                _fileId = ExtractFileId(newFileId);
                string extractedNode = ExtractNodeId(newFileId);
                if (!string.IsNullOrEmpty(extractedNode)) _nodeId = extractedNode;
            }
            
            _nodeId = EditorGUILayout.TextField("Single Node ID", _nodeId);
            EditorGUILayout.HelpBox("Leave empty for the whole file, or provide a specific frame ID for faster single-screen sync.", MessageType.None);
            
            _accessToken = EditorGUILayout.PasswordField("Access Token (PAT)", _accessToken);
            _settings = (FigmaImporterSettings)EditorGUILayout.ObjectField("Importer Settings", _settings, typeof(FigmaImporterSettings), false);
            EditorGUILayout.EndVertical();

            // --- SECTION 2: RESOURCES ---
            EditorGUILayout.Space();
            // Help link
            if (GUILayout.Button("📖 Open Developer Manual (Documentation)"))
            {
                Debug.Log("[Figma Importer] Documentation is located in 'Packages/com.figmaimporter.v2/Manuals/' folder.");
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Step 2: Resources & Target", FigmaEditorStyles.BoldLabel);
            EditorGUILayout.HelpBox("Font Mapping links Figma fonts with TextMeshPro assets. Root Canvas is the scene object where UI will be built.", MessageType.None);
            
            _fontMapping = (FontMappingTable)EditorGUILayout.ObjectField("Font Mapping", _fontMapping, typeof(FontMappingTable), false);
            _rootCanvas = (Transform)EditorGUILayout.ObjectField("Root Canvas", _rootCanvas, typeof(Transform), true);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Font Audit")) RunFontAudit();
            if (GUILayout.Button("Clear Image Cache")) 
            {
                 Debug.Log("[FigmaImporter] Image cache logic is internal to Parser.");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // --- SECTION 3: SYNC ---
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Step 3: Sync & Generate", FigmaEditorStyles.BoldLabel);
            EditorGUILayout.HelpBox(
            "🚀 QUICK START GUIDE:\n" +
            "Step 1: Paste Figma URL and Access Token below.\n" +
            "Step 2: Assign 'Importer Settings' and 'Font Mapping' assets.\n" +
            "Step 3: DRAG your Canvas from Hierarchy into the 'Root Canvas' field.\n" +
            "Step 4: (Optional) Press 'Font Audit' to verify font assets.\n" +
            "Step 5: Press the green 'RUN FULL SYNC' button.\n" +
            "Step 6: The Prefab will be created INSTANTLY in your specified prefabs folder.",
            MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            _downloadImages = EditorGUILayout.Toggle("Sync Images", _downloadImages);
            _forceUpdate = EditorGUILayout.Toggle("Force Update", _forceUpdate);
            EditorGUILayout.EndHorizontal();

            if (_isProcessing)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("🛑 CANCEL OPERATION", GUILayout.Height(40)))
                {
                    _cts?.Cancel();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
                if (GUILayout.Button("🚀 RUN FULL SYNC", GUILayout.Height(40)))
                {
                    RunSync();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();

            // --- SECTION 4: UTILS ---
            EditorGUILayout.Space();
            if (!_isProcessing)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Utilities", FigmaEditorStyles.BoldLabel);
                if (GUILayout.Button("🧹 Clear Root Canvas"))
                {
                    if (EditorUtility.DisplayDialog("Warning", "This will delete all children of the root canvas. Continue?", "Delete", "Cancel"))
                    {
                        EditorApplication.delayCall += () => ClearCanvas();
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
                _devMode = EditorGUILayout.Toggle("Show Dev Tools", _devMode);
                if (_devMode) DrawDevSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private async void RunFontAudit()
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[FigmaImporter] Please provide Token and File ID first.");
                return;
            }

            StartOperation();
            try
            {
                FigmaParser parser = new FigmaParser(_accessToken, _fileId);
                parser.FontMapTable = _fontMapping;
                parser.Settings = _settings;
                await parser.RunFontAudit(_nodeId, _cts.Token);
            }
            catch (OperationCanceledException) { Debug.LogWarning("[FigmaImporter] Font Audit cancelled."); }
            catch (System.Exception e) { Debug.LogError($"[FigmaImporter] Audit Error: {e.Message}"); }
            finally { EndOperation(); }
        }

        private void StartOperation()
        {
            _isProcessing = true;
            _cts = new CancellationTokenSource();
        }

        private void EndOperation()
        {
            _isProcessing = false;
            _cts?.Dispose();
            _cts = null;
            Repaint();
        }

        private void DrawDevSection()
        {
            DrawHeader("🛠 Developer Tools");
            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("🧪 Run Interactive Handler Test"))
            {
                RunInteractiveTest();
            }
            EditorGUILayout.EndVertical();

            DrawHeader("♻️ Reskin Area (Experimental)");
            EditorGUILayout.BeginVertical("box");
            _reskinTarget = (GameObject)EditorGUILayout.ObjectField("Target Prefab/Obj", _reskinTarget, typeof(GameObject), true);
            _reskinNodeId = EditorGUILayout.TextField("New Figma ID", _reskinNodeId);
            if (GUILayout.Button("🎨 Perform Reskin"))
            {
                if (_reskinTarget == null || string.IsNullOrEmpty(_reskinNodeId))
                {
                    Debug.LogError("[Reskin] Please assign a Target Object and a valid Figma Node ID.");
                    return;
                }
                RunReskinAsync();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader(string text)
        {
            EditorGUILayout.Space(10);
            GUILayout.Label(text, FigmaEditorStyles.BoldLabel);
        }

        private async void RunSync()
        {
            if (_settings == null || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                EditorUtility.DisplayDialog("Error", "Please check Access Token, File ID, and Settings!", "OK");
                return;
            }

            if (_rootCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign Root Canvas!", "OK");
                return;
            }

            StartOperation();
            try 
            {
                FigmaParser parser = new FigmaParser(_accessToken, _fileId)
                {
                    FontMapTable = _fontMapping,
                    Settings = _settings,
                    DownloadImages = _downloadImages,
                    ForceUpdate = _forceUpdate
                };

                string jsonContent = "";
                
                if (_useLocalJson)
                {
                    string jsonPath = Path.Combine(Application.dataPath, "lobby_figma.json");
                    if (!File.Exists(jsonPath))
                    {
                        Debug.LogError($"[Figma v2.3.0] Local file not found: {jsonPath}");
                        return;
                    }
                    jsonContent = File.ReadAllText(jsonPath);
                }
                else 
                {
                    EditorUtility.DisplayProgressBar("Figma API", "Fetching cloud data...", 0.1f);
                    jsonContent = await new FigmaAPIClient(_accessToken).GetFileAsync(_fileId, _nodeId, _cts.Token);
                }

                if (string.IsNullOrEmpty(jsonContent)) return;

                Undo.RegisterFullObjectHierarchyUndo(_rootCanvas.gameObject, "Figma Smart Sync");
                
                await parser.ProcessFileAsync(jsonContent, _rootCanvas, (current, total, nodeName) => {
                    float progress = (float)current / total;
                    EditorUtility.DisplayProgressBar("Syncing", $"Processing node {current}/{total}: {nodeName}", progress);
                }, _cts.Token);
            }
            catch (OperationCanceledException) { Debug.LogWarning("[FigmaImporter] Sync operation cancelled."); }
            catch (System.Exception e) { Debug.LogError($"[Figma API Error] {e.Message}"); }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.UnloadUnusedAssetsImmediate();
                EndOperation();
            }
        }

        private void ClearCanvas()
        {
            if (_rootCanvas == null) return;
            Undo.RegisterFullObjectHierarchyUndo(_rootCanvas.gameObject, "Clear Canvas");
            for (int i = _rootCanvas.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(_rootCanvas.GetChild(i).gameObject);
            }
            Debug.Log("[Figma v2.3.0] Canvas cleared successfully!");
        }
        private async void RunInteractiveTest()
        {
            string jsonPath = Path.Combine(Application.dataPath, "test_interactive.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError("[Dev] test_interactive.json not found!");
                return;
            }

            if (_rootCanvas == null || _settings == null)
            {
                Debug.LogError("[Dev] Setup Root Canvas and Settings first!");
                return;
            }

            string jsonContent = File.ReadAllText(jsonPath);
            FigmaParser parser = new FigmaParser(_accessToken, _fileId)
            {
                Settings = _settings,
                DownloadImages = false
            };

            await parser.ProcessFileAsync(jsonContent, _rootCanvas);
            Debug.Log("<color=cyan>[Dev] Test Import Finished!</color>");
        }

        private async void RunReskinAsync()
        {
            if (_settings == null || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                Debug.LogError("[Reskin] Setup Token, FileID and Settings first!");
                return;
            }

            FigmaParser parser = new FigmaParser(_accessToken, _fileId)
            {
                Settings = _settings,
                FontMapTable = _fontMapping
            };

            await parser.ReskinAsync(_reskinTarget.transform, _reskinNodeId);
        }
        private string ExtractFileId(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (!input.StartsWith("http")) return input;

            var match = Regex.Match(input, @"/(?:file|design)/([^/]+)/");
            return match.Success ? match.Groups[1].Value : input;
        }

        private string ExtractNodeId(string input)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains("node-id=")) return "";
            
            var match = Regex.Match(input, @"node-id=([^&]+)");
            if (match.Success)
            {
                return match.Groups[1].Value.Replace("-", ":");
            }
            return "";
        }
    }
}
