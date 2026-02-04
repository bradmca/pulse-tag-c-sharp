using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PulseTag.Shared.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PulseTag.Api.Services;

public interface IAIEngine
{
    Task<HashtagDictionary> AnalyzePostAsync(string textContent, CancellationToken cancellationToken = default);
}

public class AIEngine : IAIEngine
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<AIEngine> _logger;
    private readonly string _systemPrompt;
    private readonly string _apiKey;

    public AIEngine(IConfiguration configuration, ILogger<AIEngine> logger)
    {
        _apiKey = configuration["OpenRouter:ApiKey"] ?? 
            throw new InvalidOperationException("OpenRouter:ApiKey configuration is required");

        _model = configuration["OpenRouter:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free";
        _logger = logger;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/")
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://pulsetag.local");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "PulseTag");

        _systemPrompt = """
            You are a Viral Social Media Strategist. You analyse social media posts to maximise reach.
            **Input:** The text content of a user's post.
            **Task:** Analyse the core topics, tone, and industry.
            **Output:** Return ONLY a JSON object with three arrays of hashtags:
            1. 'safe': High-volume, broad tags (e.g., #Marketing, #Tech). Use these for baseline visibility.
            2. 'rising': Trending, mid-volume tags relevant *right now* or to specific modern sub-cultures (e.g., #GenAI, #GrowthHacking).
            3. 'niche': Specific, low-competition tags that target high-intent users (e.g., #SaaSMarketingTips).

            **Rules:**
            * Do not include the # symbol in the string, just the word.
            * Ensure tags are CamelCase (e.g., 'DigitalMarketing', not 'digitalmarketing').
            * Do not return any conversational text, only the JSON.
            """;
    }

    public async Task<HashtagDictionary> AnalyzePostAsync(string textContent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Sanitize input to prevent prompt injection
            var sanitizedContent = SanitizeInput(textContent);

            var requestBody = new OpenRouterRequest
            {
                Model = _model,
                Messages = new[]
                {
                    new Message { Role = "user", Content = $"{_systemPrompt}\n\nAnalyze this post:\n\n{sanitizedContent}" }
                },
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            
            _logger.LogInformation("Calling OpenRouter API with model: {Model}", _model);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenRouter API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return GetFallbackHashtags();
            }

            _logger.LogInformation("OpenRouter API response received successfully");
            
            using var docResponse = JsonDocument.Parse(responseContent);
            var aiContent = docResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(aiContent))
            {
                _logger.LogWarning("AI returned empty content");
                return GetFallbackHashtags();
            }

            _logger.LogInformation("AI response content: {Content}", aiContent);

            // Parse JSON response
            try
            {
                var hashtags = JsonSerializer.Deserialize<HashtagDictionary>(aiContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (hashtags == null)
                {
                    throw new JsonException("Failed to deserialize response");
                }

                // Ensure all values are lists
                hashtags.Safe ??= new List<string>();
                hashtags.Rising ??= new List<string>();
                hashtags.Niche ??= new List<string>();

                return hashtags;
            }
            catch (JsonException)
            {
                // Fallback: try to extract JSON from response
                var jsonMatch = Regex.Match(aiContent, @"\{.*\}", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    var hashtags = JsonSerializer.Deserialize<HashtagDictionary>(jsonMatch.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return hashtags ?? GetFallbackHashtags();
                }
                else
                {
                    _logger.LogError("Could not parse AI response: {Content}", aiContent);
                    return GetFallbackHashtags();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing post. Exception type: {ExceptionType}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
            
            // Log inner exception if present
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
            
            return GetFallbackHashtags();
        }
    }

    private static string SanitizeInput(string text)
    {
        // Remove potential prompt injection patterns
        text = Regex.Replace(text, @"(?i)system\s*:", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?i)assistant\s*:", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?i)user\s*:", "", RegexOptions.IgnoreCase);
        
        // Limit length to prevent token overflow
        return text.Length > 5000 ? text[..5000] : text;
    }

    private static HashtagDictionary GetFallbackHashtags()
    {
        return new HashtagDictionary
        {
            Safe = new List<string> { "SocialMedia", "Marketing" },
            Rising = new List<string> { "DigitalTrends" },
            Niche = new List<string> { "ContentStrategy" }
        };
    }
    
    private class OpenRouterRequest
    {
        public string Model { get; set; } = "";
        public Message[] Messages { get; set; } = Array.Empty<Message>();
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
    }
    
    private class Message
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
