using UnityEngine;
using System.Collections.Generic;
using System.Text;
using TMPro;

namespace FigmaImporter.V2
{
    [System.Serializable]
    public class FontMapping
    {
        [Header("Search Criteria (Fill what's needed)")]
        [Tooltip("Search by PostScript name (e.g. FaricyNew-Bold)")]
        public string fontPostScriptName;

        [Tooltip("Search by substring in Figma Font Family (e.g. Faricy New)")]
        public string figmaFontFamily;

        [Tooltip("Search by Figma font weight (400=Regular, 700=Bold). 0 = any weight.")]
        public int figmaFontWeight;

        [Space(10)]
        [Tooltip("Local TextMeshPro asset")]
        public TMP_FontAsset targetTMPAsset;
    }

    [CreateAssetMenu(fileName = "FontMappingTable", menuName = "Figma Importer/Font Mapping Table")]
    public class FontMappingTable : ScriptableObject
    {
        [Tooltip("Fallback font if mapping not found")]
        public TMP_FontAsset GlobalFallbackFont;
        
        [Tooltip("Mapping table: Figma Font -> TMP_FontAsset")]
        public List<FontMapping> Mappings = new List<FontMapping>();

        /// <summary>
        /// Вычисляет хеш текущего состояния таблицы для Smart Sync.
        /// </summary>
        public string GetTableHash()
        {
            var sb = new StringBuilder();
            if (GlobalFallbackFont != null) sb.Append(GlobalFallbackFont.name);
            
            foreach (var m in Mappings)
            {
                sb.Append(m.fontPostScriptName);
                sb.Append(m.figmaFontFamily);
                sb.Append(m.figmaFontWeight);
                if (m.targetTMPAsset != null) sb.Append(m.targetTMPAsset.name);
            }

            return sb.ToString().GetHashCode().ToString();
        }
    }
}
