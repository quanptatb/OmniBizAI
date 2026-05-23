using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class MaintenanceController : Controller
{
    private readonly MaintenanceService _service;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public MaintenanceController(MaintenanceService service, ApplicationDbContext db, ITenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    // ─── DASHBOARD (Theirs) ──────────────────────────────────────────────────
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

    // ─── INCIDENTS (CM - Ours) ───────────────────────────────────────────────
    public async Task<IActionResult> Incidents(string? search, string? severity, string? status)
    {
        var (items, total, open, inProg, resolved) = await _service.GetIncidentsAsync(search, severity, status);
        ViewBag.Search = search; ViewBag.SeverityFilter = severity; ViewBag.StatusFilter = status;
        ViewBag.Total = total; ViewBag.Open = open; ViewBag.InProgress = inProg; ViewBag.Resolved = resolved;
        return View(items);
    }

    public async Task<IActionResult> IncidentDetail(Guid id)
    {
        var vm = await _service.GetIncidentDetailAsync(id);
        if (vm == null) return NotFound();
        vm.AiAnalysis = TempData["AiAnalysis"] as string;
        return View(vm);
    }

    public async Task<IActionResult> ReportIncident()
    {
        var form = await _service.GetIncidentCreateFormAsync();
        return View(new IncidentCreateViewModel { Equipments = form.Equipments, Technicians = form.Technicians });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportIncident(IncidentCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetIncidentCreateFormAsync();
            vm.Equipments = form.Equipments; vm.Technicians = form.Technicians;
            return View(vm);
        }
        var id = await _service.CreateIncidentAsync(vm);
        TempData["SuccessMessage"] = "Đã báo cáo sự cố thành công.";
        return RedirectToAction(nameof(IncidentDetail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> ResolveIncident(ResolveIncidentViewModel vm)
    {
        var (success, message) = await _service.ResolveIncidentAsync(vm);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(IncidentDetail), new { id = vm.IncidentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> StartIncident(Guid id)
    {
        if (await _service.StartIncidentAsync(id))
            TempData["SuccessMessage"] = "Đã chuyển sự cố sang đang xử lý.";
        else
            TempData["ErrorMessage"] = "Không thể bắt đầu xử lý sự cố.";
        return RedirectToAction(nameof(IncidentDetail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CloseIncident(Guid id)
    {
        if (await _service.CloseIncidentAsync(id))
            TempData["SuccessMessage"] = "Đã đóng sự cố.";
        else
            TempData["ErrorMessage"] = "Chỉ có thể đóng sự cố đã ở trạng thái Resolved.";
        return RedirectToAction(nameof(IncidentDetail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeIncident(Guid id)
    {
        TempData["AiAnalysis"] = await _service.AnalyzeIncidentWithAiAsync(id);
        return RedirectToAction(nameof(IncidentDetail), new { id });
    }

    // ─── PM SCHEDULES (Ours) ──────────────────────────────────────────────────
    public async Task<IActionResult> PmSchedules(Guid? equipmentId, bool? overdueOnly)
    {
        var items = await _service.GetPmSchedulesAsync(equipmentId, overdueOnly);
        ViewBag.EquipmentFilter = equipmentId;
        ViewBag.OverdueOnly = overdueOnly;
        return View(items);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreatePmSchedule()
    {
        var form = await _service.GetPmCreateFormAsync();
        return View(new PmScheduleCreateViewModel { Equipments = form.Equipments, Technicians = form.Technicians });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreatePmSchedule(PmScheduleCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetPmCreateFormAsync();
            vm.Equipments = form.Equipments; vm.Technicians = form.Technicians;
            return View(vm);
        }
        await _service.CreatePmScheduleAsync(vm);
        TempData["SuccessMessage"] = "Đã tạo lịch bảo trì định kỳ.";
        return RedirectToAction(nameof(PmSchedules));
    }

    public async Task<IActionResult> ExecutePm(Guid id)
    {
        var tid = HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;
        var db = HttpContext.RequestServices.GetRequiredService<OmniBizAI.Data.ApplicationDbContext>();
        var techs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
        );
        return View(new ExecutePmViewModel { PmScheduleId = id, Technicians = techs });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> ExecutePm(ExecutePmViewModel vm)
    {
        if (await _service.ExecutePmTaskAsync(vm))
            TempData["SuccessMessage"] = "Đã ghi nhận hoàn thành công việc bảo trì.";
        return RedirectToAction(nameof(PmSchedules));
    }

    // ─── SPARE PARTS (Ours) ───────────────────────────────────────────────────
    public async Task<IActionResult> SpareParts(string? search, string? category)
    {
        var items = await _service.GetSparePartsAsync(search, category);
        ViewBag.Search = search; ViewBag.CategoryFilter = category;
        return View(items);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public IActionResult CreateSparePart() => View(new SparePartCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreateSparePart(SparePartCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.CreateSparePartAsync(vm);
        TempData["SuccessMessage"] = "Thêm phụ tùng thành công.";
        return RedirectToAction(nameof(SpareParts));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AdjustStock(StockAdjustViewModel vm)
    {
        var (success, message) = await _service.AdjustStockAsync(vm.PartId, vm.Delta, vm.Reason);
        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(SpareParts));
    }

    // ─── IoT / SENSOR (Ours) ──────────────────────────────────────────────────
    public async Task<IActionResult> SensorMonitor(Guid equipmentId)
    {
        var readings = await _service.GetLatestSensorReadingsAsync(equipmentId);
        var db = HttpContext.RequestServices.GetRequiredService<OmniBizAI.Data.ApplicationDbContext>();
        var eq = await db.Equipments.FindAsync(equipmentId);
        ViewBag.EquipmentId = equipmentId;
        ViewBag.EquipmentName = eq?.Name ?? "Thiết bị";
        return View(readings);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> SimulateSensor(Guid equipmentId)
    {
        await _service.SimulateSensorDataAsync(equipmentId);
        TempData["SuccessMessage"] = "Đã cập nhật dữ liệu cảm biến (giả lập).";
        return RedirectToAction(nameof(SensorMonitor), new { equipmentId });
    }

    [HttpGet]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> QuickSimulate()
    {
        try
        {
            var firstEquipment = await _db.Equipments.FirstOrDefaultAsync(e => !e.IsDeleted);
            if (firstEquipment != null)
            {
                await _service.SimulateSensorDataAsync(firstEquipment.Id);
                return Json(new { 
                    success = true, 
                    equipmentId = firstEquipment.Id, 
                    name = firstEquipment.Name, 
                    message = $"Đã giả lập thành công dữ liệu cảm biến cảnh báo cho thiết bị: {firstEquipment.Name}." 
                });
            }
            return Json(new { success = false, message = "Không tìm thấy thiết bị nào trong hệ thống để giả lập." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }
}
