using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonHall.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

public sealed class AnthropicAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicAiService> _logger;
    private readonly string _defaultModel;
    private readonly JsonSerializerOptions _jsonOptions;

    public AnthropicAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnthropicAiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _defaultModel = configuration["AI:Anthropic:Model"] ?? "claude-sonnet-4-5-20250929";

        var apiKey = configuration["AI:Anthropic:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        AiGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AiGenerationOptions();

        var request = new AnthropicRequest
        {
            Model = options.Model ?? _defaultModel,
            MaxTokens = options.MaxTokens,
            System = systemPrompt,
            Messages = new[]
            {
                new AnthropicMessage { Role = "user", Content = userPrompt }
            }
        };

        var response = await SendRequestAsync<AnthropicResponse>(request, cancellationToken);

        return response?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemPrompt,
        string userPrompt,
        AiGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new AiGenerationOptions();

        var request = new AnthropicRequest
        {
            Model = options.Model ?? _defaultModel,
            MaxTokens = options.MaxTokens,
            System = systemPrompt,
            Messages = new[]
            {
                new AnthropicMessage { Role = "user", Content = userPrompt }
            },
            Stream = true
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var eventData = line[6..];

            if (eventData == "[DONE]")
                break;

            try
            {
                using var doc = JsonDocument.Parse(eventData);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeVal) &&
                    typeVal.GetString() == "content_block_delta" &&
                    root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var text))
                {
                    var textValue = text.GetString();
                    if (!string.IsNullOrEmpty(textValue))
                    {
                        yield return textValue;
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed JSON
            }
        }
    }

    public async Task<ClassificationResult> ClassifyAsync(
        string text,
        string[] categories,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = $@"You are a text classifier. Classify the following text into one of these categories: {string.Join(", ", categories)}.

Respond in JSON format:
{{
  ""category"": ""the_category"",
  ""confidence"": 0.0-1.0,
  ""reasoning"": ""brief explanation""
}}

Only respond with valid JSON, no other text.";

        var response = await GenerateAsync(
            systemPrompt,
            $"Classify this text:\n\n{text}",
            new AiGenerationOptions { MaxTokens = 256, Temperature = 0.3 },
            cancellationToken);

        return ParseClassificationResult(response, categories);
    }

    public async Task<List<ClassificationResult>> ClassifyBatchAsync(
        string[] texts,
        string[] categories,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = $@"You are a text classifier. For each numbered text, classify it into one of these categories: {string.Join(", ", categories)}.

Respond in JSON array format:
[
  {{ ""index"": 0, ""category"": ""the_category"", ""confidence"": 0.0-1.0 }},
  ...
]

Only respond with valid JSON, no other text.";

        var numberedTexts = texts.Select((t, i) => $"[{i}] {t}").ToArray();
        var userPrompt = $"Classify these texts:\n\n{string.Join("\n\n", numberedTexts)}";

        var response = await GenerateAsync(
            systemPrompt,
            userPrompt,
            new AiGenerationOptions { MaxTokens = 1024, Temperature = 0.3 },
            cancellationToken);

        return ParseBatchClassificationResults(response, texts.Length, categories);
    }

    private async Task<T?> SendRequestAsync<T>(AnthropicRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("v1/messages", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Anthropic API error: {Error}", error);
            throw new HttpRequestException($"Anthropic API error: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
    }

    private static ClassificationResult ParseClassificationResult(string json, string[] validCategories)
    {
        try
        {
            // Clean up the response (remove markdown code blocks if present)
            json = json.Trim();
            if (json.StartsWith("```"))
            {
                json = json.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")).Aggregate((a, b) => $"{a}\n{b}");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var category = root.GetProperty("category").GetString() ?? validCategories[0];
            var confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5;
            var reasoning = root.TryGetProperty("reasoning", out var reason) ? reason.GetString() : null;

            // Validate category
            if (!validCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                category = validCategories[0];
                confidence = 0.3;
            }

            return new ClassificationResult
            {
                Category = category,
                Confidence = Math.Clamp(confidence, 0, 1),
                Reasoning = reasoning
            };
        }
        catch (Exception)
        {
            return new ClassificationResult
            {
                Category = validCategories[0],
                Confidence = 0.3,
                Reasoning = "Failed to parse classification result"
            };
        }
    }

    private static List<ClassificationResult> ParseBatchClassificationResults(
        string json, int expectedCount, string[] validCategories)
    {
        var results = new List<ClassificationResult>();

        try
        {
            json = json.Trim();
            if (json.StartsWith("```"))
            {
                json = json.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")).Aggregate((a, b) => $"{a}\n{b}");
            }

            using var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var category = item.TryGetProperty("category", out var cat) ? cat.GetString() ?? validCategories[0] : validCategories[0];
                var confidence = item.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5;

                if (!validCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
                {
                    category = validCategories[0];
                    confidence = 0.3;
                }

                results.Add(new ClassificationResult
                {
                    Category = category,
                    Confidence = Math.Clamp(confidence, 0, 1)
                });
            }
        }
        catch (Exception)
        {
            // Return default results on parse failure
        }

        // Pad with defaults if needed
        while (results.Count < expectedCount)
        {
            results.Add(new ClassificationResult
            {
                Category = validCategories[0],
                Confidence = 0.3
            });
        }

        return results;
    }

    private record AnthropicRequest
    {
        public required string Model { get; init; }
        public int MaxTokens { get; init; } = 1024;
        public string? System { get; init; }
        public required AnthropicMessage[] Messages { get; init; }
        public bool? Stream { get; init; }
    }

    private record AnthropicMessage
    {
        public required string Role { get; init; }
        public required string Content { get; init; }
    }

    private record AnthropicResponse
    {
        public AnthropicContent[]? Content { get; init; }
    }

    private record AnthropicContent
    {
        public string? Type { get; init; }
        public string? Text { get; init; }
    }
}
