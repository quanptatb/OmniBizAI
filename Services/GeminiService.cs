using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace OmniBizAI.Services;

/// <summary>
/// Configuration for the Gemini AI provider, bound from appsettings.json.
/// </summary>
public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash-lite";
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

/// <summary>
/// Lightweight Gemini REST API client using HttpClient.
/// Sends a system prompt + user prompt and returns structured text.
/// </summary>
public class GeminiService
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    /// <summary>
    /// Call Gemini generateContent API with system instruction + user message.
    /// </summary>
    public async Task<GeminiResponse> GenerateAsync(string systemPrompt, string userPrompt, double temperature = 0.3, int maxOutputTokens = 4096)
    {
        if (!IsConfigured)
            return new GeminiResponse { Success = false, ErrorMessage = "Gemini API Key chưa được cấu hình. Vui lòng thiết lập trong appsettings.json → Gemini:ApiKey." };

        var url = $"{_options.Endpoint}/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { parts = new[] { new { text = userPrompt } } } },
            generationConfig = new
            {
                temperature,
                maxOutputTokens,
                topP = 0.95,
                topK = 40
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API returned {StatusCode}: {Body}", response.StatusCode, responseBody[..Math.Min(500, responseBody.Length)]);
                return new GeminiResponse { Success = false, ErrorMessage = $"Gemini API lỗi ({response.StatusCode}). Vui lòng thử lại." };
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Extract text from candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentEl) &&
                    contentEl.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString() ?? "";

                    // Extract usage metadata
                    int inputTokens = 0, outputTokens = 0;
                    if (root.TryGetProperty("usageMetadata", out var usage))
                    {
                        if (usage.TryGetProperty("promptTokenCount", out var ptc)) inputTokens = ptc.GetInt32();
                        if (usage.TryGetProperty("candidatesTokenCount", out var ctc)) outputTokens = ctc.GetInt32();
                    }

                    return new GeminiResponse
                    {
                        Success = true,
                        Text = text,
                        ModelName = _options.Model,
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens,
                        RawJson = responseBody
                    };
                }
            }

            return new GeminiResponse { Success = false, ErrorMessage = "Không thể phân tích phản hồi từ Gemini." };
        }
        catch (TaskCanceledException)
        {
            return new GeminiResponse { Success = false, ErrorMessage = "Yêu cầu AI đã hết thời gian chờ. Vui lòng thử lại." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
            return new GeminiResponse { Success = false, ErrorMessage = "Lỗi kết nối với Gemini AI. Kiểm tra API Key và thử lại." };
        }
    }
}

public class GeminiResponse
{
    public bool Success { get; set; }
    public string Text { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public string? ModelName { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? RawJson { get; set; }
}
