using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;

    public PerformanceController(IPerformanceService performanceService)
    {
        _performanceService = performanceService;
    }

    [HttpPost("evaluation-periods")]
    [Authorize(Roles = "Admin,Director")]
    public async Task<ActionResult<ApiResponse<EvaluationPeriodDto>>> CreatePeriod(CreateEvaluationPeriodRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<EvaluationPeriodDto>.Ok(await _performanceService.CreatePeriodAsync(request, cancellationToken), "Evaluation period created"));
    }

    [HttpPost("objectives")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<ObjectiveDto>>> CreateObjective(CreateObjectiveRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<ObjectiveDto>.Ok(await _performanceService.CreateObjectiveAsync(request, cancellationToken), "Objective created"));
    }

    [HttpPost("key-results")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<KeyResultDto>>> CreateKeyResult(CreateKeyResultRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<KeyResultDto>.Ok(await _performanceService.CreateKeyResultAsync(request, cancellationToken), "Key result created"));
    }

    [HttpPost("kpis")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<KpiDto>>> CreateKpi(CreateKpiRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<KpiDto>.Ok(await _performanceService.CreateKpiAsync(request, cancellationToken), "KPI created"));
    }

    [HttpPost("kpis/{id:guid}/check-in")]
    public async Task<ActionResult<ApiResponse<KpiCheckInDto>>> CreateCheckIn(Guid id, CreateKpiCheckInRequest request, CancellationToken cancellationToken)
    {
        var normalized = request with { KpiId = id };
        return Ok(ApiResponse<KpiCheckInDto>.Ok(await _performanceService.CreateCheckInAsync(normalized, cancellationToken), "Check-in submitted"));
    }

    [HttpPost("check-ins/{id:guid}/approve")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<KpiCheckInDto>>> ApproveCheckIn(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<KpiCheckInDto>.Ok(await _performanceService.ApproveCheckInAsync(id, request, cancellationToken), "Check-in approved"));
    }

    [HttpPost("check-ins/{id:guid}/reject")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<KpiCheckInDto>>> RejectCheckIn(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<KpiCheckInDto>.Ok(await _performanceService.RejectCheckInAsync(id, request, cancellationToken), "Check-in rejected"));
    }
}
