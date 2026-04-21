using UnityEngine;
using UnityEngine.UI;
using FigmaImporter.V2.Data;
using FigmaImporter.V2;

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
            bool isAutoLayout = !string.IsNullOrEmpty(node.layoutMode) &&
                                (node.layoutMode == "HORIZONTAL" || node.layoutMode == "VERTICAL");
            return isAutoLayout || node.layoutGrow > 0;
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

            if (!string.IsNullOrEmpty(node.layoutMode))
            {
                ApplyContentSizeFitter(node, go);
            }

            ApplyLayoutGrow(node, go, context);
        }

        private void ApplyHorizontalLayout(FigmaNode node, GameObject go)
        {
            // Remove conflicting layout if present
            var existingVertical = go.GetComponent<VerticalLayoutGroup>();
            if (existingVertical != null) Object.DestroyImmediate(existingVertical);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = go.AddComponent<HorizontalLayoutGroup>();

            ConfigureLayoutGroup(layout, node);

            FigmaLog.Verbose($"[FigmaImporter] Applied HorizontalLayoutGroup to '{go.name}' " +
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

            FigmaLog.Verbose($"[FigmaImporter] Applied VerticalLayoutGroup to '{go.name}' " +
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

        private void ApplyLayoutGrow(FigmaNode node, GameObject go, FigmaHandlerContext context)
        {
            if (node.layoutGrow <= 0) return;

            var parent = context.ParentNode;
            if (parent == null || string.IsNullOrEmpty(parent.layoutMode)) return;

            var layoutElement = go.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = go.AddComponent<LayoutElement>();

            if (parent.layoutMode == "HORIZONTAL")
                layoutElement.flexibleWidth = node.layoutGrow;
            else
                layoutElement.flexibleHeight = node.layoutGrow;
        }

        private void ApplyContentSizeFitter(FigmaNode node, GameObject go)
        {
            bool hasAutoPrimary = node.primaryAxisSizingMode == "AUTO";
            bool hasAutoCounter = node.counterAxisSizingMode == "AUTO";
            bool needsFitter = hasAutoPrimary || hasAutoCounter;
            
            var fitter = go.GetComponent<ContentSizeFitter>();
            
            if (!needsFitter)
            {
                if (fitter != null) Object.DestroyImmediate(fitter);
                return;
            }

            if (fitter == null) fitter = go.AddComponent<ContentSizeFitter>();

            bool isHorizontal = node.layoutMode == "HORIZONTAL";

            // Horizontal Fit
            bool isHorizontalAuto = isHorizontal ? hasAutoPrimary : hasAutoCounter;
            fitter.horizontalFit = isHorizontalAuto 
                ? ContentSizeFitter.FitMode.PreferredSize 
                : ContentSizeFitter.FitMode.Unconstrained;

            // Vertical Fit
            bool isVerticalAuto = isHorizontal ? hasAutoCounter : hasAutoPrimary;
            fitter.verticalFit = isVerticalAuto 
                ? ContentSizeFitter.FitMode.PreferredSize 
                : ContentSizeFitter.FitMode.Unconstrained;
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
