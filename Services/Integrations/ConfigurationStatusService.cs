using Microsoft.Extensions.Options;
using OmniBizAI.Services.Integrations.Models;
using OmniBizAI.Services.Options;

namespace OmniBizAI.Services.Integrations;

public sealed class ConfigurationStatusService : IConfigurationStatusService
{
    private readonly IConfiguration _configuration;
    private readonly AiOptions _ai;
    private readonly JwtOptions _jwt;
    private readonly SmtpOptions _smtp;
    private readonly RedisOptions _redis;

    public ConfigurationStatusService(
        IConfiguration configuration,
        IOptions<AiOptions> ai,
        IOptions<JwtOptions> jwt,
        IOptions<SmtpOptions> smtp,
        IOptions<RedisOptions> redis)
    {
        _configuration = configuration;
        _ai = ai.Value;
        _jwt = jwt.Value;
        _smtp = smtp.Value;
        _redis = redis.Value;
    }

    public IntegrationStatusSnapshot GetSnapshot()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var allowedOrigins = _configuration["AllowedOrigins"];

        return new IntegrationStatusSnapshot(
            Database: Item("SQL Server", !string.IsNullOrWhiteSpace(connectionString), "EF Core", MaskConnectionString(connectionString)),
            Jwt: Item("JWT", IsJwtConfigured(), "Token settings", BuildJwtDetail()),
            Cors: Item("CORS", !string.IsNullOrWhiteSpace(allowedOrigins), "Allowed origins", string.IsNullOrWhiteSpace(allowedOrigins) ? "Not configured" : allowedOrigins),
            Ai: Item("AI Provider", IsAiConfigured(), _ai.Provider, BuildAiDetail()),
            Smtp: Item("SMTP Email", !string.IsNullOrWhiteSpace(_smtp.Host), "Email queue ready", string.IsNullOrWhiteSpace(_smtp.Host) ? "Optional connector is not configured" : $"{_smtp.Host}:{_smtp.Port}"),
            Redis: Item("Redis Cache", !string.IsNullOrWhiteSpace(_redis.ConnectionString), "Cache connector", string.IsNullOrWhiteSpace(_redis.ConnectionString) ? "Optional connector is not configured" : "Connection string present"));
    }

    private static IntegrationStatusItem Item(string name, bool configured, string status, string detail)
        => new(name, configured, configured ? status : "Missing configuration", detail);

    private bool IsJwtConfigured()
        => !string.IsNullOrWhiteSpace(_jwt.Secret)
           && !string.IsNullOrWhiteSpace(_jwt.Issuer)
           && !string.IsNullOrWhiteSpace(_jwt.Audience);

    private bool IsAiConfigured()
        => _ai.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase)
           || (!string.IsNullOrWhiteSpace(_ai.Provider)
               && !string.IsNullOrWhiteSpace(_ai.ApiKey)
               && !string.IsNullOrWhiteSpace(_ai.Model));

    private string BuildJwtDetail()
    {
        if (!IsJwtConfigured())
        {
            return "Jwt__Secret, Jwt__Issuer, or Jwt__Audience is missing";
        }

        return $"Issuer: {_jwt.Issuer}; Audience: {_jwt.Audience}; Secret: configured";
    }

    private string BuildAiDetail()
    {
        if (_ai.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            return "Mock provider enabled for offline demo";
        }

        return IsAiConfigured()
            ? $"Model: {_ai.Model}; API key: configured"
            : "AI__Provider, AI__ApiKey, or AI__Model is missing";
    }

    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "Connection string is missing";
        }

        return string.Join(';', connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase) || part.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase)
                ? $"{part.Split('=')[0]}=***"
                : part));
    }
}
