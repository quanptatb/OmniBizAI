namespace OmniBizAI.Services.Integrations.Models;

public sealed record IntegrationStatusSnapshot(
    IntegrationStatusItem Database,
    IntegrationStatusItem Jwt,
    IntegrationStatusItem Cors,
    IntegrationStatusItem Ai,
    IntegrationStatusItem Smtp,
    IntegrationStatusItem Redis);

public sealed record IntegrationStatusItem(
    string Name,
    bool Configured,
    string Status,
    string Detail);
