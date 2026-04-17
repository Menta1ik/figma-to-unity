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
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Figma-Token", _accessToken);
        }

        public async Task<string> GetFileAsync(string fileId, string nodeId = "")
        {
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}";
            if (!string.IsNullOrEmpty(nodeId))
            {
                url += $"/nodes?ids={Uri.EscapeDataString(nodeId)}";
            }

            try
            {
                var response = await _httpClient.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[Figma v2.1] API Error ({response.StatusCode}): {content}");
                    return null;
                }

                return content;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Figma v2.1] Exception during API call: {e.Message}");
                return null;
            }
        }

        public async Task<string> GetFileNodesAsync(string fileId, List<string> nodeIds)
        {
            string idsJoined = string.Join(",", nodeIds);
            string url = $"https://api.figma.com/v1/files/{fileId.Trim()}/nodes?ids={Uri.EscapeDataString(idsJoined)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[Figma v2.1] API Error ({response.StatusCode}): {content}");
                    return null;
                }

                return content;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Figma v2.1] Exception during API call: {e.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, string>> GetImageLinksAsync(string fileId, List<string> nodeIds, float scale = 1f, string format = "png")
        {
            if (nodeIds == null || nodeIds.Count == 0) return null;

            string idsJoined = string.Join(",", nodeIds);
            // Important: encode IDs as they contain symbols ':' and ';'
            string escapedIds = Uri.EscapeDataString(idsJoined);
            
            // Append format to URL
            string url = $"https://api.figma.com/v1/images/{fileId.Trim()}?ids={escapedIds}&format={format}";
            
            // Figma API ignores scale for SVG, but we need it for PNG
            if (format == "png") url += $"&scale={scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[Figma v2.1] API Error ({response.StatusCode}): {content}");
                    return null;
                }

                var figmaResponse = JsonConvert.DeserializeObject<FigmaImageResponse>(content);
                return figmaResponse?.images;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Figma v2.1] Exception during API call: {e.Message}");
                return null;
            }
        }

        [Serializable]
        private class FigmaImageResponse
        {
            public Dictionary<string, string> images;
            public string err;
        }
    }
}
