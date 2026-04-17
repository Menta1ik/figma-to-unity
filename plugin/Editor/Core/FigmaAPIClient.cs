using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace FigmaImporter.V2.Core
{
    public class FigmaAPIClient
    {
        private readonly string _accessToken;
        private static readonly HttpClient _httpClient = new HttpClient();

        public FigmaAPIClient(string accessToken)
        {
            _accessToken = accessToken?.Trim();
            
            // Настройка заголовков (делаем один раз для статического клиента)
            if (!_httpClient.DefaultRequestHeaders.Contains("X-Figma-Token"))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Figma-Token", _accessToken);
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Unity-Figma-Importer/2.1");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            else 
            {
                // Если токен изменился, обновляем заголовок
                _httpClient.DefaultRequestHeaders.Remove("X-Figma-Token");
                _httpClient.DefaultRequestHeaders.Add("X-Figma-Token", _accessToken);
            }

            if (!string.IsNullOrEmpty(_accessToken))
            {
                Debug.Log($"<b>[Figma Debug]</b> API Client initialized. Token length: {_accessToken.Length}");
            }
        }

        public async Task<string> GetFileAsync(string fileId, string nodeId = "", CancellationToken ct = default)
        {
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}";
            if (!string.IsNullOrEmpty(nodeId))
            {
                url += $"/nodes?ids={Uri.EscapeDataString(nodeId.Trim().Replace("-", ":"))}";
            }

            Debug.Log($"<b>[Figma Debug]</b> Requesting: <color=white>{url}</color>");
            return await ExecuteWithRetry(() => _httpClient.GetAsync(url, ct), 10, ct);
        }

        public async Task<string> GetFileNodesAsync(string fileId, List<string> nodeIds, CancellationToken ct = default)
        {
            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}/nodes?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}";

            return await ExecuteWithRetry(() => _httpClient.GetAsync(url, ct), 10, ct);
        }

        public async Task<Dictionary<string, string>> GetImageLinksAsync(string fileId, List<string> nodeIds, float scale = 1f, string format = "png", CancellationToken ct = default)
        {
            if (nodeIds == null || nodeIds.Count == 0) return null;

            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/images/{fileId.Trim()}?ids={Uri.EscapeDataString(idsJoined.Replace("-", ":"))}&format={format}";
            if (format == "png") url += $"&scale={scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            string content = await ExecuteWithRetry(() => _httpClient.GetAsync(url, ct), 10, ct);
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

        private async Task<string> ExecuteWithRetry(Func<Task<HttpResponseMessage>> call, int maxRetries, CancellationToken ct)
        {
            int retryCount = 0;
            int delayMs = 2000;

            while (retryCount < maxRetries)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var response = await call();
                    string content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode) return content;

                    if ((int)response.StatusCode == 429)
                    {
                        Debug.LogWarning($"<color=orange>[Figma 429]</color> Too many requests. Waiting {delayMs / 1000f}s... (Attempt {retryCount + 1}/{maxRetries})");
                        await Task.Delay(delayMs, ct);
                        retryCount++;
                        delayMs *= 2;
                        continue;
                    }

                    Debug.LogError($"[Figma API Error] {response.StatusCode}: {content}");
                    return null;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    Debug.LogError($"[Figma API Exception] {e.Message}");
                    return null;
                }
            }

            Debug.LogError("[Figma API] Max retries reached. Operation aborted.");
            return null;
        }

        [Serializable] private class FigmaImageResponse { public Dictionary<string, string> images; }
    }
}
