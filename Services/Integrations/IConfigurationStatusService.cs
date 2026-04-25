using OmniBizAI.Services.Integrations.Models;

namespace OmniBizAI.Services.Integrations;

public interface IConfigurationStatusService
{
    IntegrationStatusSnapshot GetSnapshot();
}
