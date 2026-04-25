using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OmniBizAI.Services.Integrations.Models;
using OmniBizAI.Services.Options;

namespace OmniBizAI.Services.Integrations;

public sealed class AiProviderClient : IAiProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;

    public AiProviderClient(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (_options.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return MockResponse(request, (int)stopwatch.ElapsedMilliseconds);
        }

        return _options.Provider.ToLowerInvariant() switch
        {
            "groq" => await CompleteOpenAiCompatibleAsync(BuildBaseUrl("https://api.groq.com/openai/v1/chat/completions"), request, stopwatch, cancellationToken),
            "openai" => await CompleteOpenAiCompatibleAsync(BuildBaseUrl("https://api.openai.com/v1/chat/completions"), request, stopwatch, cancellationToken),
            "claude" or "anthropic" => await CompleteClaudeAsync(BuildBaseUrl("https://api.anthropic.com/v1/messages"), request, stopwatch, cancellationToken),
            "google" or "gemini" => await CompleteGeminiAsync(BuildGeminiUrl(), request, stopwatch, cancellationToken),
            _ => MockResponse(request, (int)stopwatch.ElapsedMilliseconds)
        };
    }

    private async Task<AiProviderResponse> CompleteOpenAiCompatibleAsync(string url, AiProviderRequest request, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent(new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = BuildSystemPrompt(request.Module, request.Context) },
                new { role = "user", content = request.Message }
            },
            temperature = 0.2
        });

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureProviderSuccess(response, body);

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        var tokens = doc.RootElement.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var totalTokens)
            ? totalTokens.GetInt32()
            : (int?)null;

        return new AiProviderResponse(_options.Provider, _options.Model, content, tokens, (int)stopwatch.ElapsedMilliseconds);
    }

    private async Task<AiProviderResponse> CompleteClaudeAsync(string url, AiProviderRequest request, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = JsonContent(new
        {
            model = _options.Model,
            max_tokens = 900,
            system = BuildSystemPrompt(request.Module, request.Context),
            messages = new[] { new { role = "user", content = request.Message } }
        });

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureProviderSuccess(response, body);

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;

        return new AiProviderResponse(_options.Provider, _options.Model, content, null, (int)stopwatch.ElapsedMilliseconds);
    }

    private async Task<AiProviderResponse> CompleteGeminiAsync(string url, AiProviderRequest request, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
        httpRequest.Content = JsonContent(new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = BuildSystemPrompt(request.Module, request.Context) } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = request.Message } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 900
            }
        });

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureProviderSuccess(response, body);

        using var doc = JsonDocument.Parse(body);
        var content = ReadGeminiContent(doc.RootElement);
        var tokens = doc.RootElement.TryGetProperty("usageMetadata", out var usage)
            && usage.TryGetProperty("totalTokenCount", out var totalTokens)
            ? totalTokens.GetInt32()
            : (int?)null;

        return new AiProviderResponse(_options.Provider, _options.Model, content, tokens, (int)stopwatch.ElapsedMilliseconds);
    }

    private string BuildBaseUrl(string fallback)
        => string.IsNullOrWhiteSpace(_options.BaseUrl) ? fallback : _options.BaseUrl;

    private string BuildGeminiUrl()
    {
        var fallback = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent";
        var baseUrl = BuildBaseUrl(fallback);
        var separator = baseUrl.Contains('?') ? "&" : "?";

        return baseUrl.Contains("key=", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}{separator}key={Uri.EscapeDataString(_options.ApiKey ?? string.Empty)}";
    }

    private static string BuildSystemPrompt(string module, string? context)
        => $"""
           Bạn là OmniBizAI Copilot cho doanh nghiệp SME Việt Nam.
           Chỉ trả lời trong phạm vi module {module}; ưu tiên số liệu có trong context.
           Trả lời ngắn gọn, có khuyến nghị hành động và nhắc rõ khi dữ liệu chưa đủ.
           Context: {context ?? "Không có context bổ sung."}
           """;

    private static AiProviderResponse MockResponse(AiProviderRequest request, int latencyMs)
        => new(
            "Mock",
            "mock-omnibiz-ai",
            $"[Demo AI] Đã nhận yêu cầu {request.PromptType} cho module {request.Module}. Khuyến nghị: kiểm tra dữ liệu nguồn, ưu tiên các cảnh báo rủi ro cao và ghi nhận hành động vào workflow.",
            0,
            latencyMs,
            true);

    private static string ReadGeminiContent(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        if (!candidates[0].TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts))
        {
            return string.Empty;
        }

        var texts = parts.EnumerateArray()
            .Where(part => part.TryGetProperty("text", out _))
            .Select(part => part.GetProperty("text").GetString())
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, texts);
    }

    private static StringContent JsonContent<T>(T payload)
        => new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    private static void EnsureProviderSuccess(HttpResponseMessage response, string body)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "AI provider rejected the API key or credentials.",
            HttpStatusCode.Forbidden => "AI provider denied access for this API key, model, or project.",
            HttpStatusCode.NotFound => "AI provider endpoint or model was not found.",
            HttpStatusCode.TooManyRequests => "AI provider rate limit was reached.",
            _ => "AI provider request failed."
        };

        throw new AiProviderException(response.StatusCode, message, TrimProviderBody(body));
    }

    private static string? TrimProviderBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        const int maxLength = 600;
        return body.Length <= maxLength ? body : body[..maxLength];
    }
}

public sealed class AiProviderException : Exception
{
    public AiProviderException(HttpStatusCode statusCode, string message, string? providerBody)
        : base(message)
    {
        StatusCode = statusCode;
        ProviderBody = providerBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ProviderBody { get; }
}
