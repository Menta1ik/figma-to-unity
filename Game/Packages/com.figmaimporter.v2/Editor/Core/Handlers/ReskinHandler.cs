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

            var children = GetFigmaChildren(target.transform);
            
            if (newData.children != null)
            {
                foreach (var childNode in newData.children)
                {
                    var matchingChild = children.FirstOrDefault(c => c.name == childNode.name);
                    if (matchingChild != null)
                    {
                        ApplyReskin(matchingChild.gameObject, childNode);
                    }
                }
            }
        }

        private void UpdateVisuals(GameObject go, FigmaNode node)
        {
            var img = go.GetComponent<Image>();
            if (img != null && node.fills != null && node.fills.Count > 0)
            {
                var fill = node.fills[0];
                if (fill.color != null)
                {
                    img.color = new Color(fill.color.r, fill.color.g, fill.color.b, fill.opacity);
                }
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text != null && !string.IsNullOrEmpty(node.characters))
            {
                text.text = node.characters;
                if (node.style != null)
                {
                    text.fontSize = node.style.fontSize;
                }
            }

            var figmaElement = go.GetComponent<FigmaElement>();
            if (figmaElement == null)
            {
                figmaElement = go.AddComponent<FigmaElement>();
            }
            figmaElement.FigmaNodeId = node.id;
            figmaElement.LastUpdateHash = node.computedHash;
        }

        private List<Transform> GetFigmaChildren(Transform parent)
        {
            var list = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                list.Add(parent.GetChild(i));
            }
            return list;
        }
    }
}
