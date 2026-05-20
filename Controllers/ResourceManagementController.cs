using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class ResourceManagementController : Controller
{
    private readonly ResourceManagementService _service;

    public ResourceManagementController(ResourceManagementService service)
    {
        _service = service;
    }

    // ─── DASHBOARD ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var vm = await _service.GetDashboardAsync();
        return View(vm);
    }

    // ─── EQUIPMENT ──────────────────────────────────────────────────────────
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

    // ─── WORK SHIFTS ────────────────────────────────────────────────────────
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

    // ─── CERTIFICATES ────────────────────────────────────────────────────────
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

    // ─── WORKSPACES ──────────────────────────────────────────────────────────
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
