using UnityEngine;

namespace FigmaImporter.V2
{
    public enum FigmaLogLevel { Silent, Minimal, Verbose }

    public static class FigmaLog
    {
        private static FigmaLogLevel _level = FigmaLogLevel.Minimal;

        public static void SetLevel(FigmaLogLevel level) => _level = level;

        public static void Info(string message)
        {
            if (_level >= FigmaLogLevel.Minimal)
                Debug.Log(message);
        }

        public static void Verbose(string message)
        {
            if (_level >= FigmaLogLevel.Verbose)
                Debug.Log(message);
        }

        public static void Warning(string message)
        {
            if (_level >= FigmaLogLevel.Minimal)
                Debug.LogWarning(message);
        }

        public static void Error(string message)
        {
            Debug.LogError(message);
        }
    }
}
