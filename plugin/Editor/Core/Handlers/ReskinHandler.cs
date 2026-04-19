using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using FigmaImporter.V2;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core.Handlers
{
    public class ReskinHandler
    {
        private readonly FigmaHandlerContext _context;

        public ReskinHandler(FigmaHandlerContext context)
        {
            _context = context;
        }

        public void ApplyReskin(GameObject target, FigmaNode newData)
        {
            if (target == null || newData == null) return;

            UpdateVisuals(target, newData);

            if (newData.children != null)
            {
                foreach (var childNode in newData.children)
                {
                    // Find matching child by Figma ID
                    Transform matchedChild = null;
                    foreach (Transform t in target.transform)
                    {
                        var childElement = t.GetComponent<FigmaElement>();
                        if (childElement != null && childElement.FigmaNodeId == childNode.id)
                        {
                            matchedChild = t;
                            break;
                        }
                    }

                    if (matchedChild != null)
                    {
                        ApplyReskin(matchedChild.gameObject, childNode);
                    }
                }
            }
        }

        private void UpdateVisuals(GameObject go, FigmaNode node)
        {
            // 1. Text Update
            if (node.type == "TEXT" && !string.IsNullOrEmpty(node.characters))
            {
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = node.characters;
                    
                    // Update text color from fills
                    if (node.fills != null && node.fills.Count > 0 && node.fills[0].color != null)
                    {
                        float alpha = node.opacity * node.fills[0].opacity;
                        tmp.color = node.fills[0].color.ToUnityColor(alpha);
                    }

                    if (node.style != null)
                    {
                        tmp.fontSize = node.style.fontSize;
                    }
                }
            }

            // 2. Image Update (Color tint)
            var img = go.GetComponent<Image>();
            if (img != null && node.fills != null && node.fills.Count > 0)
            {
                var fill = node.fills[0];
                if (fill.type == "SOLID" && fill.color != null)
                {
                    float alpha = node.opacity * fill.opacity;
                    img.color = fill.color.ToUnityColor(alpha);
                }
            }

            // 3. Update Sync Meta
            var figmaElement = go.GetComponent<FigmaElement>();
            if (figmaElement == null)
            {
                figmaElement = go.AddComponent<FigmaElement>();
            }
            figmaElement.FigmaNodeId = node.id;
            figmaElement.LastUpdateHash = node.computedHash;

            // 4. Queuing images for download if they changed
            if (node.fills != null && node.fills.Any(f => f.type == "IMAGE"))
            {
                _context.ImageNodesToDownload.Add(node);
            }
        }
    }
}

