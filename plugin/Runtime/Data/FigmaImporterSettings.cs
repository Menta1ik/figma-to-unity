using UnityEngine;

namespace FigmaImporter.V2.Data
{
    [CreateAssetMenu(fileName = "FigmaImporterSettings", menuName = "Figma Importer/Settings", order = 1)]
    public class FigmaImporterSettings : ScriptableObject
    {
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
    }
}
