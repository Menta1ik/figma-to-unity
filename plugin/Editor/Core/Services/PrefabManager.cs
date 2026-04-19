using System.IO;
using UnityEngine;
using UnityEditor;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Services
{
    public class PrefabManager
    {
        private readonly FigmaImporterSettings _settings;

        public PrefabManager(FigmaImporterSettings settings)
        {
            _settings = settings;
        }

        public void UpdateOrCreatePrefab(GameObject go) 
        {
            if (_settings == null || string.IsNullOrEmpty(_settings.basePrefabsPath)) return;

            string folderPath = Path.Combine("Assets", _settings.basePrefabsPath);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string prefabPath = Path.Combine(folderPath, go.name + ".prefab").Replace("\\", "/");
            
            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction, out success);
            
            if (success) Debug.Log($"<color=green>[FigmaImporter] Prefab saved and connected: {prefabPath}</color>");
            else Debug.LogError($"[FigmaImporter] Failed to save prefab at {prefabPath}");
        }
    }
}
