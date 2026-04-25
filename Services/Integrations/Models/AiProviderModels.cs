namespace OmniBizAI.Services.Integrations.Models;

public sealed record AiProviderRequest(
    string Module,
    string PromptType,
    string Message,
    string? Context = null);

public sealed record AiProviderResponse(
    string Provider,
    string Model,
    string Content,
    int? TokensUsed = null,
    int LatencyMs = 0,
    bool IsMock = false);
