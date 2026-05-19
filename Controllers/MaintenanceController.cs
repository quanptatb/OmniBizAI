using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public MaintenanceController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? mode = null)
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var validTypes = new[] { "Maintenance", "Maintenance_PM", "Maintenance_CM" };

        var query = _db.OperationRequests
            .Where(x => x.TenantId == tid && !x.IsDeleted && validTypes.Contains(x.Type));

        if (mode == "PM") query = query.Where(x => x.Type == "Maintenance_PM");
        if (mode == "CM") query = query.Where(x => x.Type == "Maintenance_CM");

        var requests = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(80)
            .Select(x => new MaintenanceRequestItemViewModel
            {
                Id = x.Id,
                RequestNo = x.RequestNo,
                Title = x.Title,
                Type = x.Type,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                DueDate = x.DueDate,
                CreatedAt = x.CreatedAt,
                TotalAmount = x.TotalAmount
            }).ToListAsync();

        var requestIds = requests.Select(x => x.Id).ToList();
        var partsByRequest = await _db.OperationRequestLines
            .Where(x => x.TenantId == tid && !x.IsDeleted && requestIds.Contains(x.OperationRequestId))
            .GroupBy(x => x.OperationRequestId)
            .Select(g => new { RequestId = g.Key, PartCount = g.Count(), TotalPartQty = g.Sum(v => v.Quantity) })
            .ToListAsync();

        var partMap = partsByRequest.ToDictionary(x => x.RequestId, x => (x.PartCount, x.TotalPartQty));
        foreach (var item in requests)
        {
            if (partMap.TryGetValue(item.Id, out var parts))
            {
                item.SparePartLines = parts.PartCount;
                item.SparePartQuantity = parts.TotalPartQty;
            }
        }

        var vm = new MaintenanceDashboardViewModel
        {
            Mode = mode,
            TotalRequests = await query.CountAsync(),
            PreventiveCount = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance_PM"),
            CorrectiveCount = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance_CM"),
            OverdueCount = await query.CountAsync(x => x.DueDate.HasValue && x.DueDate < today && x.Status != OperationStatus.Completed),
            CompletedCount = await query.CountAsync(x => x.Status == OperationStatus.Completed),
            TotalSparePartLines = partsByRequest.Sum(x => x.PartCount),
            TotalSparePartQuantity = partsByRequest.Sum(x => x.TotalPartQty),
            IotSignalCount = await _db.AiInsights.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.ContextType == "IoT"),
            Requests = requests
        };

        return View(vm);
    }
}
