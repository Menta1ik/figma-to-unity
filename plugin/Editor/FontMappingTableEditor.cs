using UnityEditor;
using UnityEngine;
using FigmaImporter.V2;
using System.Collections.Generic;
using TMPro;
using System.Linq;

[CustomEditor(typeof(FontMappingTable))]
[CanEditMultipleObjects]
public class FontMappingTableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Guard: EditorStyles may not be ready during domain reload
        try { _ = EditorStyles.boldLabel; }
        catch { Repaint(); return; }

        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🪐 Typography Sync Config", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("Specify how to replace fonts from Figma.\n" +
                                 "Use the button below to automatically search for assets in the project.", MessageType.Info);

        if (GUILayout.Button("🔍 AUTO-SCAN PROJECT", GUILayout.Height(30)))
        {
            Undo.RecordObject(target, "Auto Scan Fonts");
            AutoScanFonts();
            serializedObject.Update(); // Important: pull new data into the inspector
        }

        EditorGUILayout.Space(10);
        
        // Global Fallback
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Fallback Font", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("GlobalFallbackFont"), new GUIContent("Global Fallback"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        
        var mappingsProp = serializedObject.FindProperty("Mappings");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Mapping List ({mappingsProp.arraySize})", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add New", GUILayout.Width(100)))
        {
            mappingsProp.InsertArrayElementAtIndex(mappingsProp.arraySize);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Устанавливаем ширину лейблов для всего списка
        EditorGUIUtility.labelWidth = 150f;

        for (int i = 0; i < mappingsProp.arraySize; i++)
        {
            var element = mappingsProp.GetArrayElementAtIndex(i);
            
            // Сбрасываем отступ, чтобы не было "лестницы"
            EditorGUI.indentLevel = 0;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Заголовок элемента
            EditorGUILayout.BeginHorizontal();
            var assetProp = element.FindPropertyRelative("targetTMPAsset");
            string title = assetProp.objectReferenceValue != null ? 
                $"[{i}] {assetProp.objectReferenceValue.name}" : $"[{i}] New Mapping";
            
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                mappingsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break; 
            }
            EditorGUILayout.EndHorizontal();

            // Поля ввода без дополнительного отступа
            EditorGUILayout.PropertyField(element.FindPropertyRelative("fontPostScriptName"), new GUIContent("PostScript Name (Optional)"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("figmaFontFamily"), new GUIContent("Figma Font Family"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("figmaFontWeight"), new GUIContent("Figma Font Weight (0=Any)"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(assetProp, new GUIContent("→ Unity TMP Asset"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AutoScanFonts()
    {
        var table = (FontMappingTable)target;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        
        Debug.Log($"[FigmaImporter] Starting scan. Found {guids.Length} potential TMP assets in database.");
        
        int addedCount = 0;
        int updatedCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            
            if (fontAsset == null) continue;

            // Ищем, есть ли уже такой ассет в таблице
            var existingMapping = table.Mappings.FirstOrDefault(m => m.targetTMPAsset == fontAsset);
            
            if (existingMapping == null)
            {
                // Если нет — создаем новый
                existingMapping = new FontMapping { targetTMPAsset = fontAsset };
                table.Mappings.Add(existingMapping);
                addedCount++;
            }
            else
            {
                updatedCount++;
            }

            // ЗАПОЛНЯЕМ/ОБНОВЛЯЕМ ПОЛЯ
            string name = fontAsset.name;
            
            if (string.IsNullOrEmpty(existingMapping.fontPostScriptName))
                existingMapping.fontPostScriptName = name.Replace(" SDF", "");

            // Принудительно обновляем вес, чтобы исправить ошибки прошлого сканирования
            existingMapping.figmaFontWeight = GuessWeight(name);

            if (string.IsNullOrEmpty(existingMapping.figmaFontFamily))
            {
                string family = name.Replace(" SDF", "");
                if (name.Contains("-")) family = name.Split('-')[0];
                else if (name.Contains(" ")) family = name.Split(' ')[0];

                // Save "clean" family name as primary identifier
                existingMapping.figmaFontFamily = family; 
            }
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FigmaImporter] Scan completed. Added new: {addedCount}, Updated existing: {updatedCount}.");
    }

    private int GuessWeight(string name)
    {
        name = name.ToLower().Replace(" ", "").Replace("-", "");
        
        if (name.Contains("thin") || name.Contains("hairline")) return 100;
        if (name.Contains("extralight") || name.Contains("ultralight")) return 200;
        if (name.Contains("light")) return 300;
        if (name.Contains("medium")) return 500;
        if (name.Contains("semibold") || name.Contains("demibold") || name.Contains("demi")) return 600;
        if (name.Contains("extrabold") || name.Contains("ultrabold")) return 800; // Должно быть перед "bold"
        if (name.Contains("bold")) return 700;
        if (name.Contains("black") || name.Contains("heavy")) return 900;
        
        return 400; // Regular
    }
}
