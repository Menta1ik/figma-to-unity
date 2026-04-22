using UnityEngine;
using UnityEditor;

namespace FigmaImporter.V2.Core
{
    /// <summary>
    /// Shared utilities used by FigmaParser and its extracted subsystems.
    /// </summary>
    internal static class FigmaParserUtils
    {
        public static void EnsureUnpacked(GameObject go)
        {
            if (go == null) return;
            
            // Check if object is part of any prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                // Get the outermost instance root to safely unpack
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (root != null)
                {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    FigmaLog.Debug($"[Figma Utils] Unpacked prefab instance for: {go.name}");
                }
            }
        }
    }
}
