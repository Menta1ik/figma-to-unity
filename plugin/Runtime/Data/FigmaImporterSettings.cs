using UnityEngine;

namespace FigmaImporter.V2.Data
{
    public enum FigmaLogLevel { Silent, Minimal, Verbose }

    [CreateAssetMenu(fileName = "FigmaImporterSettings", menuName = "Figma Importer/Settings", order = 1)]
    public class FigmaImporterSettings : ScriptableObject
    {
        [Header("Optimization & Audit")]
        public bool auditEntireFile = false;
        public bool AuditEntireFile => auditEntireFile;

        [Header("Logging")]
        public FigmaLogLevel logLevel = FigmaLogLevel.Minimal;

        [Header("Image Export")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _imageExportScale = 2f;
        public float ImageExportScale => _imageExportScale;

        [Header("Asset Paths")]
        public string baseSpritesPath = "UI/Generated/Sprites";
        public string basePrefabsPath = "UI/Generated/Prefabs";

        [Header("UI Interaction")]
        public bool disableRaycastByDefault = true;

        [Header("Non-Destructive Policy")]
        public bool preserveUnityNames = true;
        public bool preserveManualComponents = true;
        
        [Header("Interactive Markers")]
        public string buttonMarker = "[Btn]";
        public string inputMarker = "[Input]";
        public string scrollMarker = "[Scroll]";
        public string toggleMarker = "[Toggle]";

        [Header("Adaptive Layout")]
        public bool enableConstraintsTranslation = false;

        public enum CanvasScaleMode { None, ConstantPixelSize, ScaleWithScreenSize }
        public CanvasScaleMode canvasScaleMode = CanvasScaleMode.None;
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public float matchWidthOrHeight = 0.5f;

        [Header("Legacy Compatibility")]
        public bool useCompactHierarchy = true;
        public bool createPrefabsForTopFrames = false;
    }
}
