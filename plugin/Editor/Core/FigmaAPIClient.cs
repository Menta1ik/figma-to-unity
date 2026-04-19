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
            if (!string.IsNullOrEmpty(_accessToken))
            {
                Debug.Log($"<b>[Figma Debug]</b> API Client (UnityWebRequest) initialized. Token length: {_accessToken.Length}");
            }
        }

        public async Task<string> GetFileAsync(string fileId, string nodeId = "", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(fileId)) return null;
            
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
            if (string.IsNullOrEmpty(fileId) || nodeIds == null) return null;
            
            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}/nodes?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}";

            return await ExecuteRequest(url, ct);
        }

        public async Task<Dictionary<string, string>> GetImageLinksAsync(string fileId, List<string> nodeIds, float scale = 1f, string format = "png", CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(fileId) || nodeIds == null || nodeIds.Count == 0) return null;

            const int batchSize = 25;
            var allImages = new Dictionary<string, string>();

            for (int i = 0; i < nodeIds.Count; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();
                
                int currentBatchSize = Math.Min(batchSize, nodeIds.Count - i);
                var batch = nodeIds.GetRange(i, currentBatchSize);
                
                string idsJoined = string.Join(",", batch);
                string url = $"https://api.figma.com/v1/images/{fileId.Trim()}?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}&format={format}";
                if (format == "png") url += $"&scale={scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                Debug.Log($"<color=cyan>[Figma API]</color> Requesting image batch {i / batchSize + 1} ({currentBatchSize} nodes)...");
                
                string content = await ExecuteRequest(url, ct);
                if (string.IsNullOrEmpty(content)) continue;

                try
                {
                    var figmaResponse = JsonConvert.DeserializeObject<FigmaImageResponse>(content);
                    if (figmaResponse?.images != null)
                    {
                        foreach (var kvp in figmaResponse.images)
                        {
                            allImages[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!ct.IsCancellationRequested) Debug.LogError($"[Figma API] JSON Error in batch: {e.Message}");
                }
            }

            return allImages.Count > 0 ? allImages : null;
        }

        private async Task<string> ExecuteRequest(string url, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(url)) return null;
            
            int retryCount = 0;
            int maxRetries = 10;
            int delayMs = 2000;

            while (retryCount < maxRetries)
            {
                ct.ThrowIfCancellationRequested();

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    if (!string.IsNullOrEmpty(_accessToken))
                    {
                        request.SetRequestHeader("X-Figma-Token", _accessToken);
                    }
                    request.SetRequestHeader("Accept", "application/json");
                    request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Unity/2.3.0");

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
                        return (request.downloadHandler != null) ? request.downloadHandler.text : string.Empty;
                    }

                    if (request.responseCode == 429)
                    {
                        Debug.LogWarning($"<color=orange>[Figma 429]</color> Rate limit. Waiting {delayMs / 1000f}s... (Attempt {retryCount + 1}/{maxRetries})");
                        await Task.Delay(delayMs, ct);
                        retryCount++;
                        delayMs *= 2;
                        continue;
                    }

                    string errorMsg = "Unknown Error";
                    try { errorMsg = request.error; } catch {}
                    
                    string errorContent = "No content";
                    try { if (request.downloadHandler != null) errorContent = request.downloadHandler.text; } catch {}

                    Debug.LogError($"[Figma API Error] {request.responseCode}: {errorMsg}\nContent: {errorContent}");
                    return null;
                }
            }

            Debug.LogError("[Figma API] Max retries reached.");
            return null;
        }

        [Serializable] private class FigmaImageResponse { public Dictionary<string, string> images; }
    }
}
