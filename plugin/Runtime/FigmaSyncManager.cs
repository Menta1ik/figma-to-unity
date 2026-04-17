using UnityEngine;

namespace FigmaImporter.V2
{
    /// <summary>
    /// Компонент для управления синхронизацией конкретного экрана.
    /// Позволяет хранить настройки импорта прямо на игровом объекте.
    /// </summary>
    public class FigmaSyncManager : MonoBehaviour
    {
        [Header("Figma Connection")]
        public string AccessToken = "";
        public string FileId = "VTzGVHnsRpELqG3pYTFE3M";
        public string NodeId = "1:16556";
        
        [Header("Sync Settings")]
        [Tooltip("Если включено, плагин будет искать локальный JSON файл вместо запроса к API.")]
        public bool UseLocalJson = true;
        public string LocalJsonPath = "lobby_figma.json";

        [Header("Typography")]
        public FontMappingTable FontMapping;

        [Space]
        [TextArea(3, 5)]
        public string StatusInfo = "Готов к синхронизации.";

        public void UpdateStatus(string message)
        {
            StatusInfo = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
        }
    }
}
