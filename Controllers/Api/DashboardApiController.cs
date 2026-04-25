using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var income = await _db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.Type == "Income").SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var expense = await _db.Transactions.AsNoTracking().Where(x => !x.IsDeleted && x.Type == "Expense").SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var allocated = await _db.Budgets.AsNoTracking().Where(x => !x.IsDeleted).SumAsync(x => (decimal?)x.AllocatedAmount, cancellationToken) ?? 0;
        var spent = await _db.Budgets.AsNoTracking().Where(x => !x.IsDeleted).SumAsync(x => (decimal?)x.SpentAmount, cancellationToken) ?? 0;
        var kpiAverage = await _db.Kpis.AsNoTracking().Where(x => !x.IsDeleted).AverageAsync(x => (decimal?)x.Progress, cancellationToken) ?? 0;
        var pendingApprovals = await _db.WorkflowInstances.AsNoTracking().CountAsync(x => x.Status == "Pending" || x.Status == "InProgress", cancellationToken);
        var highRiskRequests = await _db.PaymentRequests.AsNoTracking().CountAsync(x => !x.IsDeleted && x.AiRiskScore >= 70, cancellationToken);

        return Ok(new
        {
            finance = new { income, expense, allocatedBudget = allocated, spentBudget = spent, remainingBudget = allocated - spent },
            performance = new { kpiAverage = Math.Round(kpiAverage, 2), activeKpis = await _db.Kpis.CountAsync(x => !x.IsDeleted && x.Status == "Active", cancellationToken) },
            workflow = new { pendingApprovals, highRiskRequests },
            generatedAt = DateTime.UtcNow
        });
    }
}
