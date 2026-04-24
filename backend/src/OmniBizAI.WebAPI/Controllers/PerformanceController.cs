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

    [HttpGet("evaluation-periods")]
    public async Task<ActionResult<ApiResponse<PagedResult<EvaluationPeriodDto>>>> GetPeriods([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<EvaluationPeriodDto>>.Ok(await _performanceService.GetPeriodsAsync(request, cancellationToken)));

    [HttpGet("evaluation-periods/{id}")]
    public async Task<ActionResult<ApiResponse<EvaluationPeriodDto>>> GetPeriod(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<EvaluationPeriodDto>.Ok(await _performanceService.GetPeriodAsync(id, cancellationToken)));

    [HttpPut("evaluation-periods/{id}")]
    public async Task<ActionResult<ApiResponse<EvaluationPeriodDto>>> UpdatePeriod(Guid id, UpdateEvaluationPeriodRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<EvaluationPeriodDto>.Ok(await _performanceService.UpdatePeriodAsync(id, request, cancellationToken)));

    [HttpGet("objectives")]
    public async Task<ActionResult<ApiResponse<PagedResult<ObjectiveDto>>>> GetObjectives([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<ObjectiveDto>>.Ok(await _performanceService.GetObjectivesAsync(request, cancellationToken)));

    [HttpGet("objectives/{id}")]
    public async Task<ActionResult<ApiResponse<ObjectiveDto>>> GetObjective(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<ObjectiveDto>.Ok(await _performanceService.GetObjectiveAsync(id, cancellationToken)));

    [HttpPut("objectives/{id}")]
    public async Task<ActionResult<ApiResponse<ObjectiveDto>>> UpdateObjective(Guid id, UpdateObjectiveRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<ObjectiveDto>.Ok(await _performanceService.UpdateObjectiveAsync(id, request, cancellationToken)));

    [HttpDelete("objectives/{id}")]
    public async Task<ActionResult> DeleteObjective(Guid id, CancellationToken cancellationToken)
    {
        await _performanceService.DeleteObjectiveAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("objectives/tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ObjectiveDto>>>> GetObjectiveTree(CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<ObjectiveDto>>.Ok(await _performanceService.GetObjectiveTreeAsync(cancellationToken)));

    [HttpGet("key-results")]
    public async Task<ActionResult<ApiResponse<PagedResult<KeyResultDto>>>> GetKeyResults([FromQuery] Guid? objectiveId, [FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<KeyResultDto>>.Ok(await _performanceService.GetKeyResultsAsync(objectiveId, request, cancellationToken)));

    [HttpPut("key-results/{id}")]
    public async Task<ActionResult<ApiResponse<KeyResultDto>>> UpdateKeyResult(Guid id, UpdateKeyResultRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<KeyResultDto>.Ok(await _performanceService.UpdateKeyResultAsync(id, request, cancellationToken)));

    [HttpDelete("key-results/{id}")]
    public async Task<ActionResult> DeleteKeyResult(Guid id, CancellationToken cancellationToken)
    {
        await _performanceService.DeleteKeyResultAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiDto>>>> GetKpis([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<KpiDto>>.Ok(await _performanceService.GetKpisAsync(request, cancellationToken)));

    [HttpGet("kpis/{id}")]
    public async Task<ActionResult<ApiResponse<KpiDto>>> GetKpi(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<KpiDto>.Ok(await _performanceService.GetKpiAsync(id, cancellationToken)));

    [HttpPut("kpis/{id}")]
    public async Task<ActionResult<ApiResponse<KpiDto>>> UpdateKpi(Guid id, UpdateKpiRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<KpiDto>.Ok(await _performanceService.UpdateKpiAsync(id, request, cancellationToken)));

    [HttpDelete("kpis/{id}")]
    public async Task<ActionResult> DeleteKpi(Guid id, CancellationToken cancellationToken)
    {
        await _performanceService.DeleteKpiAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("check-ins")]
    public async Task<ActionResult<ApiResponse<PagedResult<KpiCheckInDto>>>> GetCheckIns([FromQuery] Guid? kpiId, [FromQuery] string? status, [FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<KpiCheckInDto>>.Ok(await _performanceService.GetCheckInsAsync(kpiId, status, request, cancellationToken)));

    [HttpGet("scorecard/{employeeId}")]
    public async Task<ActionResult<ApiResponse<KpiScorecardDto>>> GetScorecard(Guid employeeId, CancellationToken cancellationToken) => Ok(ApiResponse<KpiScorecardDto>.Ok(await _performanceService.GetScorecardAsync(employeeId, cancellationToken)));

}
