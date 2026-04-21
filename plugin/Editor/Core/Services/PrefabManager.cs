using System.IO;
using UnityEngine;
using UnityEditor;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Runtime;
using FigmaImporter.V2;

namespace FigmaImporter.V2.Core.Services
{
    public class PrefabManager
    {
        private readonly FigmaImporterSettings _settings;

        public PrefabManager(FigmaImporterSettings settings)
        {
            _settings = settings;
        }

        public void UpdateOrCreatePrefab(GameObject go, string customName = null) 
        {
            if (_settings == null || string.IsNullOrEmpty(_settings.basePrefabsPath)) return;

            string folderPath = Path.Combine("Assets", _settings.basePrefabsPath);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fileName = !string.IsNullOrEmpty(customName) ? customName : go.name;
            string prefabPath = Path.Combine(folderPath, fileName + ".prefab").Replace("\\", "/");
            
            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction, out success);
            
            if (success) FigmaLog.Info($"<color=green>[Figma v2.5.4] Prefab saved and connected: {prefabPath}</color>");
            else FigmaLog.Error($"[Figma v2.5.4] Failed to save prefab at {prefabPath}");
        }
    }
}
