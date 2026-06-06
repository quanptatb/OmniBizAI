using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class SparePartRequisitionsController : Controller
{
    private const string ManagerRoles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN";

    private readonly SparePartRequisitionService _service;

    public SparePartRequisitionsController(SparePartRequisitionService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(SparePartRequisitionStatus? status)
        => View(await _service.GetListAsync(status));

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Create(Guid? linkedWorkOrderId)
        => View(await _service.GetCreateFormAsync(linkedWorkOrderId));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SparePartRequisitionFormViewModel vm)
    {
        var (success, id, message) = await _service.CreateAsync(vm);
        if (!success)
        {
            TempData["ErrorMessage"] = message;
            var form = await _service.GetCreateFormAsync(vm.LinkedWorkOrderId);
            vm.Parts = form.Parts;
            vm.WorkOrders = form.WorkOrders;
            return View(vm);
        }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var (success, message) = await _service.SubmitAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Approve(Guid id, string? note)
    {
        var (success, message) = await _service.ApproveAsync(id, note);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var (success, message) = await _service.RejectAsync(id, reason);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> Issue(Guid id)
    {
        var (success, message) = await _service.IssueAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (success, message) = await _service.CancelAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = ManagerRoles)]
    public async Task<IActionResult> AutoReorder()
    {
        var count = await _service.GenerateAutoReorderDraftsAsync();
        TempData["SuccessMessage"] = count > 0
            ? $"Đã tạo phiếu Draft tự động cho {count} phụ tùng dưới ngưỡng."
            : "Không có phụ tùng nào dưới ngưỡng (hoặc đã có phiếu pending).";
        return RedirectToAction(nameof(Index));
    }
}
