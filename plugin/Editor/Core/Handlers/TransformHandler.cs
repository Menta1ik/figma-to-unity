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
                if (PrefabUtility.IsPartOfAnyPrefab(target.gameObject))
                {
                    target.transform.localPosition = Vector3.zero;
                    return;
                }
                rt = target.gameObject.AddComponent<RectTransform>();
            }

            // Fallback for missing bounding box (e.g. GROUP nodes)
            float boxX = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.x : 0f;
            float boxY = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.y : 0f;
            float boxWidth = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.width : 1f;
            float boxHeight = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.height : 1f;

            // PROTECT ROOT: Never modify the pivot, scale or position of the Root Canvas itself
            if (target.transform == context.RootTransform)
            {
                target.AbsoluteBox = new Rect(boxX, boxY, boxWidth, boxHeight);
                return;
            }

            target.AbsoluteBox = new Rect(boxX, boxY, boxWidth, boxHeight);
            target.gameObject.name = node.name;
            target.gameObject.SetActive(node.visible != false);

            // Hard reset scale/rotation
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            // SMART CENTERING / POSITIONING: If this is a top-level child of Canvas
            if (context.ParentNode == null)
            {
                // CRITICAL: We must store the absolute box even for root objects, 
                // so their children can calculate relative positions correctly!
                target.AbsoluteBox = new Rect(boxX, boxY, boxWidth, boxHeight);

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f); 
                rt.sizeDelta = new Vector2(boxWidth, boxHeight);
                rt.anchoredPosition = Vector2.zero; 
                
                Debug.Log($"[Figma v2.4.1] Smart Centered Root Frame: '{node.name}' at Center (Global Figma: {boxX}, {boxY})");
                return; 
            }

            rt.pivot = new Vector2(0, 1);

            if (context.Settings != null && context.Settings.enableConstraintsTranslation && node.layoutMode == null)
            {
                ApplyConstraints(node, rt, context);
            }
            else
            {
                ApplyAbsolutePosition(node, target, rt);
            }
        }

        private void ApplyConstraints(FigmaNode node, RectTransform rt, FigmaHandlerContext context)
        {
            if (node.constraints == null || context.ParentNode == null || context.ParentNode.absoluteBoundingBox == null || node.absoluteBoundingBox == null)
            {
                ApplyAbsolutePosition(node, null, rt);
                return;
            }

            var parentBbox = context.ParentNode.absoluteBoundingBox;
            var nodeBbox = node.absoluteBoundingBox;

            // X-Axis
            switch (node.constraints.horizontal)
            {
                case "LEFT":
                    rt.anchorMin = new Vector2(0, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(0, rt.anchorMax.y);
                    break;
                case "RIGHT":
                    rt.anchorMin = new Vector2(1, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(1, rt.anchorMax.y);
                    break;
                case "CENTER":
                    rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
                    break;
                case "LEFT_RIGHT": // STRETCH
                    rt.anchorMin = new Vector2(0, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(1, rt.anchorMax.y);
                    break;
                case "SCALE":
                    float xMin = (nodeBbox.x - parentBbox.x) / parentBbox.width;
                    float xMax = (nodeBbox.x + nodeBbox.width - parentBbox.x) / parentBbox.width;
                    rt.anchorMin = new Vector2(xMin, rt.anchorMin.y);
                    rt.anchorMax = new Vector2(xMax, rt.anchorMax.y);
                    break;
            }

            // Y-Axis
            switch (node.constraints.vertical)
            {
                case "TOP":
                    rt.anchorMin = new Vector2(rt.anchorMin.x, 1);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, 1);
                    break;
                case "BOTTOM":
                    rt.anchorMin = new Vector2(rt.anchorMin.x, 0);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, 0);
                    break;
                case "CENTER":
                    rt.anchorMin = new Vector2(rt.anchorMin.x, 0.5f);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, 0.5f);
                    break;
                case "TOP_BOTTOM": // STRETCH
                    rt.anchorMin = new Vector2(rt.anchorMin.x, 0);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, 1);
                    break;
                case "SCALE":
                    float yMin = 1f - ((nodeBbox.y + nodeBbox.height - parentBbox.y) / parentBbox.height);
                    float yMax = 1f - ((nodeBbox.y - parentBbox.y) / parentBbox.height);
                    rt.anchorMin = new Vector2(rt.anchorMin.x, yMin);
                    rt.anchorMax = new Vector2(rt.anchorMax.x, yMax);
                    break;
            }

            // Now apply offsets based on calculated anchors
            // Formula: offset = Position - (Anchor * ParentSize)
            float left = nodeBbox.x - parentBbox.x;
            float right = nodeBbox.x + nodeBbox.width - parentBbox.x;
            float top = nodeBbox.y - parentBbox.y;
            float bottom = nodeBbox.y + nodeBbox.height - parentBbox.y;

            // Unity Y is bottom-up, Figma is top-down
            // parentHeight - top = Unity top
            // parentHeight - bottom = Unity bottom
            float unityTop = parentBbox.height - top;
            float unityBottom = parentBbox.height - bottom;

            rt.offsetMin = new Vector2(left - (rt.anchorMin.x * parentBbox.width), unityBottom - (rt.anchorMin.y * parentBbox.height));
            rt.offsetMax = new Vector2(right - (rt.anchorMax.x * parentBbox.width), unityTop - (rt.anchorMax.y * parentBbox.height));
        }

        private void ApplyAbsolutePosition(FigmaNode node, FigmaElement target, RectTransform rt)
        {
            // Figma's absolute positioning is relative to Top-Left
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            float boxWidth = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.width : 1f;
            float boxHeight = node.absoluteBoundingBox != null ? node.absoluteBoundingBox.height : 1f;
            rt.sizeDelta = new Vector2(boxWidth, boxHeight);

            float localX = 0, localY = 0;
            if (rt.transform.parent != null)
            {
                var parentElement = rt.transform.parent.GetComponent<FigmaElement>();
                if (parentElement != null && node.absoluteBoundingBox != null)
                {
                    // Calculate relative offset from parent's absolute Top-Left
                    localX = node.absoluteBoundingBox.x - parentElement.AbsoluteBox.x;
                    localY = -(node.absoluteBoundingBox.y - parentElement.AbsoluteBox.y);
                }
            }

            rt.anchoredPosition = new Vector2(localX, localY);
        }
    }
}
