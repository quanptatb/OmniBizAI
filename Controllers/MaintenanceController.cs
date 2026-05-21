using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class MaintenanceController : Controller
{
    private readonly MaintenanceService _service;
    private readonly OmniBizAI.Data.ApplicationDbContext _db;

    public MaintenanceController(MaintenanceService service, OmniBizAI.Data.ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }

    // ─── DASHBOARD ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var vm = await _service.GetDashboardAsync();
        return View(vm);
    }

    // ─── INCIDENTS (CM) ─────────────────────────────────────────────────────
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
        if (await _service.ResolveIncidentAsync(vm))
            TempData["SuccessMessage"] = "Đã xác nhận giải quyết sự cố.";
        return RedirectToAction(nameof(IncidentDetail), new { id = vm.IncidentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeIncident(Guid id)
    {
        TempData["AiAnalysis"] = await _service.AnalyzeIncidentWithAiAsync(id);
        return RedirectToAction(nameof(IncidentDetail), new { id });
    }

    // ─── PM SCHEDULES ────────────────────────────────────────────────────────
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
    public async Task<IActionResult> ExecutePm(ExecutePmViewModel vm)
    {
        if (await _service.ExecutePmTaskAsync(vm))
            TempData["SuccessMessage"] = "Đã ghi nhận hoàn thành công việc bảo trì.";
        return RedirectToAction(nameof(PmSchedules));
    }

    // ─── SPARE PARTS ─────────────────────────────────────────────────────────
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
    public async Task<IActionResult> AdjustStock(StockAdjustViewModel vm)
    {
        await _service.AdjustStockAsync(vm.PartId, vm.Delta, vm.Reason);
        TempData["SuccessMessage"] = vm.Delta > 0 ? $"Đã nhập kho +{vm.Delta}" : $"Đã xuất kho {vm.Delta}";
        return RedirectToAction(nameof(SpareParts));
    }

    // ─── IoT / SENSOR ────────────────────────────────────────────────────────
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
    public async Task<IActionResult> SimulateSensor(Guid equipmentId)
    {
        await _service.SimulateSensorDataAsync(equipmentId);
        TempData["SuccessMessage"] = "Đã cập nhật dữ liệu cảm biến (giả lập).";
        return RedirectToAction(nameof(SensorMonitor), new { equipmentId });
    }

    [HttpGet]
    [AllowAnonymous]
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
