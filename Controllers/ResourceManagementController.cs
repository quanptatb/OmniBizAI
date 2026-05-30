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
public class ResourceManagementController : Controller
{
    private readonly ResourceManagementService _service;
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public ResourceManagementController(ResourceManagementService service, ApplicationDbContext db, ITenantContext tenant)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
    }

    // ─── DASHBOARD (Theirs) ──────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var vm = new ResourceManagementDashboardViewModel
        {
            Human = new HumanResourceOverviewViewModel
            {
                TotalEmployees = await _db.EmployeeProfiles.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                OnLeaveToday = await _db.LeaveRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == LeaveStatus.Approved && x.StartDate <= today && x.EndDate >= today),
                PendingLeaves = await _db.LeaveRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == LeaveStatus.Submitted),
                AvgPerformanceScore = await _db.EvaluationResults.Where(x => x.TenantId == tid && !x.IsDeleted).Select(x => x.TotalScore).AverageAsync() ?? 0
            },
            Equipment = new EquipmentResourceOverviewViewModel
            {
                TotalMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance"),
                ActiveMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance" && (x.Status == OperationStatus.Submitted || x.Status == OperationStatus.InProgress)),
                CompletedMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance" && x.Status == OperationStatus.Completed),
                OverdueMaintenanceRequests = await _db.OperationRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Type == "Maintenance" && x.DueDate.HasValue && x.DueDate < today && x.Status != OperationStatus.Completed)
            },
            Inventory = new InventoryResourceOverviewViewModel
            {
                TotalProducts = await _db.ProductServices.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                ActiveStockAlerts = await _db.StockAlerts.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == StockAlertStatus.Active),
                GoodsReceiptCount = await _db.GoodsReceipts.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                GoodsIssueCount = await _db.GoodsIssues.CountAsync(x => x.TenantId == tid && !x.IsDeleted)
            },
            Infrastructure = new InfrastructureResourceOverviewViewModel
            {
                TotalDepartments = await _db.OrganizationUnits.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                TotalPositions = await _db.Positions.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                TotalWorkCalendars = await _db.WorkCalendars.CountAsync(x => x.TenantId == tid && !x.IsDeleted),
                TotalCustomerSites = await _db.CustomerSites.CountAsync(x => x.TenantId == tid && !x.IsDeleted)
            }
        };

        return View(vm);
    }

    // ─── EQUIPMENT (Ours) ────────────────────────────────────────────────────
    public async Task<IActionResult> Equipments(string? search, string? status, string? type)
    {
        var items = await _service.GetEquipmentsAsync(search, status, type);
        ViewBag.Search = search;
        ViewBag.StatusFilter = status;
        ViewBag.TypeFilter = type;
        return View(items);
    }

    public async Task<IActionResult> EquipmentDetail(Guid id)
    {
        var vm = await _service.GetEquipmentDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public IActionResult CreateEquipment()
    {
        return View(new EquipmentCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreateEquipment(EquipmentCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var id = await _service.CreateEquipmentAsync(vm);
        TempData["SuccessMessage"] = "Thêm thiết bị thành công.";
        return RedirectToAction(nameof(EquipmentDetail), new { id });
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> ScheduleMaintenance(Guid equipmentId)
    {
        var tid = HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;
        var db = HttpContext.RequestServices.GetRequiredService<OmniBizAI.Data.ApplicationDbContext>();
        var technicians = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
        );
        return View(new ScheduleMaintenanceViewModel { EquipmentId = equipmentId, Technicians = technicians });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> ScheduleMaintenance(ScheduleMaintenanceViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _service.ScheduleMaintenanceAsync(vm))
            TempData["SuccessMessage"] = "Đã lên lịch bảo trì thành công.";
        return RedirectToAction(nameof(EquipmentDetail), new { id = vm.EquipmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CompleteMaintenance(CompleteMaintenanceViewModel vm)
    {
        if (await _service.CompleteMaintenanceAsync(vm))
            TempData["SuccessMessage"] = "Xác nhận hoàn thành bảo trì.";
        return RedirectToAction(nameof(Equipments));
    }

    // ─── WORK SHIFTS (Ours) ──────────────────────────────────────────────────
    public async Task<IActionResult> WorkShifts()
    {
        var shifts = await _service.GetShiftsAsync();
        return View(shifts);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public IActionResult CreateShift()
    {
        return View(new WorkShiftCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreateShift(WorkShiftCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.CreateShiftAsync(vm);
        TempData["SuccessMessage"] = "Tạo ca làm việc thành công.";
        return RedirectToAction(nameof(WorkShifts));
    }

    public async Task<IActionResult> ShiftSchedule(string? date)
    {
        DateOnly? d = null;
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsed))
            d = parsed;
        var vm = await _service.GetShiftScheduleAsync(d);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AssignShift(AssignShiftViewModel vm)
    {
        await _service.AssignShiftAsync(vm);
        TempData["SuccessMessage"] = "Phân công ca làm việc thành công.";
        return RedirectToAction(nameof(ShiftSchedule), new { date = vm.WorkDate.ToString("yyyy-MM-dd") });
    }

    // ─── CERTIFICATES (Ours) ──────────────────────────────────────────────────
    public async Task<IActionResult> Certificates(string? search, string? category, bool? expiredOnly)
    {
        var items = await _service.GetCertificatesAsync(search, category, expiredOnly);
        ViewBag.Search = search;
        ViewBag.CategoryFilter = category;
        ViewBag.ExpiredOnly = expiredOnly;
        return View(items);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN,HR_MANAGER")]
    public async Task<IActionResult> AddCertificate()
    {
        var form = await _service.GetCertificateCreateFormAsync();
        return View(new CertificateCreateViewModel { Users = form.Users });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN,HR_MANAGER")]
    public async Task<IActionResult> AddCertificate(CertificateCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCertificateCreateFormAsync();
            vm.Users = form.Users;
            return View(vm);
        }
        await _service.AddCertificateAsync(vm);
        TempData["SuccessMessage"] = "Thêm chứng chỉ thành công.";
        return RedirectToAction(nameof(Certificates));
    }

    // ─── WORKSPACES (Ours) ────────────────────────────────────────────────────
    public async Task<IActionResult> Workspaces(string? search, string? type)
    {
        var items = await _service.GetWorkspacesAsync(search, type);
        ViewBag.Search = search;
        ViewBag.TypeFilter = type;
        return View(items);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreateWorkspace()
    {
        var tid = HttpContext.RequestServices.GetRequiredService<ITenantContext>().TenantId;
        var db = HttpContext.RequestServices.GetRequiredService<OmniBizAI.Data.ApplicationDbContext>();
        var parents = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            db.Workspaces.Where(w => w.TenantId == tid && !w.IsDeleted && w.Status == "Active")
                .OrderBy(w => w.Name)
                .Select(w => new SelectOption { Value = w.Id.ToString(), Text = w.Name })
        );
        return View(new WorkspaceCreateViewModel { ParentWorkspaces = parents });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> CreateWorkspace(WorkspaceCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.CreateWorkspaceAsync(vm);
        TempData["SuccessMessage"] = "Tạo khu vực làm việc thành công.";
        return RedirectToAction(nameof(Workspaces));
    }
}
