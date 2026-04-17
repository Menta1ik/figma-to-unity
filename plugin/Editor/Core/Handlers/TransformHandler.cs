using UnityEngine;
using UnityEditor;
using FigmaImporter.V2.Data;
using FigmaImporter.V2.Core.Validation;

namespace FigmaImporter.V2.Core.Handlers
{
    public class TransformHandler : IFigmaComponentHandler
    {
        public bool CanHandle(FigmaNode node) => true;

        public void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt == null) 
            {
                // If we can't add it (prefab instance), just use regular transform position as fallback, or skip.
                if (PrefabUtility.IsPartOfAnyPrefab(target.gameObject))
                {
                    // For regular Transform, we just move it.
                    target.transform.localPosition = Vector3.zero;
                    return;
                }
                rt = target.gameObject.AddComponent<RectTransform>();
            }

            // 1. HARD RESET
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }

            // Fallback for missing bounding box (e.g. GROUP nodes)
            float boxX = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.x : 0f;
            float boxY = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.y : 0f;
            float boxWidth = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.width : 1f;
            float boxHeight = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.height : 1f;

            target.AbsoluteBox = new Rect(boxX, boxY, boxWidth, boxHeight);

            // 2. CALCULATE LOCAL POSITION
            float localX = 0, localY = 0;
            if (target.transform.parent != null)
            {
                var parentElement = target.transform.parent.GetComponent<FigmaElement>();
                if (parentElement != null)
                {
                    localX = boxX - parentElement.AbsoluteBox.x;
                    localY = -(boxY - parentElement.AbsoluteBox.y);
                }
            }

            // 3. APPLY TO UNITY (Validation happens in Parser Stages 2 & 3)
            rt.sizeDelta = new Vector2(boxWidth, boxHeight);
            rt.anchoredPosition3D = new Vector3(localX, localY, 0f);
            rt.localScale = Vector3.one;

            target.gameObject.name = node.name;
            target.gameObject.SetActive(node.visible != false);
            
            // NO MASKING HERE. Deferred to FigmaParser.ApplyDeferredMasks() for stability.
        }
    }
}
