using UnityEngine;
using UnityEditor;
using System.IO;
using FigmaImporter.V2.Core;

namespace FigmaImporter.V2.UI
{
    [CustomEditor(typeof(FigmaSyncManager))]
    public class FigmaSyncManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // NEW ULTIMATE GUARD: EditorStyles may throw NullReferenceException during domain reload if Unity is unstable.
            // We catch EVERYTHING here and just skip drawing this frame, requesting a repaint.
            try 
            {
                _ = EditorStyles.boldLabel;
            }
            catch (System.Exception)
            {
                Repaint();
                return;
            }

            try 
            {
                base.OnInspectorGUI();

            FigmaSyncManager manager = (FigmaSyncManager)target;

            EditorGUILayout.Space();
            GUILayout.Label("Запуск импорта", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);

            if (GUILayout.Button("🚀 СОБРАТЬ UI И СКАЧАТЬ КАРТИНКИ", GUILayout.Height(50)))
            {
                EditorApplication.delayCall += () => RunSync(manager);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space();

            if (GUILayout.Button("🧹 Очистить дочерние объекты (Сбросить кеш)"))
            {
                EditorApplication.delayCall += () => ClearChildren(manager.transform);
            }
            }
            catch (System.Exception)
            {
                Repaint();
            }
        }

        private async void RunSync(FigmaSyncManager manager)
        {
            if (string.IsNullOrEmpty(manager.AccessToken))
            {
                EditorUtility.DisplayDialog("Ошибка", "Введите Access Token в инспекторе!", "OK");
                return;
            }

            // ПЕРЕХОД НА НОВЫЙ КОНСТРУКТОР (Guardrails внутри)
            FigmaParser parser = new FigmaParser(manager.AccessToken, manager.FileId)
            {
                FontMapTable = manager.FontMapping
            };
            string jsonContent = "";

            manager.UpdateStatus("Загрузка данных...");
            EditorUtility.SetDirty(manager);

            if (manager.UseLocalJson)
            {
                string path = Path.Combine(Application.dataPath, manager.LocalJsonPath);
                if (File.Exists(path))
                {
                    jsonContent = File.ReadAllText(path);
                }
                else
                {
                    Debug.LogError($"[Figma v2.3.1] Файл не найден: {path}");
                    manager.UpdateStatus("Ошибка: Файл не найден.");
                    return;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Внимание", "API загрузка JSON еще не реализована. Включите Use Local Json.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Figma Smart Sync");
            
            try 
            {
                await parser.ProcessFileAsync(jsonContent, manager.transform, (current, total, nodeName) => {
                    float progress = (float)current / total;
                    EditorUtility.DisplayProgressBar("Figma Sync", $"Обработка: {current}/{total} ({nodeName})", progress);
                });

                manager.UpdateStatus("Синхронизация завершена!");
                Debug.Log("<color=green>[Figma v2.3.1]</color> Успешно!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Figma v2.3.1] Ошибка: {e.Message}");
                manager.UpdateStatus("Ошибка синхронизации.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.SetDirty(manager);
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        private void ClearChildren(Transform parent)
        {
            Undo.RegisterFullObjectHierarchyUndo(parent.gameObject, "Clear Children");
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
            }
            Debug.Log("[Figma v2.3.1] Дочерние объекты очищены!");
        }
    }
}
