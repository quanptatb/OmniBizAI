using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OrderProcessController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public OrderProcessController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var tid = _tenant.TenantId;

        var baseQ = _db.OperationRequests.Where(x => x.TenantId == tid && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OperationStatus>(status, out var st))
            baseQ = baseQ.Where(x => x.Status == st);

        var items = await baseQ.OrderByDescending(x => x.CreatedAt).Take(100)
            .Select(x => new OrderProcessItemViewModel
            {
                Id = x.Id,
                RequestNo = x.RequestNo,
                Title = x.Title,
                Type = x.Type,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                DueDate = x.DueDate,
                CreatedAt = x.CreatedAt
            }).ToListAsync();

        var ids = items.Select(x => x.Id).ToList();

        var approvalMap = await _db.ApprovalTasks.Where(x => x.TenantId == tid && !x.IsDeleted && ids.Contains(x.TargetId))
            .GroupBy(x => x.TargetId)
            .Select(g => new { Id = g.Key, Total = g.Count(), Done = g.Count(v => v.Status == ApprovalStatus.Approved), Pending = g.Count(v => v.Status == ApprovalStatus.Pending) })
            .ToDictionaryAsync(x => x.Id, x => x);

        var qcMap = await _db.GoodsIssues.Where(x => x.TenantId == tid && !x.IsDeleted)
            .Where(x => x.OperationRequestId.HasValue && ids.Contains(x.OperationRequestId.Value))
            .GroupJoin(_db.GoodsIssueLines.Where(l => l.TenantId == tid && !l.IsDeleted), x => x.Id, l => l.GoodsIssueId,
                (x, lines) => new { RequestId = x.OperationRequestId!.Value, Ordered = lines.Sum(v => v.RequestedQuantity), Accepted = lines.Sum(v => v.IssuedQuantity) })
            .GroupBy(x => x.RequestId)
            .Select(g => new { Id = g.Key, Ordered = g.Sum(v => v.Ordered), Accepted = g.Sum(v => v.Accepted) })
            .ToDictionaryAsync(x => x.Id, x => x);

        foreach (var item in items)
        {
            if (approvalMap.TryGetValue(item.Id, out var ap))
            {
                item.ApprovalLevels = ap.Total;
                item.ApprovedLevels = ap.Done;
                item.PendingApprovalLevels = ap.Pending;
            }
            if (qcMap.TryGetValue(item.Id, out var qc) && qc.Ordered > 0)
            {
                item.QcPassRate = Math.Round(qc.Accepted / qc.Ordered * 100, 1);
            }

            item.TraceabilityCode = $"TRC-{item.RequestNo}";
        }

        var vm = new OrderProcessDashboardViewModel
        {
            StatusFilter = status,
            TotalOrders = await baseQ.CountAsync(),
            ProcessingOrders = await baseQ.CountAsync(x => x.Status == OperationStatus.Submitted || x.Status == OperationStatus.InProgress || x.Status == OperationStatus.InReview),
            CompletedOrders = await baseQ.CountAsync(x => x.Status == OperationStatus.Completed),
            PendingApprovals = await _db.ApprovalTasks.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == ApprovalStatus.Pending),
            QaCheckedOrders = items.Count(x => x.QcPassRate.HasValue),
            Items = items
        };

        return View(vm);
    }
}
