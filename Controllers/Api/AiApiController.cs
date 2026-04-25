using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services.Integrations;
using OmniBizAI.Services.Integrations.Models;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/ai")]
public class AiApiController : ControllerBase
{
    private readonly IAiProviderClient _aiProvider;
    private readonly IConfigurationStatusService _configurationStatus;

    public AiApiController(IAiProviderClient aiProvider, IConfigurationStatusService configurationStatus)
    {
        _aiProvider = aiProvider;
        _configurationStatus = configurationStatus;
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(_configurationStatus.GetSnapshot().Ai);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiProviderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required" });
        }

        var response = await _aiProvider.CompleteAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("risk-analysis")]
    public async Task<IActionResult> RiskAnalysis([FromBody] AiProviderRequest request, CancellationToken cancellationToken)
    {
        var enriched = request with
        {
            Module = string.IsNullOrWhiteSpace(request.Module) ? "Finance" : request.Module,
            PromptType = "RiskAnalysis"
        };

        var response = await _aiProvider.CompleteAsync(enriched, cancellationToken);
        return Ok(response);
    }

    [HttpPost("report-summary")]
    public async Task<IActionResult> ReportSummary([FromBody] AiProviderRequest request, CancellationToken cancellationToken)
    {
        var enriched = request with
        {
            Module = string.IsNullOrWhiteSpace(request.Module) ? "Report" : request.Module,
            PromptType = "ReportSummary"
        };

        var response = await _aiProvider.CompleteAsync(enriched, cancellationToken);
        return Ok(response);
    }
}
