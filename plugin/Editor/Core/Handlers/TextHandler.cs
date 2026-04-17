using UnityEngine;
using TMPro;
using System.Linq;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Handlers
{
    public class TextHandler : IFigmaComponentHandler
    {
        public bool CanHandle(FigmaNode node) => node.type == "TEXT";

        public void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context)
        {
            var tmp = target.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = target.gameObject.AddComponent<TextMeshProUGUI>();
            
            // 1. Text and Case
            string characters = node.characters ?? "";
            if (node.style != null && !string.IsNullOrEmpty(node.style.textCase))
            {
                if (node.style.textCase == "UPPER") characters = characters.ToUpper();
                else if (node.style.textCase == "LOWER") characters = characters.ToLower();
            }
            tmp.text = characters;
            
            if (node.style != null)
            {
                // --- STRICT FONT CLEANUP ---
                float fSize = node.style.fontSize;
                if (float.IsNaN(fSize) || float.IsInfinity(fSize) || fSize < 1f) fSize = 14f; 
                tmp.fontSize = fSize; 
                tmp.overflowMode = TextOverflowModes.Overflow;
                
                tmp.alignment = MapAlignment(node.style.textAlignHorizontal, node.style.textAlignVertical);

                // Line spacing protection
                float lHeight = node.style.lineHeightPx;
                if (!float.IsNaN(lHeight) && !float.IsInfinity(lHeight) && lHeight > 0 && fSize > 0)
                {
                    float spacing = (lHeight / fSize) * 100f - 100f;
                    if (!float.IsNaN(spacing) && !float.IsInfinity(spacing))
                    {
                        // Apply strict limit
                        spacing = Mathf.Clamp(spacing, -50f, 500f);
                        tmp.lineSpacing = spacing;
                    }
                }

                // 2. Font Mapping
                if (context.FontMappings != null && node.style != null)
                {
                    FontMapping bestMatch = null;
                    string figmaFamily = node.style.fontFamily ?? "";
                    string postScript = node.style.fontPostScriptName ?? "";
                    int weight = node.style.fontWeight;
                    
                    // Priority 1: Match by PostScript Name
                    if (!string.IsNullOrEmpty(postScript))
                    {
                        bestMatch = context.FontMappings.FirstOrDefault(m => 
                            !string.IsNullOrEmpty(m.fontPostScriptName) && 
                            m.fontPostScriptName == postScript);
                    }

                    if (bestMatch == null)
                    {
                        string normalizedFigmaFamily = figmaFamily.Replace(" ", "").ToLower();

                        // Search for all mappings of this font family
                        var mappingsForFamily = context.FontMappings
                            .Where(m => (m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == normalizedFigmaFamily)
                            .ToList();

                        if (mappingsForFamily.Count > 0)
                        {
                            // Attempt exact weight match
                            bestMatch = mappingsForFamily.FirstOrDefault(m => m.figmaFontWeight > 0 && m.figmaFontWeight == weight);

                            // Fallback: Find closest weight in the same family
                            if (bestMatch == null)
                            {
                                bestMatch = mappingsForFamily
                                    .OrderBy(m => Mathf.Abs(m.figmaFontWeight - weight))
                                    .FirstOrDefault();
                                
                                if (bestMatch != null)
                                {
                                    Debug.Log($"<color=orange>[FigmaImporter]</color> Exact weight {weight} not found for '{figmaFamily}'. " +
                                              $"Using closest: {bestMatch.figmaFontWeight} ({bestMatch.targetTMPAsset?.name})");
                                }
                            }
                        }
                    }

                    if (bestMatch?.targetTMPAsset != null)
                    {
                        if (tmp.font != bestMatch.targetTMPAsset)
                        {
                            var oldFont = tmp.font != null ? tmp.font.name : "null";
                            tmp.font = bestMatch.targetTMPAsset;
                            Debug.Log($"<color=cyan>[FigmaImporter]</color> Font replaced: <b>{node.name}</b> ('{oldFont}' -> '<b>{tmp.font.name}</b>')");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"<color=red>[FigmaImporter] Font Mapping Missing!</color>\n" +
                                         $"Figma: <b>{figmaFamily}</b> (Weight: {weight})\n" +
                                         $"PostScript: <color=orange>{postScript}</color>\n" +
                                         $"<i>Please add this to your FontMappingTable asset.</i>");
                        
                        if (context.GlobalFont != null) tmp.font = context.GlobalFont;
                    }

                    // --- VISIBILITY DIAGNOSTICS ---
                    bool isVisible = node.visible;
                    float nodeOpacity = node.opacity;
                    
                    if (!isVisible || nodeOpacity < 0.01f)
                    {
                        // Keep only important hidden info
                        // Debug.Log($"<color=gray>[Diagnostic]</color> Text '{node.name}' hidden...");
                    }

                    // FORCED UPDATE (Important for Unity 6)
                    tmp.SetAllDirty();
                    tmp.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
                    Canvas.ForceUpdateCanvases(); 
                    UnityEditor.EditorUtility.SetDirty(tmp);

                    // LOG FINAL STATE (Disabled to reduce noise)
                    // var rt = tmp.rectTransform;
                    // Debug.Log($"[Diagnostic] Text '{node.name}' set at {rt.anchoredPosition} with size {rt.sizeDelta}. Font: {tmp.font?.name}, Color: {tmp.color}");
                }
                else if (context.GlobalFont != null)
                {
                    tmp.font = context.GlobalFont;
                }

                // --- PROTECTION AGAINST EMPTY FONT ---
                if (tmp.font == null)
                {
                    Debug.LogError($"[Figma v2.1] 💀 CRITICAL ERROR: Font not assigned for text '{node.name}'.");
                    tmp.enabled = false; 
                }
                else
                {
                    tmp.enabled = node.visible; // Sync visibility
                    tmp.gameObject.layer = 5;   // Force UI Layer
                    tmp.ForceMeshUpdate();      // Ensure mesh generation
                }
            }
            
            if (node.fills != null)
            {
                var textFill = node.fills.FirstOrDefault(f => f.type == "SOLID" && f.visible);
                if (textFill != null && textFill.color != null) 
                {
                    float combinedOpacity = node.opacity * textFill.opacity;
                    tmp.color = textFill.color.ToUnityColor(combinedOpacity);
                }
            }
        }

        private TextAlignmentOptions MapAlignment(string h, string v)
        {
            if (h == "LEFT" && v == "TOP") return TextAlignmentOptions.TopLeft;
            if (h == "CENTER" && v == "TOP") return TextAlignmentOptions.Top;
            if (h == "RIGHT" && v == "TOP") return TextAlignmentOptions.TopRight;
            
            if (h == "LEFT" && v == "CENTER") return TextAlignmentOptions.Left;
            if (h == "CENTER" && v == "CENTER") return TextAlignmentOptions.Center;
            if (h == "RIGHT" && v == "CENTER") return TextAlignmentOptions.Right;

            if (h == "LEFT" && v == "BOTTOM") return TextAlignmentOptions.BottomLeft;
            if (h == "CENTER" && v == "BOTTOM") return TextAlignmentOptions.Bottom;
            if (h == "RIGHT" && v == "BOTTOM") return TextAlignmentOptions.BottomRight;

            // Fallback
            return TextAlignmentOptions.Left;
        }
    }
}
