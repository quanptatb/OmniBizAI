using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/v1/performance")]
public class PerformanceApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PerformanceApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("objectives")]
    public async Task<IActionResult> Objectives(CancellationToken cancellationToken)
    {
        var data = await _db.Objectives.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.OwnerType,
                x.DepartmentId,
                x.OwnerId,
                x.Progress,
                x.Status,
                x.Priority,
                x.PeriodId,
                keyResultCount = x.KeyResults.Count
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken cancellationToken)
    {
        var data = await _db.Kpis.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DepartmentId,
                x.AssigneeId,
                x.MetricType,
                x.Unit,
                x.TargetValue,
                x.CurrentValue,
                x.Weight,
                x.Progress,
                x.Frequency,
                x.Status,
                x.Score,
                x.Rating,
                x.LastCheckInAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("check-ins")]
    public async Task<IActionResult> CheckIns(CancellationToken cancellationToken)
    {
        var data = await _db.KpiCheckIns.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.KpiId,
                x.CheckInDate,
                x.PreviousValue,
                x.NewValue,
                x.Progress,
                x.Status,
                x.ReviewedAt,
                x.CreatedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("evaluations")]
    public async Task<IActionResult> Evaluations(CancellationToken cancellationToken)
    {
        var data = await _db.PerformanceEvaluations.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.EmployeeId,
                x.DepartmentId,
                x.PeriodId,
                x.OkrScore,
                x.KpiScore,
                x.TotalScore,
                x.Rating,
                x.Status,
                x.CreatedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
