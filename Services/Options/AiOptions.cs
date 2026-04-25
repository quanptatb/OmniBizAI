namespace OmniBizAI.Services.Options;

public sealed class AiOptions
{
    public string Provider { get; set; } = "Mock";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "mock-omnibiz-ai";

    public string? BaseUrl { get; set; }
}
