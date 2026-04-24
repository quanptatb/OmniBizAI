using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ai")]
public sealed class AiController : ControllerBase
{
    private readonly IAiCopilotService _aiCopilotService;

    public AiController(IAiCopilotService aiCopilotService)
    {
        _aiCopilotService = aiCopilotService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiChatResponse>>> Chat(AiChatRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<AiChatResponse>.Ok(await _aiCopilotService.ChatAsync(request, cancellationToken)));
    }

    [HttpPost("risk-analysis")]
    public async Task<ActionResult<ApiResponse<RiskAnalysisResponse>>> RiskAnalysis(RiskAnalysisRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<RiskAnalysisResponse>.Ok(await _aiCopilotService.AnalyzeRiskAsync(request, cancellationToken)));
    }
}
