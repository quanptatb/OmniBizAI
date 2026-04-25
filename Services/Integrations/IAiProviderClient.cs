using OmniBizAI.Services.Integrations.Models;

namespace OmniBizAI.Services.Integrations;

public interface IAiProviderClient
{
    Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, CancellationToken cancellationToken = default);
}
