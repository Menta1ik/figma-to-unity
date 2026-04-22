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
            
            // If the object itself is a prefab instance, unpack it
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                if (root != null)
                {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }

            // Also search for any nested prefab instances within the hierarchy and unpack them
            var instances = go.GetComponentsInChildren<Transform>(true)
                .Where(t => PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                .Select(t => PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject))
                .Distinct()
                .ToList();

            foreach (var inst in instances)
            {
                if (inst != null)
                {
                    PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
        }
    }
}
