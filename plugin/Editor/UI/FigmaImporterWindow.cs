using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Editor.UI
{
    public class FigmaImporterWindow : EditorWindow
    {
        private string _fileId = "";
        private string _nodeId = "";
        private string _accessToken = "";
        private FigmaImporterSettings _settings;
        private FontMappingTable _fontMapping;
        private Transform _rootCanvas;
        private Vector2 _scrollPos;

        private bool _downloadImages = true;
        private bool _forceUpdate = false;
        private bool _useLocalJson = false;
        private bool _devMode = false;
        private bool _isProcessing = false;
        private CancellationTokenSource _cts;

        private GameObject _reskinTarget;
        private string _reskinNodeId = "";

        [MenuItem("Figma Importer/Sync & Reskin Dashboard")]
        public static void ShowWindow()
        {
            FigmaImporterWindow window = GetWindow<FigmaImporterWindow>("Figma v2.5.5");
            window.minSize = new Vector2(350, 450);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Figma v2.5.5");
            // Sync settings with LogLevel on load
            if (_settings != null) FigmaLog.SetLevel(_settings.logLevel);
        }

        private void OnGUI()
        {
            try 
            {
                _ = EditorStyles.boldLabel;
            }
            catch (Exception)
            {
                Repaint();
                return;
            }

            try
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                EditorGUIUtility.labelWidth = 160f;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("🚀 Antigravity Figma Importer v2.5.5", EditorStyles.boldLabel);
            
                // --- SECTION 1: CONNECTION ---
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Step 1: Connection & Config", EditorStyles.boldLabel);
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

                if (_settings != null)
                {
                    EditorGUI.BeginChangeCheck();
                    FigmaLogLevel newLogLevel = (FigmaLogLevel)EditorGUILayout.EnumPopup("Log Level", _settings.logLevel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_settings, "Change Log Level");
                        _settings.logLevel = newLogLevel;
                        EditorUtility.SetDirty(_settings);
                        FigmaLog.SetLevel(newLogLevel);
                    }
                }
                EditorGUILayout.EndVertical();

                // --- SECTION 2: RESOURCES ---
                EditorGUILayout.Space();
                if (GUILayout.Button("📖 Open Developer Manual (Documentation)"))
                {
                    FigmaLog.Info("[Figma Importer] Documentation is located in 'Packages/com.figmaimporter.v2/docs/' folder.");
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Step 2: Resources & Target", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Font Mapping links Figma fonts with TextMeshPro assets. Root Canvas is the scene object where UI will be built.", MessageType.None);
                
                _fontMapping = (FontMappingTable)EditorGUILayout.ObjectField("Font Mapping", _fontMapping, typeof(FontMappingTable), false);
                _rootCanvas = (Transform)EditorGUILayout.ObjectField("Root Canvas", _rootCanvas, typeof(Transform), true);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Font Audit")) RunFontAudit();
                if (GUILayout.Button("Clear Image Cache")) FigmaAPIClient.ClearLocalCache();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                // --- SECTION 3: SYNC ---
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Step 3: Sync & Generate", EditorStyles.boldLabel);
                
                _downloadImages = EditorGUILayout.Toggle("Download Images", _downloadImages);
                _forceUpdate = EditorGUILayout.Toggle("Force All Assets", _forceUpdate);
                _useLocalJson = EditorGUILayout.Toggle("Use Local JSON", _useLocalJson);

                GUI.backgroundColor = Color.green;
                if (_isProcessing)
                {
                    if (GUILayout.Button("CANCEL OPERATION", GUILayout.Height(40))) _cts?.Cancel();
                }
                else
                {
                    if (GUILayout.Button("RUN FULL SYNC", GUILayout.Height(40))) RunSync();
                }
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button("Clear Canvas (Root)")) ClearCanvas();
                EditorGUILayout.EndVertical();

                // --- DEV TOOLS ---
                EditorGUILayout.Space(20);
                _devMode = EditorGUILayout.Toggle("Show Dev Tools", _devMode);
                if (_devMode) DrawDevSection();

                EditorGUILayout.EndScrollView();
            }
            catch (Exception)
            {
                Repaint();
            }
        }

        private void DrawDevSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛠 Developer Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("🧪 Run Interactive Handler Test")) RunInteractiveTest();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("♻️ Reskin Area (Experimental)", EditorStyles.boldLabel);
            _reskinTarget = (GameObject)EditorGUILayout.ObjectField("Target Prefab/Obj", _reskinTarget, typeof(GameObject), true);
            _reskinNodeId = EditorGUILayout.TextField("New Figma ID", _reskinNodeId);
            if (GUILayout.Button("🎨 Perform Reskin")) RunReskinAsync();
            EditorGUILayout.EndVertical();
        }

        private async void RunSync()
        {
            if (_settings == null || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_fileId))
            {
                EditorUtility.DisplayDialog("Error", "Missing Access Token, File ID, or Settings!", "OK");
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
                    if (File.Exists(jsonPath)) jsonContent = File.ReadAllText(jsonPath);
                }
                else 
                {
                    EditorUtility.DisplayProgressBar("Figma API", "Fetching data...", 0.1f);
                    jsonContent = await new FigmaAPIClient(_accessToken).GetFileAsync(_fileId, _nodeId, _cts.Token, _settings.imageExportScale);
                }

                if (!string.IsNullOrEmpty(jsonContent))
                {
                    Undo.RegisterFullObjectHierarchyUndo(_rootCanvas.gameObject, "Figma Sync");
                    await parser.ProcessFileAsync(jsonContent, _rootCanvas, (curr, tot, name) => {
                        EditorUtility.DisplayProgressBar("Syncing", $"Node {curr}/{tot}: {name}", (float)curr/tot);
                    }, _cts.Token);
                }
            }
            catch (Exception e) { FigmaLog.Error($"[Sync Error] {e.Message}"); }
            finally
            {
                EditorUtility.ClearProgressBar();
                EndOperation();
            }
        }

        private void StartOperation() { _isProcessing = true; _cts = new CancellationTokenSource(); }
        private void EndOperation() { _isProcessing = false; _cts?.Dispose(); _cts = null; Repaint(); }

        private void ClearCanvas() { /* Logic ... */ }
        private async void RunFontAudit() { /* Logic ... */ }
        private async void RunReskinAsync() { /* Logic ... */ }
        private async void RunInteractiveTest() { /* Logic ... */ }

        private string ExtractFileId(string input)
        {
            if (string.IsNullOrEmpty(input) || !input.StartsWith("http")) return input;
            var match = Regex.Match(input, @"/(?:file|design)/([^/]+)/");
            return match.Success ? match.Groups[1].Value : input;
        }

        private string ExtractNodeId(string input)
        {
            if (string.IsNullOrEmpty(input) || !input.Contains("node-id=")) return "";
            var match = Regex.Match(input, @"node-id=([^&/]+)");
            return match.Success ? match.Groups[1].Value.Replace("-", ":") : "";
        }
    }
}
