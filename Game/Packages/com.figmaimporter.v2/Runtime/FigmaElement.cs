using UnityEngine;

namespace FigmaImporter.V2
{
    /// <summary>
    /// Passport component to link a Unity GameObject with a unique Figma node.
    /// Enables non-destructive updates (Smart Sync).
    /// </summary>
    [DisallowMultipleComponent]
    public class FigmaElement : MonoBehaviour
    {
        public const string DeletedPrefix = "[DELETED] ";

        [Header("Figma Identity")]
        [Tooltip("Unique ID of the node from Figma API (node_id).")]
        [SerializeField] private string _figmaNodeId;
        
        [Tooltip("Type of the node in Figma (FRAME, TEXT, RECTANGLE, etc.).")]
        [SerializeField] private string _nodeType;
        
        [Header("Sync Meta")]
        [Tooltip("Hash of parameters from the last update for fast change detection.")]
        [SerializeField] private string _lastUpdateHash;

        [HideInInspector] public Rect AbsoluteBox; // Absolute coordinates from Figma

        // Public properties for Editor script access
        public string FigmaNodeId 
        { 
            get => _figmaNodeId; 
            set => _figmaNodeId = value; 
        }

        public string NodeType 
        { 
            get => _nodeType; 
            set => _nodeType = value; 
        }

        public string LastUpdateHash 
        { 
            get => _lastUpdateHash; 
            set => _lastUpdateHash = value; 
        }

        /// <summary>
        /// Marks the object as deleted in Figma but preserved in Unity.
        /// </summary>
        public void MarkAsDeleted()
        {
            if (!gameObject.name.StartsWith(DeletedPrefix))
            {
                gameObject.name = DeletedPrefix + gameObject.name;
                gameObject.SetActive(false);
            }
        }
    }
}
