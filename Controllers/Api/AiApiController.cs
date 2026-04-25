using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OmniBizAI.Services.Integrations;
using OmniBizAI.Services.Integrations.Models;

namespace OmniBizAI.Controllers.Api;

[Authorize]
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

        return await CompleteSafelyAsync(request, cancellationToken);
    }

    [HttpPost("risk-analysis")]
    public async Task<IActionResult> RiskAnalysis([FromBody] AiProviderRequest request, CancellationToken cancellationToken)
    {
        var enriched = request with
        {
            Module = string.IsNullOrWhiteSpace(request.Module) ? "Finance" : request.Module,
            PromptType = "RiskAnalysis"
        };

        return await CompleteSafelyAsync(enriched, cancellationToken);
    }

    [HttpPost("report-summary")]
    public async Task<IActionResult> ReportSummary([FromBody] AiProviderRequest request, CancellationToken cancellationToken)
    {
        var enriched = request with
        {
            Module = string.IsNullOrWhiteSpace(request.Module) ? "Report" : request.Module,
            PromptType = "ReportSummary"
        };

        return await CompleteSafelyAsync(enriched, cancellationToken);
    }

    private async Task<IActionResult> CompleteSafelyAsync(AiProviderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aiProvider.CompleteAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AiProviderException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = ex.Message,
                providerStatus = (int)ex.StatusCode,
                detail = BuildProviderErrorDetail(ex)
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Cannot connect to the configured AI provider.",
                detail = "Check network access, AI__Provider, AI__BaseUrl, AI__ApiKey, and AI__Model."
            });
        }
    }

    private static string BuildProviderErrorDetail(AiProviderException ex)
    {
        if ((int)ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return "Provider returned 401. For Google Gemini, use a valid Gemini API key, usually from Google AI Studio, and verify AI__Model.";
        }

        return "Check AI provider configuration and selected model.";
    }
}
