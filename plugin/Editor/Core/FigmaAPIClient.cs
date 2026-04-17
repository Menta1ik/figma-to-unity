using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace FigmaImporter.V2.Core
{
    public class FigmaAPIClient
    {
        private readonly string _accessToken;
        private readonly HttpClient _httpClient;

        public FigmaAPIClient(string accessToken)
        {
            _accessToken = accessToken?.Trim();
            if (_accessToken != null) Debug.Log($"<b>[Figma Debug]</b> Token initialized. Length: {_accessToken.Length} chars.");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Figma-Token", _accessToken);
        }

        public async Task<string> GetFileAsync(string fileId, string nodeId = "")
        {
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}";
            if (!string.IsNullOrEmpty(nodeId))
            {
                url += $"/nodes?ids={Uri.EscapeDataString(nodeId.Trim().Replace("-", ":"))}";
            }

            Debug.Log($"<b>[Figma Debug]</b> Requesting URL: <color=white>{url}</color>");
            return await ExecuteWithRetry(() => _httpClient.GetAsync(url));
        }

        public async Task<string> GetFileNodesAsync(string fileId, List<string> nodeIds)
        {
            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}/nodes?ids={Uri.EscapeDataString(idsJoined)}";

            return await ExecuteWithRetry(() => _httpClient.GetAsync(url));
        }

        public async Task<Dictionary<string, string>> GetImageLinksAsync(string fileId, List<string> nodeIds, float scale = 1f, string format = "png")
        {
            if (nodeIds == null || nodeIds.Count == 0) return null;

            string idsJoined = string.Join(",", nodeIds);
            string escapedIds = Uri.EscapeDataString(idsJoined);
            
            string url = $"https://api.figma.com/v1/images/{fileId.Trim()}?ids={escapedIds}&format={format}";
            if (format == "png") url += $"&scale={scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            string content = await ExecuteWithRetry(() => _httpClient.GetAsync(url));
            if (string.IsNullOrEmpty(content)) return null;

            try
            {
                var figmaResponse = JsonConvert.DeserializeObject<FigmaImageResponse>(content);
                return figmaResponse?.images;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Figma v2.1] JSON Error: {e.Message}");
                return null;
            }
        }

        private async Task<string> ExecuteWithRetry(Func<Task<HttpResponseMessage>> call, int maxRetries = 10)
        {
            int retryCount = 0;
            int delayMs = 2000; // Start with 2 seconds

            while (retryCount < maxRetries)
            {
                try
                {
                    var response = await call();
                    string content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return content;
                    }

                    if ((int)response.StatusCode == 429)
                    {
                        Debug.LogWarning($"<color=orange>[Figma v2.1] Rate limit hit (429).</color> Figma is busy. Waiting <b>{delayMs/1000f}s</b> before retry {retryCount + 1}/{maxRetries}...");
                        await Task.Delay(delayMs);
                        retryCount++;
                        delayMs *= 2; // Exponential backoff: 2s, 4s, 8s, 16s...
                        continue;
                    }

                    Debug.LogError($"[Figma v2.1] API Error ({response.StatusCode}): {content}");
                    return null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Figma v2.1] Exception during API call: {e.Message}");
                    return null;
                }
            }

            Debug.LogError("[Figma v2.1] Max retries reached for API call.");
            return null;
        }

        [Serializable]
        private class FigmaImageResponse
        {
            public Dictionary<string, string> images;
            public string err;
        }
    }
}
