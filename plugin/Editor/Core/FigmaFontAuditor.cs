using System.Collections.Generic;
using System.Linq;
using FigmaImporter.V2.Data;

namespace FigmaImporter.V2.Core
{
    /// <summary>
    /// Audits Figma file fonts against the local FontMappingTable.
    /// </summary>
    internal class FigmaFontAuditor
    {
        private readonly FontMappingTable _fontMapTable;

        public FigmaFontAuditor(FontMappingTable fontMapTable)
        {
            _fontMapTable = fontMapTable;
        }

        public void Audit(FigmaFileResponse response)
        {
            var figmaFonts = new HashSet<(string family, string postScript, int weight)>();
            if (response.nodes != null)
                foreach (var container in response.nodes.Values) CollectFontsRecursive(container.document, figmaFonts);
            else if (response.document != null)
                CollectFontsRecursive(response.document, figmaFonts);

            if (figmaFonts.Count == 0) return;

            List<string> mapped = new List<string>(), missing = new List<string>();

            foreach (var f in figmaFonts)
            {
                bool match = false;
                if (_fontMapTable != null)
                {
                    string norm = (f.family ?? "").Replace(" ", "").ToLower();
                    match = _fontMapTable.Mappings.Any(m =>
                        m.fontPostScriptName == f.postScript ||
                        ((m.figmaFontFamily ?? "").Replace(" ", "").ToLower() == norm &&
                         (m.figmaFontWeight == 0 || m.figmaFontWeight == f.weight)));
                }

                string desc = $"'{f.family}' ({f.weight})";
                if (match) mapped.Add(desc); else missing.Add(desc);
            }

            if (mapped.Count > 0) FigmaLog.Info($"<color=green>Mapped ({mapped.Count}):</color> {string.Join(", ", mapped)}");
            if (missing.Count > 0) FigmaLog.Error($"<color=red>MISSING ({missing.Count}):</color> {string.Join(", ", missing)}");
        }

        private void CollectFontsRecursive(FigmaNode node, HashSet<(string, string, int)> fonts)
        {
            if (node.type == "TEXT" && node.style != null)
                fonts.Add((node.style.fontFamily, node.style.fontPostScriptName, node.style.fontWeight));
            if (node.children != null)
                foreach (var child in node.children) CollectFontsRecursive(child, fonts);
        }
    }
}
