using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class WorkOrdersController : Controller
{
    private const string TechnicianRoles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN";
    private const string ManagerRoles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN";

    private readonly WorkOrderService _service;

    public WorkOrdersController(WorkOrderService service)
    {
        _service = service;
    }

    // ── INDEX ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(WorkOrderStatus? status, Guid? equipmentId, Guid? technicianId)
    {
        var vm = await _service.GetListAsync(status, equipmentId, technicianId);
        return View(vm);
    }

    // ── DETAILS ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Mobile-friendly view (F5.8) - same data, simpler responsive layout
    public async Task<IActionResult> Mobile(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── CREATE ───────────────────────────────────────────────────────────────
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Create(Guid? incidentId, Guid? pmScheduleId)
    {
        var vm = await _service.GetCreateFormAsync(incidentId, pmScheduleId);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Create(WorkOrderCreateFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateFormAsync(vm.IncidentId, vm.PmScheduleId);
            vm.Equipments = form.Equipments;
            vm.Technicians = form.Technicians;
            return View(vm);
        }
        var (success, id, message) = await _service.CreateAsync(vm);
        if (!success)
        {
            TempData["ErrorMessage"] = message;
            var form = await _service.GetCreateFormAsync(vm.IncidentId, vm.PmScheduleId);
            vm.Equipments = form.Equipments;
            vm.Technicians = form.Technicians;
            return View(vm);
        }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── AUTO-GEN FROM INCIDENT/PM ────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> CreateFromIncident(Guid incidentId)
    {
        var id = await _service.CreateFromIncidentAsync(incidentId);
        if (id == null)
        {
            TempData["ErrorMessage"] = "Không tạo được Work Order.";
            return RedirectToAction("IncidentDetail", "Maintenance", new { id = incidentId });
        }
        TempData["SuccessMessage"] = "Đã tạo Work Order từ sự cố.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> CreateFromPm(Guid pmScheduleId)
    {
        var id = await _service.CreateFromPmScheduleAsync(pmScheduleId);
        if (id == null)
        {
            TempData["ErrorMessage"] = "Không tạo được Work Order.";
            return RedirectToAction("PmSchedules", "Maintenance");
        }
        TempData["SuccessMessage"] = "Đã tạo Work Order từ PM Schedule.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── TRANSITIONS ──────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Assign(Guid id, Guid technicianId)
    {
        var (success, message) = await _service.AssignAsync(id, technicianId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = TechnicianRoles)]
    public async Task<IActionResult> Start(Guid id)
    {
        var (success, message) = await _service.StartAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = TechnicianRoles)]
    public async Task<IActionResult> Hold(Guid id)
    {
        var (success, message) = await _service.HoldAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Cancel(Guid id, string? reason)
    {
        var (success, message) = await _service.CancelAsync(id, reason);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = TechnicianRoles)]
    public async Task<IActionResult> Complete(WorkOrderCompleteViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin.";
            return RedirectToAction(nameof(Details), new { id = vm.WorkOrderId });
        }
        var (success, message) = await _service.CompleteAsync(vm.WorkOrderId, vm);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id = vm.WorkOrderId });
    }

    // ── CHECKLIST ────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = TechnicianRoles)]
    public async Task<IActionResult> ToggleChecklist(Guid id, Guid itemId, bool completed, bool mobile = false)
    {
        var (success, message) = await _service.ToggleChecklistAsync(id, itemId, completed);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(mobile ? nameof(Mobile) : nameof(Details), new { id });
    }

    // ── PART USAGE (F5.2) ────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = TechnicianRoles)]
    public async Task<IActionResult> AddPart(Guid id, Guid sparePartId, int quantity)
    {
        var (success, message) = await _service.AddPartUsageAsync(id, sparePartId, quantity);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = TechnicianRoles)]
    public async Task<IActionResult> RemovePart(Guid id, Guid usageId)
    {
        var (success, message) = await _service.RemovePartUsageAsync(usageId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
