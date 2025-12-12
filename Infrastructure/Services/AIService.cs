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
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string? _apiKey;
        private readonly string _model;
        private readonly int _maxTokens;

        public AIService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            var baseUrl = configuration["AIServer:BaseUrl"] ?? "http://localhost:8000";
            var endpoint = configuration["AIServer:ChatEndpoint"] ?? "/chat/completions";
            _apiUrl = baseUrl.TrimEnd('/') + endpoint;
            _apiKey = configuration["AIServer:ApiKey"];
            _model = configuration["AIServer:Model"] ?? "gpt-3.5-turbo";
            _maxTokens = int.Parse(configuration["AIServer:MaxTokens"] ?? "500");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<string> GenerateAnswerAsync(string question, string context, CancellationToken ct = default)
        {
            try
            {
                var systemPrompt = @"Bạn là một trợ lý AI thông minh, chuyên trả lời câu hỏi dựa trên ngữ cảnh được cung cấp. 
Hãy trả lời câu hỏi một cách chính xác, ngắn gọn và dễ hiểu. 
Nếu ngữ cảnh không đủ để trả lời, hãy nói rõ điều đó.";

                var userPrompt = $@"Ngữ cảnh:
{context}

Câu hỏi: {question}

Hãy trả lời câu hỏi dựa trên ngữ cảnh trên:";

                var request = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = _maxTokens,
                    temperature = 0.7
                };

                var response = await _httpClient.PostAsJsonAsync(_apiUrl, request, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AIResponse>(cancellationToken: ct);
                
                if (result?.Choices == null || result.Choices.Length == 0)
                {
                    throw new Exception("Empty response from AI service");
                }

                return result.Choices[0].Message.Content.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling AI service: {ex.Message}");
                throw;
            }
        }

        private class AIResponse
        {
            public Choice[]? Choices { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string Content { get; set; } = string.Empty;
        }
    }
}

