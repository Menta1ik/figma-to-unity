using System;
using UnityEngine;

namespace FigmaImporter.V2.Runtime
{
    /// <summary>
    /// Component added to GameObjects that were not found in the latest Figma sync.
    /// These objects are deactivated instead of destroyed for safety.
    /// </summary>
    [AddComponentMenu("Figma Importer/Figma Orphaned Element")]
    [DisallowMultipleComponent]
    public class FigmaOrphanedElement : MonoBehaviour
    {
        [Header("Orphan Metadata")]
        [Tooltip("The Figma Node ID this object was originally linked to.")]
        public string originalFigmaNodeId;
        
        [Tooltip("The timestamp when this element was marked as orphaned.")]
        public string orphanedAt;

        [Space]
        [TextArea(3, 5)]
        public string info = "This element was not found in the latest Figma sync. " +
                           "It has been deactivated to prevent data loss. " +
                           "You can safely delete it manually if it is no longer needed.";

        public void Initialize(string nodeId)
        {
            originalFigmaNodeId = nodeId;
            orphanedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (!gameObject.name.StartsWith("[Orphan]"))
                gameObject.name = "[Orphan] " + gameObject.name;
        }
    }
}
