using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services.Kpi;
using OmniBizAI.ViewModels.Kpi;

namespace OmniBizAI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KpisController : ControllerBase
{
    private readonly IKpiService _kpiService;

    public KpisController(IKpiService kpiService)
    {
        _kpiService = kpiService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateKpi([FromBody] CreateKpiRequest request, CancellationToken cancellationToken)
    {
        var kpi = await _kpiService.CreateKpiAsync(request, GetUserId(), cancellationToken);
        return Ok(kpi);
    }

    [HttpPost("{id}/check-ins")]
    public async Task<IActionResult> SubmitCheckIn(Guid id, [FromBody] SubmitCheckInRequest request, CancellationToken cancellationToken)
    {
        if (id != request.KpiId) return BadRequest("KPI ID mismatch");

        var checkIn = await _kpiService.SubmitCheckInAsync(request, GetUserId(), cancellationToken);
        return Ok(checkIn);
    }

    [HttpPost("check-ins/{checkInId}/approve")]
    public async Task<IActionResult> ApproveCheckIn(Guid checkInId, [FromBody] ReviewCheckInRequest request, CancellationToken cancellationToken)
    {
        // For MVP, normally Director/Manager roles would be enforced here via Policy or inside Service
        var checkIn = await _kpiService.ApproveCheckInAsync(checkInId, request, GetUserId(), cancellationToken);
        return Ok(checkIn);
    }

    [HttpPost("check-ins/{checkInId}/reject")]
    public async Task<IActionResult> RejectCheckIn(Guid checkInId, [FromBody] ReviewCheckInRequest request, CancellationToken cancellationToken)
    {
        var checkIn = await _kpiService.RejectCheckInAsync(checkInId, request, GetUserId(), cancellationToken);
        return Ok(checkIn);
    }

    [HttpGet("scorecard")]
    public async Task<IActionResult> GetScorecard([FromQuery] Guid employeeId, [FromQuery] Guid periodId, CancellationToken cancellationToken)
    {
        var scorecard = await _kpiService.GetScorecardAsync(employeeId, periodId, GetUserId(), cancellationToken);
        return Ok(scorecard);
    }
}
