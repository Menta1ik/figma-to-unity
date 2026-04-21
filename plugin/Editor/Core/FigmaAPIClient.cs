using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FigmaImporter.V2.Core
{
    public class FigmaAPIClient
    {
        private readonly string _accessToken;
        private static readonly HttpClient _httpClient = new HttpClient();

        public FigmaAPIClient(string accessToken)
        {
            _accessToken = accessToken;
            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Unity-Figma-Importer/2.5.5");
            }
        }

        public async Task<string> GetFileAsync(string fileKey, string nodeId = "", CancellationToken ct = default, float scale = 1.0f)
        {
            string url = $"https://api.figma.com/v1/files/{fileKey}";
            if (!string.IsNullOrEmpty(nodeId)) url += $"?ids={nodeId}";
            
            // Add scale if specified
            if (scale != 1.0f)
            {
                url += (url.Contains("?") ? "&" : "?") + $"scale={scale.ToString("F1")}";
            }

            return await SendRequestAsync(url, ct);
        }

        public async Task<string> GetImageNodesAsync(string fileKey, string ids, CancellationToken ct = default, float scale = 1.0f)
        {
            string url = $"https://api.figma.com/v1/images/{fileKey}?ids={ids}&format=png";
            if (scale != 1.0f) url += $"&scale={scale.ToString("F1")}";
            
            return await SendRequestAsync(url, ct);
        }

        private async Task<string> SendRequestAsync(string url, CancellationToken ct)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("X-Figma-Token", _accessToken);
                var response = await _httpClient.SendAsync(request, ct);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                string errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"Figma API Error ({response.StatusCode}): {errorText}");
            }
        }
    }
}
