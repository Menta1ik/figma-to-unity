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
        public static GUIStyle GetSafeStyle(System.Func<GUIStyle> getter)
        {
            try
            {
                var style = getter();
                if (style != null && style.normal != null) return style;
            }
            catch
            {
                // Unity internals are not ready yet
            }

            if (GUI.skin != null && GUI.skin.label != null)
                return GUI.skin.label;

            return new GUIStyle(); // Ultra fallback to avoid NRE
        }

        public static GUIStyle BoldLabel => GetSafeStyle(() => EditorStyles.boldLabel);
        public static GUIStyle MiniBoldLabel => GetSafeStyle(() => EditorStyles.miniBoldLabel);
    }
}
