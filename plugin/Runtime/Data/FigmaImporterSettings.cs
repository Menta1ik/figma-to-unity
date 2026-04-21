using UnityEngine;

namespace FigmaImporter.V2.Data
{
    [CreateAssetMenu(fileName = "FigmaImporterSettings", menuName = "Figma Importer/Settings", order = 1)]
    public class FigmaImporterSettings : ScriptableObject
    {
        [Header("Naming Conventions")]
        public string framePrefix = "Frame_";
        public string textPrefix = "Text_";
        public string imagePrefix = "Image_";
        public string vectorPrefix = "Vector_";

        [Header("Hierarchy Options")]
        public bool createPrefabsForTopFrames = false;
        public bool useCompactHierarchy = true;

        [Header("Image Options")]
        public bool exportAssetsAsSprites = true;
        [Range(0.5f, 4f)]
        public float imageExportScale = 1.0f;

        [Header("Logging")]
        [SerializeField]
        public FigmaLogLevel logLevel = FigmaLogLevel.Minimal;

        [Header("Scene Management")]
        public bool autoSaveOnSync = true;
    }
}
