using UnityEngine;
using UnityEditor;
using System.IO;
using FigmaImporter.V2.Core;
using FigmaImporter.V2.Data;
using UnityEngine.UI;

namespace FigmaImporter.V2.Tests
{
    public static class FigmaImportTestRunner
    {
        [MenuItem("Figma Importer/Tests/Run Interactive Test")]
        public static async void RunTest()
        {
            Debug.Log("[Test] Starting Interactive Test...");

            // 1. Create Settings
            string settingsPath = "Assets/Test/TestSettings.asset";
            FigmaImporterSettings settings = AssetDatabase.LoadAssetAtPath<FigmaImporterSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FigmaImporterSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                Debug.Log($"[Test] Created TestSettings at {settingsPath}");
            }

            // 2. Create Canvas
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas");
                canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }
            
            Transform root = new GameObject("TestRoot").transform;
            root.SetParent(canvasGo.transform, false);

            // 3. Load JSON
            string jsonPath = Path.Combine(Application.dataPath, "test_interactive.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[Test] JSON not found at {jsonPath}");
                return;
            }
            string jsonContent = File.ReadAllText(jsonPath);

            // 4. Start Parser
            FigmaParser parser = new FigmaParser("test_token", "test_file_id")
            {
                Settings = settings,
                DownloadImages = false // Disable image downloading in tests
            };

            try
            {
                await parser.ProcessFileAsync(jsonContent, root, (curr, total, name) => {
                    // Progress log
                });
                Debug.Log("<color=green>[Test] Import Finished!</color>");
                
                // Validation
                ValidateResults(root);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void ValidateResults(Transform root)
        {
            var btnGo = root.Find("Test Frame/[Btn] Submit");
            if (btnGo != null)
            {
                var btn = btnGo.GetComponent<Button>();
                var image = btnGo.GetComponent<Image>();
                Debug.Log($"[Test] Button Validated: Found={btn != null}, RaycastTarget={image?.raycastTarget}");
            }
            else Debug.LogError("[Test] Button not found!");

            var inputGo = root.Find("Test Frame/[Input] Name");
            if (inputGo != null)
            {
                var input = inputGo.GetComponent<TMPro.TMP_InputField>();
                Debug.Log($"[Test] Input Validated: Found={input != null}");
            }
            else Debug.LogError("[Test] Input not found!");
        }
    }
}
