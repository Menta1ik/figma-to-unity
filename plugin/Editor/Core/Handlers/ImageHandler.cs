using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Handlers
{
    public class ImageHandler : IFigmaComponentHandler
    {
        public bool CanHandle(FigmaNode node)
        {
            if (node.type == "TEXT") return false;
            
            // Apply to any node with fills or strokes to apply color (Solid Color).
            if (node.fills != null && node.fills.Any(f => f.visible != false)) return true;
            if (node.strokes != null && node.strokes.Any(s => s.visible != false)) return true;
            
            string type = node.type.ToUpper();
            // These types ALWAYS require handling (vectors or complex shapes)
            if (type == "VECTOR" || type == "BOOLEAN_OPERATION" || type == "STAR" || 
                type == "REGULAR_POLYGON" || type == "POLYGON" || type == "ELLIPSE") return true;

            // Containers might be icons (candidates for flattening)
            if (type == "COMPONENT" || type == "INSTANCE" || type == "GROUP" || type == "FRAME") return true;

            return false;
        }

        // New overload or logic to use context for better matching in CanHandle
        // Wait, CanHandle is defined in Interface without context. 
        // We will move the IconCandidate check into Apply, OR we use a trick.
        // Actually, the interface is IFigmaComponentHandler { bool CanHandle(FigmaNode node); void Apply(...) }
        // If we can't change CanHandle signature, we let it return true for potential candidates
        // and do the final check in Apply.


        private bool IsIconCandidate(FigmaNode node)
        {
            string name = node.name.ToLower();
            bool hasIconName = name.Contains("icon") || name.Contains("img") || 
                              name.Contains("avatar") || name.Contains("illustration") || 
                              name.Contains("logo") || name.Contains("pic");
            
            if (!hasIconName) return false;

            // Check for text inside: if it's a container with text (e.g. Button "OK_Icon"),
            // do not flatten to image to preserve text editability.
            bool hasTextChildren = HasTextRecursive(node);
            return !hasTextChildren;
        }

        private bool HasTextRecursive(FigmaNode node)
        {
            if (node.type == "TEXT") return true;
            if (node.children == null) return false;
            return node.children.Any(HasTextRecursive);
        }

        public void Apply(FigmaNode node, FigmaElement target, FigmaHandlerContext context)
        {
            string type = node.type.ToUpper();
            bool isContainer = type == "COMPONENT" || type == "INSTANCE" || type == "GROUP" || type == "FRAME";
            bool isIcon = false;

            if (isContainer)
            {
                isIcon = IsIconCandidateCached(node, context);
                
                // If container is NOT an icon and has no visible fills/strokes, we don't need an Image component
                bool hasVisibleFills = node.fills != null && node.fills.Any(f => f.visible != false);
                bool hasVisibleStrokes = node.strokes != null && node.strokes.Any(s => s.visible != false);
                
                if (!isIcon && !hasVisibleFills && !hasVisibleStrokes)
                {
                    var existingImg = target.GetComponent<Image>();
                    if (existingImg != null && (context.Settings == null || !context.Settings.preserveManualComponents))
                    {
                        Object.DestroyImmediate(existingImg);
                    }
                    return;
                }
            }

            var image = target.GetComponent<Image>();
            if (image == null) image = target.gameObject.AddComponent<Image>();

            bool hasFills = node.fills != null && node.fills.Count > 0;
            bool hasImageFill = hasFills && node.fills.Any(f => f.type == "IMAGE");
            bool hasGradientFill = hasFills && node.fills.Any(f => f.type.StartsWith("GRADIENT"));
            
            bool isComplexVector = type == "VECTOR" || type == "STAR" || type == "REGULAR_POLYGON" || 
                                   type == "POLYGON" || type == "ELLIPSE" || type == "BOOLEAN_OPERATION";
            
            bool isRectangle = type == "RECTANGLE";

            bool hasStroke = node.strokes != null && node.strokes.Any(s => s.visible != false);
            bool hasCornerRadius = node.cornerRadius > 0f;

            // DOWNLOAD CRITERIA
            bool shouldDownload = hasImageFill || 
                                 (isComplexVector && (hasFills || hasStroke)) || 
                                 (isRectangle && (hasGradientFill || hasStroke || hasCornerRadius)) ||
                                 isIcon;

            if (shouldDownload)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                context.ImageNodesToDownload.Add(node);
                
                string reason = hasImageFill ? "IMAGE Fill" : 
                               (isComplexVector ? "Complex Vector" : 
                               (hasStroke ? "Has Stroke" :
                               (hasCornerRadius ? "Has Corner Radius" :
                               (isRectangle && hasGradientFill ? "Gradient Rectangle" : "ICON Candidate"))));

                Debug.Log($"[FigmaImporter] Node '{node.name}' ({type}) queued for download. Reason: {reason}");
                return;
            }

            // Solid Color
            if (hasFills && !hasStroke)
            {
                var solidFill = node.fills.FirstOrDefault(f => f.type == "SOLID" && f.visible != false);
                if (solidFill != null && solidFill.color != null) 
                {
                    float combinedOpacity = node.opacity * solidFill.opacity;
                    image.color = solidFill.color.ToUnityColor(combinedOpacity);
                    image.sprite = null; 
                    return;
                }
            }

            // Invisible/Fallback
            image.color = new Color(0, 0, 0, 0);
            image.sprite = null;
        }

        private bool IsIconCandidateCached(FigmaNode node, FigmaHandlerContext context)
        {
            if (context.IconCandidateCache.TryGetValue(node.id, out bool result))
                return result;

            result = IsIconCandidate(node);
            context.IconCandidateCache[node.id] = result;
            return result;
        }
    }
}
