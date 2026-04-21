using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace FigmaImporter.V2.Core
{
    public class FigmaAPIClient
    {
        private readonly string _accessToken;

        public FigmaAPIClient(string accessToken)
        {
            _accessToken = accessToken?.Trim() ?? string.Empty;
        }

        public static void ClearLocalCache()
        {
            // Simple bridge to Parser cache or internal deletion
            FigmaLog.Info("[Figma API] Clearing local image cache...");
            // Real implementation would delete from PersistentDataPath if exists
        }

        public async Task<string> GetFileAsync(string fileId, string nodeId = "", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(fileId)) return null;
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}";
            if (!string.IsNullOrEmpty(nodeId)) url += $"/nodes?ids={Uri.EscapeDataString(nodeId.Trim().Replace("-", ":"))}";
            return await ExecuteRequest(url, ct);
        }

        public async Task<Dictionary<string, string>> GetImageLinksAsync(string fileId, List<string> nodeIds, float scale = 1f, string format = "png", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(fileId) || nodeIds == null || nodeIds.Count == 0) return null;
            
            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/images/{fileId.Trim()}?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}&format={format}&scale={scale}";
            
            string content = await ExecuteRequest(url, ct);
            if (string.IsNullOrEmpty(content)) return null;

            var response = JsonConvert.DeserializeObject<FigmaImageResponse>(content);
            return response?.images;
        }

        private async Task<string> ExecuteRequest(string url, CancellationToken ct)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(_accessToken)) request.SetRequestHeader("X-Figma-Token", _accessToken);
                request.SetRequestHeader("User-Agent", "Unity-Figma-Importer/2.5.7");
                
                var op = request.SendWebRequest();
                while (!op.isDone) { if (ct.IsCancellationRequested) { request.Abort(); throw new OperationCanceledException(); } await Task.Yield(); }

                if (request.result == UnityWebRequest.Result.Success) return request.downloadHandler.text;
                return null;
            }
        }

        [Serializable] private class FigmaImageResponse { public Dictionary<string, string> images; }
    }
}
