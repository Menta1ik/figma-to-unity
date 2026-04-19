using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Handlers
{
    /// <summary>
    /// Translates Figma Auto Layout properties into Unity Layout Groups.
    /// Handles HORIZONTAL → HorizontalLayoutGroup, VERTICAL → VerticalLayoutGroup,
    /// with padding, spacing, and child alignment.
    /// </summary>
    public class LayoutHandler : IFigmaComponentHandler
    {
        public bool CanHandle(FigmaNode node)
        {
            // Only handle nodes that have Auto Layout enabled
            return !string.IsNullOrEmpty(node.layoutMode) && 
                   (node.layoutMode == "HORIZONTAL" || node.layoutMode == "VERTICAL");
        }

        public void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context)
        {
            var go = target.gameObject;

            if (node.layoutMode == "HORIZONTAL")
            {
                ApplyHorizontalLayout(node, go);
            }
            else if (node.layoutMode == "VERTICAL")
            {
                ApplyVerticalLayout(node, go);
            }

            // Content Size Fitter for AUTO sizing
            ApplyContentSizeFitter(node, go);
        }

        private void ApplyHorizontalLayout(FigmaNode node, GameObject go)
        {
            // Remove conflicting layout if present
            var existingVertical = go.GetComponent<VerticalLayoutGroup>();
            if (existingVertical != null) Object.DestroyImmediate(existingVertical);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = go.AddComponent<HorizontalLayoutGroup>();

            ConfigureLayoutGroup(layout, node);

            Debug.Log($"[FigmaImporter] Applied HorizontalLayoutGroup to '{go.name}' " +
                      $"(spacing: {node.itemSpacing}, padding: L{node.paddingLeft}/R{node.paddingRight}/T{node.paddingTop}/B{node.paddingBottom})");
        }

        private void ApplyVerticalLayout(FigmaNode node, GameObject go)
        {
            // Remove conflicting layout if present
            var existingHorizontal = go.GetComponent<HorizontalLayoutGroup>();
            if (existingHorizontal != null) Object.DestroyImmediate(existingHorizontal);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = go.AddComponent<VerticalLayoutGroup>();

            ConfigureLayoutGroup(layout, node);

            Debug.Log($"[FigmaImporter] Applied VerticalLayoutGroup to '{go.name}' " +
                      $"(spacing: {node.itemSpacing}, padding: L{node.paddingLeft}/R{node.paddingRight}/T{node.paddingTop}/B{node.paddingBottom})");
        }

        private void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup layout, FigmaNode node)
        {
            // Spacing between children
            layout.spacing = node.itemSpacing;

            // Padding
            layout.padding = new RectOffset(
                Mathf.RoundToInt(node.paddingLeft),
                Mathf.RoundToInt(node.paddingRight),
                Mathf.RoundToInt(node.paddingTop),
                Mathf.RoundToInt(node.paddingBottom)
            );

            // Child alignment based on Figma axis alignment
            layout.childAlignment = MapAlignment(node);

            // Child control size based on sizing modes
            bool isHorizontal = node.layoutMode == "HORIZONTAL";

            // For primary axis: AUTO means children control the container size
            // For counter axis: "FIXED" means container doesn't resize to fit children
            layout.childControlWidth = isHorizontal 
                ? (node.primaryAxisSizingMode == "AUTO") 
                : (node.counterAxisSizingMode == "AUTO");
            layout.childControlHeight = isHorizontal 
                ? (node.counterAxisSizingMode == "AUTO") 
                : (node.primaryAxisSizingMode == "AUTO");

            // Force expand: when children should stretch to fill the axis
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void ApplyContentSizeFitter(FigmaNode node, GameObject go)
        {
            bool needsFitter = node.primaryAxisSizingMode == "AUTO" || node.counterAxisSizingMode == "AUTO";
            
            var fitter = go.GetComponent<ContentSizeFitter>();
            
            if (!needsFitter)
            {
                if (fitter != null) Object.DestroyImmediate(fitter);
                return;
            }

            if (fitter == null) fitter = go.AddComponent<ContentSizeFitter>();

            bool isHorizontal = node.layoutMode == "HORIZONTAL";

            // Primary axis AUTO → fit
            if (node.primaryAxisSizingMode == "AUTO")
            {
                if (isHorizontal)
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                else
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Counter axis AUTO → fit
            if (node.counterAxisSizingMode == "AUTO")
            {
                if (isHorizontal)
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                else
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        /// <summary>
        /// Maps Figma primaryAxisAlignItems + counterAxisAlignItems to Unity TextAnchor.
        /// </summary>
        private TextAnchor MapAlignment(FigmaNode node)
        {
            string primary = node.primaryAxisAlignItems ?? "MIN";
            string counter = node.counterAxisAlignItems ?? "MIN";
            bool isHorizontal = node.layoutMode == "HORIZONTAL";

            // For horizontal layout: primary = horizontal, counter = vertical
            // For vertical layout:   primary = vertical,   counter = horizontal
            string hAlign, vAlign;
            if (isHorizontal)
            {
                hAlign = primary;
                vAlign = counter;
            }
            else
            {
                hAlign = counter;
                vAlign = primary;
            }

            // Map to TextAnchor (3x3 grid)
            if (vAlign == "MIN" && hAlign == "MIN") return TextAnchor.UpperLeft;
            if (vAlign == "MIN" && hAlign == "CENTER") return TextAnchor.UpperCenter;
            if (vAlign == "MIN" && (hAlign == "MAX" || hAlign == "SPACE_BETWEEN")) return TextAnchor.UpperRight;

            if (vAlign == "CENTER" && hAlign == "MIN") return TextAnchor.MiddleLeft;
            if (vAlign == "CENTER" && hAlign == "CENTER") return TextAnchor.MiddleCenter;
            if (vAlign == "CENTER" && (hAlign == "MAX" || hAlign == "SPACE_BETWEEN")) return TextAnchor.MiddleRight;

            if ((vAlign == "MAX" || vAlign == "SPACE_BETWEEN") && hAlign == "MIN") return TextAnchor.LowerLeft;
            if ((vAlign == "MAX" || vAlign == "SPACE_BETWEEN") && hAlign == "CENTER") return TextAnchor.LowerCenter;
            if ((vAlign == "MAX" || vAlign == "SPACE_BETWEEN") && (hAlign == "MAX" || hAlign == "SPACE_BETWEEN")) return TextAnchor.LowerRight;

            return TextAnchor.UpperLeft;
        }
    }
}
