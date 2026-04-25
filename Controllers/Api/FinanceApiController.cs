using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/finance")]
public class FinanceApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FinanceApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("budgets")]
    public async Task<IActionResult> Budgets(CancellationToken cancellationToken)
    {
        var data = await _db.Budgets.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.AllocatedAmount,
                x.SpentAmount,
                x.CommittedAmount,
                remainingAmount = x.AllocatedAmount - x.SpentAmount - x.CommittedAmount,
                utilization = x.AllocatedAmount == 0 ? 0 : Math.Round((x.SpentAmount + x.CommittedAmount) * 100 / x.AllocatedAmount, 2),
                x.WarningThreshold,
                x.Status,
                x.DepartmentId,
                x.CategoryId,
                x.FiscalPeriodId
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("payment-requests")]
    public async Task<IActionResult> PaymentRequests(CancellationToken cancellationToken)
    {
        var data = await _db.PaymentRequests.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.RequestNumber,
                x.Title,
                x.DepartmentId,
                x.RequesterId,
                x.VendorId,
                x.TotalAmount,
                x.Currency,
                x.Priority,
                x.Status,
                x.AiRiskScore,
                x.PaymentDueDate,
                x.CreatedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(CancellationToken cancellationToken)
    {
        var data = await _db.Transactions.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.TransactionDate)
            .Select(x => new
            {
                x.Id,
                x.TransactionNumber,
                x.Type,
                x.Amount,
                x.Currency,
                x.TransactionDate,
                x.DepartmentId,
                x.CategoryId,
                x.BudgetId,
                x.PaymentRequestId,
                x.VendorId,
                x.Status,
                x.Reconciled
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("vendors")]
    public async Task<IActionResult> Vendors(CancellationToken cancellationToken)
    {
        var data = await _db.Vendors.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ShortName,
                x.TaxCode,
                x.ContactPerson,
                x.Phone,
                x.Email,
                x.Rating,
                x.Status,
                paymentRequestCount = x.PaymentRequests.Count,
                transactionCount = x.Transactions.Count
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
