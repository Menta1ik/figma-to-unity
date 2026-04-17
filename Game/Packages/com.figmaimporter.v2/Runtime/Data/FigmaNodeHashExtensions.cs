using System.Text;
using System.Security.Cryptography;

namespace FigmaImporter.V2.Data
{
    public static class FigmaNodeHashExtensions
    {
        public static string ComputeHash(this FigmaNode node)
        {
            if (node == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(node.id).Append("|");
            sb.Append(node.name).Append("|");
            sb.Append(node.type).Append("|");
            sb.Append(node.visible).Append("|");
            sb.Append(node.opacity).Append("|");
            
            if (node.absoluteBoundingBox != null)
            {
                sb.Append(node.absoluteBoundingBox.x).Append("|");
                sb.Append(node.absoluteBoundingBox.y).Append("|");
                sb.Append(node.absoluteBoundingBox.width).Append("|");
                sb.Append(node.absoluteBoundingBox.height).Append("|");
            }
            
            if (node.characters != null)
            {
                sb.Append(node.characters).Append("|");
            }
            
            if (node.style != null)
            {
                sb.Append(node.style.fontFamily).Append("|");
                sb.Append(node.style.fontPostScriptName).Append("|");
                sb.Append(node.style.fontWeight).Append("|");
                sb.Append(node.style.fontSize).Append("|");
                sb.Append(node.style.textAlignHorizontal).Append("|");
                sb.Append(node.style.textAlignVertical).Append("|");
                sb.Append(node.style.lineHeightPx).Append("|");
                sb.Append(node.style.textCase).Append("|");
            }
            
            if (node.fills != null)
            {
                foreach (var fill in node.fills)
                {
                    sb.Append(fill.type).Append("|");
                    sb.Append(fill.opacity).Append("|");
                    if (fill.color != null)
                    {
                        sb.Append(fill.color.r).Append("|").Append(fill.color.g).Append("|").Append(fill.color.b).Append("|").Append(fill.color.a).Append("|");
                    }
                    if (fill.imageRef != null) sb.Append(fill.imageRef).Append("|");
                }
            }

            if (node.strokes != null)
            {
                foreach (var stroke in node.strokes)
                {
                    sb.Append("s").Append(stroke.type).Append("|");
                    sb.Append(stroke.opacity).Append("|");
                    if (stroke.color != null)
                    {
                        sb.Append(stroke.color.r).Append("|").Append(stroke.color.g).Append("|").Append(stroke.color.b).Append("|").Append(stroke.color.a).Append("|");
                    }
                }
                sb.Append(node.strokeWeight).Append("|");
            }

            sb.Append(node.cornerRadius).Append("|");

            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
