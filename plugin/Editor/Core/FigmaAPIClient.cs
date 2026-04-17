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
            _accessToken = accessToken?.Trim();
            if (!string.IsNullOrEmpty(_accessToken))
            {
                Debug.Log($"<b>[Figma Debug]</b> API Client (UnityWebRequest) initialized. Token length: {_accessToken.Length}");
            }
        }

        public async Task<string> GetFileAsync(string fileId, string nodeId = "", CancellationToken ct = default)
        {
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}";
            if (!string.IsNullOrEmpty(nodeId))
            {
                url += $"/nodes?ids={Uri.EscapeDataString(nodeId.Trim().Replace("-", ":"))}";
            }

            Debug.Log($"<b>[Figma Debug]</b> Requesting URL: <color=white>{url}</color>");
            return await ExecuteRequest(url, ct);
        }

        public async Task<string> GetFileNodesAsync(string fileId, List<string> nodeIds, CancellationToken ct = default)
        {
            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}/nodes?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}";

            return await ExecuteRequest(url, ct);
        }

        public async Task<Dictionary<string, string>> GetImageLinksAsync(string fileId, List<string> nodeIds, float scale = 1f, string format = "png", CancellationToken ct = default)
        {
            if (nodeIds == null || nodeIds.Count == 0) return null;

            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/images/{fileId.Trim()}?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}&format={format}";
            if (format == "png") url += $"&scale={scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            string content = await ExecuteRequest(url, ct);
            if (string.IsNullOrEmpty(content)) return null;

            try
            {
                var figmaResponse = JsonConvert.DeserializeObject<FigmaImageResponse>(content);
                return figmaResponse?.images;
            }
            catch (Exception e)
            {
                if (!ct.IsCancellationRequested) Debug.LogError($"[Figma API] JSON Error: {e.Message}");
                return null;
            }
        }

        private async Task<string> ExecuteRequest(string url, CancellationToken ct)
        {
            int retryCount = 0;
            int maxRetries = 10;
            int delayMs = 2000;

            while (retryCount < maxRetries)
            {
                ct.ThrowIfCancellationRequested();

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("X-Figma-Token", _accessToken);
                    request.SetRequestHeader("Accept", "application/json");
                    request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Unity/2.1");

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            request.Abort();
                            throw new OperationCanceledException();
                        }
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return request.downloadHandler.text;
                    }

                    if (request.responseCode == 429)
                    {
                        Debug.LogWarning($"<color=orange>[Figma 429]</color> Rate limit. Waiting {delayMs / 1000f}s... (Attempt {retryCount + 1}/{maxRetries})");
                        await Task.Delay(delayMs, ct);
                        retryCount++;
                        delayMs *= 2;
                        continue;
                    }

                    Debug.LogError($"[Figma API Error] {request.responseCode}: {request.error}\nContent: {request.downloadHandler.text}");
                    return null;
                }
            }

            Debug.LogError("[Figma API] Max retries reached.");
            return null;
        }

        [Serializable] private class FigmaImageResponse { public Dictionary<string, string> images; }
    }
}
