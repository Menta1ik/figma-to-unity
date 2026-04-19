using UnityEditor;
using UnityEngine;

namespace FigmaImporter.V2.Editor.Utils
{
    public static class FigmaEditorStyles
    {
        /// <summary>
        /// Safely returns a GUIStyle from EditorStyles. 
        /// Falls back to common styles if Unity has not yet initialized its skin system.
        /// </summary>
        public static GUIStyle GetSafeStyle(GUIStyle style)
        {
            try
            {
                if (style != null && style.normal != null) return style;
            }
            catch
            {
                // Fallback if Unity internals are still loading
            }
            return GUI.skin.label;
        }

        public static GUIStyle BoldLabel => GetSafeStyle(EditorStyles.boldLabel);
        public static GUIStyle MiniBoldLabel => GetSafeStyle(EditorStyles.miniBoldLabel);
    }
}
