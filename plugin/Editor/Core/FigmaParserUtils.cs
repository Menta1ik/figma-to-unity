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
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root != null)
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }
    }
}
