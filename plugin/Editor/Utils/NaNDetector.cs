using UnityEngine;
using UnityEditor;

namespace FigmaImporter.V2.Utils
{
    public class NaNDetector : EditorWindow
    {
        [MenuItem("Figma Importer/Utilities/🛠 Radar: Find and Fix NaN")]
        public static void ShowWindow()
        {
            GetWindow<NaNDetector>("NaN Radar");
        }

        private void OnGUI()
        {
            // NEW ULTIMATE GUARD: EditorStyles may throw NullReferenceException during domain reload if Unity is unstable.
            // We catch EVERYTHING here and just skip drawing this frame, requesting a repaint.
            try 
            {
                _ = EditorStyles.boldLabel;
                _ = EditorStyles.toolbarButtonRight; 
            }
            catch (System.Exception)
            {
                Repaint();
                return;
            }

            try 
            {
                GUILayout.Label("uGUI Anomaly Radar (v2.3.1)", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                if (GUILayout.Button("📡 SCAN SCENE", GUILayout.Height(40)))
                {
                    ScanAndFix(false);
                }

                EditorGUILayout.Space();

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("🛠 SCAN AND FIX (EXPERIMENTAL)", GUILayout.Height(40)))
                {
                    ScanAndFix(true);
                }
                GUI.backgroundColor = Color.white;
            }
            catch (System.Exception)
            {
                Repaint();
            }
        }

        private void ScanAndFix(bool fix)
        {
            // Используем версию без Obsolete параметров
            RectTransform[] allRects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
            int found = 0;

            foreach (var rt in allRects)
            {
                bool isInvalid = IsInvalid(rt.anchoredPosition) || IsInvalid(rt.sizeDelta) || IsInvalid(rt.localScale) || IsInvalid(rt.localPosition);

                if (isInvalid)
                {
                    found++;
                    Debug.LogError($"[Radar] Invalid values found in: {rt.gameObject.name}", rt.gameObject);
                    if (fix)
                    {
                        Undo.RecordObject(rt, "Fix NaN");
                        if (IsInvalid(rt.anchoredPosition)) rt.anchoredPosition = Vector2.zero;
                        if (IsInvalid(rt.sizeDelta)) rt.sizeDelta = new Vector2(100, 100);
                        if (IsInvalid(rt.localScale)) rt.localScale = Vector3.one;
                        if (IsInvalid(rt.localPosition)) rt.localPosition = Vector3.zero;
                        EditorUtility.SetDirty(rt);
                    }
                }
            }

            EditorUtility.DisplayDialog("NaN Radar", $"Scan completed.\nIssues found: {found}\nFixed: {(fix ? found : 0)}", "OK");
        }

        private bool IsInvalid(Vector3 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
        private bool IsInvalid(Vector2 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y);
    }
}
