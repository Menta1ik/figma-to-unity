using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using FigmaImporter.V2.Core.Services;

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
            new FigmaResponseCache().ClearCache();
        }

        /// <summary>
        /// Lightweight call to get only the file version from Figma API.
        /// Used to check if the cached response is still valid.
        /// </summary>
        public async Task<string> GetFileVersionAsync(string fileId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(fileId)) return null;
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}?fields=version";
            string json = await ExecuteRequest(url, ct);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var obj = JsonConvert.DeserializeObject<FigmaVersionResponse>(json);
                return obj?.version;
            }
            catch { return null; }
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

                string errorMsg = $"[Figma API Error] {request.responseCode} {request.error}\nURL: {url}";
                FigmaLog.Error(errorMsg);
                return null;
            }
        }

        [Serializable] private class FigmaImageResponse { public Dictionary<string, string> images; }
        [Serializable] private class FigmaVersionResponse { public string version; }
    }
}
