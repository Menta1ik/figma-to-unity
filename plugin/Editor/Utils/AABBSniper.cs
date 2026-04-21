using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using FigmaImporter.V2;

namespace FigmaImporter.V2.Utils
{
    public static class AABBSniper
    {
        [MenuItem("Figma Importer/Utilities/🔫 EMERGENCY NaN CLEAR (No Windows)")]
        public static void EmergencyHunt()
        {
            // 1. FIRST AID: Force disable all Canvases on scene
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in allCanvases) c.enabled = false;

            int found = 0;
            var allRects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
            
            FigmaLog.Info($"<color=cyan>[Sniper]</color> Starting emergency scan of {allRects.Length} objects...");

            foreach (var rt in allRects)
            {
                if (rt == null) continue;

                bool isBad = IsBad(rt.anchoredPosition.x) || IsBad(rt.anchoredPosition.y) || 
                             IsBad(rt.sizeDelta.x) || IsBad(rt.sizeDelta.y) ||
                             IsBad(rt.localScale.x) || IsBad(rt.localScale.y) || IsBad(rt.anchorMin.x);

                if (!isBad)
                {
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    foreach (var c in corners) if (IsBad(c.x) || IsBad(c.y)) { isBad = true; break; }
                }

                if (isBad)
                {
                    found++;
                    string name = rt.gameObject.name;
                    FigmaLog.Error($"<color=red>[TARGET DESTROYED]</color> {name}. NaN detected. OBJECT REMOVED.");
                    // Physically remove object to unblock render
                    Object.DestroyImmediate(rt.gameObject);
                }
            }

            // 2. Включаем Canvas обратно
            foreach (var c in allCanvases) c.enabled = true;

            EditorUtility.DisplayDialog("Operation Result", 
                found > 0 ? $"Destroyed {found} objects with NaN. Unity should be alive now!" : "No NaN detected. Try restarting Unity if it's still laggy.", "OK");
        }

        private static bool IsBad(float v) => float.IsNaN(v) || float.IsInfinity(v);
    }
}
