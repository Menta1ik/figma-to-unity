using System.Collections.Generic;
using UnityEngine;
using FigmaImporter.V2.Runtime;

namespace FigmaImporter.V2.Core
{
    /// <summary>
    /// Marks elements that existed before sync but were not found in the current Figma response.
    /// </summary>
    internal static class FigmaOrphanManager
    {
        public static void MarkOrphans(Dictionary<string, FigmaElement> existingCache, HashSet<string> processedIds, bool isSingleNodeSync)
        {
            if (existingCache == null || isSingleNodeSync) return; // SAFETY: Never mark orphans if we only synced a sub-tree
            
            foreach (var kvp in existingCache)
            {
                if (!processedIds.Contains(kvp.Key) && kvp.Value != null)
                {
                    GameObject go = kvp.Value.gameObject;
                    
                    // Don't mark root as orphan
                    if (go.transform.parent == null) continue;

                    go.SetActive(false);
                    var orphan = go.GetComponent<FigmaOrphanedElement>() ?? go.AddComponent<FigmaOrphanedElement>();
                    orphan.Initialize(kvp.Key);
                }
            }
        }
    }
}
