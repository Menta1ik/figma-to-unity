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

            // Component/Instance/Group/Frame can be icons, handled for flattening.
            if (type == "COMPONENT" || type == "INSTANCE" || type == "GROUP" || type == "FRAME")
            {
                return IsIconCandidate(node);
            }

            return false;
        }

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
            var image = target.GetComponent<Image>();
            if (image == null) image = target.gameObject.AddComponent<Image>();

            bool hasFills = node.fills != null && node.fills.Count > 0;
            bool hasImageFill = hasFills && node.fills.Any(f => f.type == "IMAGE");
            bool hasGradientFill = hasFills && node.fills.Any(f => f.type.StartsWith("GRADIENT"));
            
            string type = node.type.ToUpper();
            bool isComplexVector = type == "VECTOR" || type == "STAR" || type == "REGULAR_POLYGON" || 
                                   type == "POLYGON" || type == "ELLIPSE" || type == "BOOLEAN_OPERATION";
            
            bool isRectangle = type == "RECTANGLE";

            bool hasStroke = node.strokes != null && node.strokes.Any(s => s.visible != false);
            bool hasCornerRadius = node.cornerRadius > 0f;

            // DOWNLOAD CRITERIA (highly selective):
            // 1. Real raster image exists in fill (IMAGE).
            // 2. Complex vector primitive (requires PNG export for accuracy).
            // 3. Container explicitly marked as icon (IsIconCandidate).
            // 4. Rectangle with gradient (Unity UI doesn't support gradients natively).
            // 5. Has stroke or corner radius (Unity UI Image lacks native support).
            bool shouldDownload = hasImageFill || 
                                 (isComplexVector && (hasGradientFill || hasFills || hasStroke)) || 
                                 (isRectangle && (hasGradientFill || hasStroke || hasCornerRadius)) ||
                                 IsIconCandidate(node);

            if (shouldDownload)
            {
                // Keep transparent until download complete!
                image.color = new Color(1f, 1f, 1f, 0f);
                context.ImageNodesToDownload.Add(node);
                
                // List of reasons for the log:
                string reason = hasImageFill ? "IMAGE Fill" : 
                               (isComplexVector ? "Complex Vector" : 
                               (hasStroke ? "Has Stroke" :
                               (hasCornerRadius ? "Has Corner Radius" :
                               (isRectangle && hasGradientFill ? "Gradient Rectangle" : "ICON Candidate"))));

                Debug.Log($"[FigmaImporter] Node '{node.name}' ({type}) queued for download. Reason: {reason}");
                
                return;
            }

            // Otherwise, apply solid color
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

            // If empty container (Frame/Group) and not an icon candidate - remove Image
            if (!hasFills && !hasStroke && !isComplexVector && !isRectangle)
            {
                bool preserve = context.Settings != null && context.Settings.preserveManualComponents;
                if (!preserve) 
                {
                    Object.DestroyImmediate(image);
                }
                else
                {
                    // If preserving, just make it invisible
                    image.color = new Color(0, 0, 0, 0);
                    image.sprite = null;
                    image.raycastTarget = false;
                }
            }
            else
            {
                image.color = new Color(0, 0, 0, 0);
                image.sprite = null;
            }
        }
    }
}
