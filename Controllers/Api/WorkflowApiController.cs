using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/workflow")]
public class WorkflowApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public WorkflowApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("approvals")]
    public async Task<IActionResult> Approvals(CancellationToken cancellationToken)
    {
        var data = await _db.WorkflowInstances.AsNoTracking()
            .OrderByDescending(x => x.InitiatedAt)
            .Select(x => new
            {
                x.Id,
                x.EntityType,
                x.EntityId,
                x.CurrentStepOrder,
                x.TotalSteps,
                x.Status,
                x.InitiatedBy,
                x.InitiatedAt,
                x.CompletedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken)
    {
        var data = await _db.WorkflowTemplates.AsNoTracking()
            .OrderBy(x => x.EntityType)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.EntityType,
                x.Description,
                x.Version,
                x.IsActive,
                x.IsDefault,
                stepCount = x.WorkflowSteps.Count,
                conditionCount = x.WorkflowConditions.Count
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit(CancellationToken cancellationToken)
    {
        var data = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.UserEmail,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.EntityName,
                x.ChangesSummary,
                x.IpAddress,
                x.RequestPath,
                x.ResponseStatus,
                x.DurationMs,
                x.CreatedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
