using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class MonitoringController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly NotificationService _notif;
    private readonly IEmailService _email;

    public MonitoringController(ApplicationDbContext db, ITenantContext tenant, NotificationService notif, IEmailService email)
    {
        _db = db;
        _tenant = tenant;
        _notif = notif;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var requests = await _db.OperationRequests.Where(x => x.TenantId == tid && !x.IsDeleted).ToListAsync();

        var total = requests.Count;
        var completed = requests.Count(x => x.Status == OperationStatus.Completed);
        var inProgress = requests.Count(x => x.Status == OperationStatus.InProgress || x.Status == OperationStatus.Submitted || x.Status == OperationStatus.InReview);
        var overdue = requests.Count(x => x.DueDate.HasValue && x.DueDate < today && x.Status != OperationStatus.Completed);

        var vm = new MonitoringDashboardViewModel
        {
            Kpi = new RealtimeKpiViewModel
            {
                TotalRequests = total,
                InProgressRequests = inProgress,
                CompletedRequests = completed,
                OverdueRequests = overdue,
                CompletionRate = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0
            },
            Oee = new OeeChartViewModel
            {
                Availability = total > 0 ? Math.Round((decimal)(total - overdue) / total * 100, 1) : 0,
                Performance = total > 0 ? Math.Round((decimal)inProgress / total * 100, 1) : 0,
                Quality = await GetQualityRateAsync(tid)
            },
            Heatmap = requests.GroupBy(x => x.OrganizationUnitId)
                .Select(g => new HeatmapCellViewModel { Label = g.Key.ToString()[..8], Intensity = g.Count() }).ToList(),
            Gantt = requests.OrderBy(x => x.DueDate ?? today).Take(20)
                .Select(x => new GanttTaskViewModel
                {
                    RequestNo = x.RequestNo,
                    Title = x.Title,
                    StartDate = DateOnly.FromDateTime(x.CreatedAt.Date),
                    EndDate = x.DueDate,
                    Status = x.Status.ToString()
                }).ToList(),
            RecentAlerts = await _db.StockAlerts.Where(x => x.TenantId == tid && !x.IsDeleted && x.IsActive)
                .OrderByDescending(x => x.CreatedAt).Take(10)
                .Select(x => new MonitoringAlertItemViewModel { Title = x.Message, Severity = x.Severity, CreatedAt = x.CreatedAt }).ToListAsync()
        };

        vm.Oee.Overall = Math.Round(vm.Oee.Availability * vm.Oee.Performance * vm.Oee.Quality / 10000, 1);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> SendAlert(MonitoringAlertSendViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Title) || string.IsNullOrWhiteSpace(vm.Message))
        {
            TempData["ErrorMessage"] = "Thiếu tiêu đề hoặc nội dung cảnh báo.";
            return RedirectToAction(nameof(Index));
        }

        await _notif.SendToManagersAsync($"🚨 {vm.Title}", vm.Message, "Monitoring", null);

        if (!string.IsNullOrWhiteSpace(vm.EmailTo))
            await _email.SendAsync(vm.EmailTo, $"[OmniBizAI Alert] {vm.Title}", $"<p>{vm.Message}</p>");

        // SMS/Zalo hooks: lưu audit để tích hợp adapter sau
        _db.AuditLogs.Add(new OmniBizAI.Models.Entities.AuditLog
        {
            TenantId = _tenant.TenantId,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            Action = "MonitoringAlert",
            EntityName = "Monitoring",
            NewValuesJson = $"{{\"Channel\":\"SMS/Zalo\",\"Phone\":\"{vm.PhoneTo}\",\"Zalo\":\"{vm.ZaloTo}\",\"Title\":\"{vm.Title}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã gửi cảnh báo (in-app/email) và ghi nhận hook SMS/Zalo.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<decimal> GetQualityRateAsync(Guid tid)
    {
        var rows = await _db.GoodsReceiptLines.Where(x => x.TenantId == tid && !x.IsDeleted)
            .Select(x => new { x.OrderedQuantity, x.AcceptedQuantity }).ToListAsync();
        var ordered = rows.Sum(x => x.OrderedQuantity);
        if (ordered <= 0) return 0;
        var accepted = rows.Sum(x => x.AcceptedQuantity);
        return Math.Round(accepted / ordered * 100, 1);
    }
}
