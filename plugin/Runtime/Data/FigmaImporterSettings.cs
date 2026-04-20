using UnityEngine;

namespace FigmaImporter.V2.Data
{
    [CreateAssetMenu(fileName = "FigmaImporterSettings", menuName = "Figma Importer/Settings", order = 1)]
    public class FigmaImporterSettings : ScriptableObject
    {
        [Header("Optimization & Audit")]
        [Tooltip("If true, Font Audit will scan the entire Figma file. If false, it will only scan the selected Node ID.")]
        [SerializeField] private bool auditEntireFile = false;
        public bool AuditEntireFile => auditEntireFile;

        [Header("Asset Paths")]
        [Tooltip("Path relative to Assets/ folder where sprites will be saved.")]
        public string baseSpritesPath = "UI/Generated/Sprites";
        
        [Tooltip("Path relative to Assets/ folder where generated prefabs or scenes will be saved.")]
        public string basePrefabsPath = "UI/Generated/Prefabs";

        [Header("UI Interaction")]
        [Tooltip("If true, all elements will have Raycast Target disabled by default, except for interactive markers.")]
        public bool disableRaycastByDefault = true;

        [Header("Non-Destructive Policy")]
        [Tooltip("If true, manually changed GameObject names in Unity will not be overwritten by Figma node names.")]
        public bool preserveUnityNames = true;

        [Tooltip("If true, manually added components in Unity will not be removed.")]
        public bool preserveManualComponents = true;
        
        [Header("Interactive Markers")]
        public string buttonMarker = "[Btn]";
        public string inputMarker = "[Input]";
        public string scrollMarker = "[Scroll]";
        public string toggleMarker = "[Toggle]";

        [Header("Adaptive Layout")]
        [Tooltip("If true, translate Figma constraints to RectTransform anchors and offsets.")]
        public bool enableConstraintsTranslation = false;

        public enum CanvasScaleMode { None, ConstantPixelSize, ScaleWithScreenSize }
        [Tooltip("How the root Canvas should scale.")]
        public CanvasScaleMode canvasScaleMode = CanvasScaleMode.None;

        [Tooltip("The resolution that the UI is designed for (e.g. 1920x1080).")]
        public Vector2 referenceResolution = new Vector2(1920, 1080);

        [Range(0, 1)]
        [Tooltip("0 = Width, 1 = Height. 0.5 = Balanced.")]
        public float matchWidthOrHeight = 0.5f;
    }
}
