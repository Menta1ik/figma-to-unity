using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace FigmaImporter.V2.Core.Services
{
    /// <summary>
    /// File-based cache for Figma API responses.
    /// Stores in Library/FigmaCache/ (Unity convention for temp data, gitignored by default).
    /// </summary>
    public class FigmaResponseCache
    {
        private static readonly string CacheDir = Path.Combine(Application.dataPath, "..", "Library", "FigmaCache");

        private string GetCachePath(string fileId, string nodeId)
        {
            string key = string.IsNullOrEmpty(nodeId) ? fileId : $"{fileId}_{nodeId.Replace(":", "-")}";
            return Path.Combine(CacheDir, $"{key}.json");
        }

        private string GetMetaPath(string fileId, string nodeId)
        {
            return GetCachePath(fileId, nodeId) + ".meta.json";
        }

        /// <summary>
        /// Returns cached JSON if the stored version matches currentVersion, otherwise null.
        /// </summary>
        public string TryLoadCached(string fileId, string nodeId, string currentVersion)
        {
            string metaPath = GetMetaPath(fileId, nodeId);
            string cachePath = GetCachePath(fileId, nodeId);

            if (!File.Exists(metaPath) || !File.Exists(cachePath)) return null;

            try
            {
                var meta = JsonConvert.DeserializeObject<CacheMeta>(File.ReadAllText(metaPath));
                if (meta != null && meta.version == currentVersion)
                {
                    FigmaLog.Info($"{FigmaLog.VersionPrefix}<color=green>Cache HIT</color> for {fileId} (version {currentVersion})");
                    return File.ReadAllText(cachePath);
                }
            }
            catch { /* corrupted meta — treat as miss */ }

            return null;
        }

        /// <summary>
        /// Saves API response to disk with version metadata.
        /// </summary>
        public void SaveToCache(string fileId, string nodeId, string version, string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return;

            Directory.CreateDirectory(CacheDir);

            string cachePath = GetCachePath(fileId, nodeId);
            string metaPath = GetMetaPath(fileId, nodeId);

            File.WriteAllText(cachePath, jsonContent);
            File.WriteAllText(metaPath, JsonConvert.SerializeObject(new CacheMeta { version = version }));

            FigmaLog.Info($"{FigmaLog.VersionPrefix}Cache saved for {fileId} (version {version})");
        }

        /// <summary>
        /// Clears cache for a specific file or all files.
        /// </summary>
        public void ClearCache(string fileId = null)
        {
            if (!Directory.Exists(CacheDir))
            {
                FigmaLog.Info($"{FigmaLog.VersionPrefix}No cache directory found.");
                return;
            }

            if (string.IsNullOrEmpty(fileId))
            {
                Directory.Delete(CacheDir, true);
                FigmaLog.Info($"{FigmaLog.VersionPrefix}All Figma cache cleared.");
            }
            else
            {
                foreach (var file in Directory.GetFiles(CacheDir, $"{fileId}*"))
                    File.Delete(file);
                FigmaLog.Info($"{FigmaLog.VersionPrefix}Cache cleared for {fileId}.");
            }
        }

        private class CacheMeta
        {
            public string version;
        }
    }
}
