using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string? _apiKey;
        private readonly int _vectorSize;

        public EmbeddingService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            var baseUrl = configuration["AIServer:BaseUrl"] ?? "http://localhost:8000";
            var endpoint = configuration["AIServer:EmbeddingEndpoint"] ?? "/embed";
            _apiUrl = baseUrl.TrimEnd('/') + endpoint;
            _apiKey = configuration["AIServer:ApiKey"];
            _vectorSize = int.Parse(configuration["AIServer:VectorSize"] ?? "384");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
        {
            try
            {
                var request = new
                {
                    text = text
                };

                var response = await _httpClient.PostAsJsonAsync(_apiUrl, request, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
                
                if (result?.Vector == null || result.Vector.Length == 0)
                {
                    throw new Exception("Empty embedding vector received");
                }

                return result.Vector;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating embedding: {ex.Message}");
                throw;
            }
        }

        public int GetVectorSize() => _vectorSize;

        private class EmbeddingResponse
        {
            public float[] Vector { get; set; } = Array.Empty<float>();
        }
    }
}

