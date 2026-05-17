using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class LeaveController : Controller
{
    private readonly HrService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public LeaveController(HrService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var vm = await _service.GetLeaveListAsync(status);
        return View(vm);
    }

    public IActionResult Create() => View(new LeaveCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var id = await _service.CreateLeaveAsync(vm);
            await _notif.SendToManagersAsync($"📋 {_tenant.UserFullName} xin nghỉ phép", $"{_tenant.UserFullName} đã gửi đơn xin nghỉ phép {vm.StartDate:dd/MM} - {vm.EndDate:dd/MM}.", "LeaveRequest", id);
            TempData["SuccessMessage"] = "Gửi đơn nghỉ phép thành công.";
        }
        catch (InvalidOperationException)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ nhân viên của bạn.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (!await _service.ApproveLeaveAsync(id)) { TempData["ErrorMessage"] = "Không thể duyệt."; }
        else { await _notif.BroadcastAsync($"✅ {_tenant.UserFullName} duyệt nghỉ phép", $"{_tenant.UserFullName} đã duyệt đơn nghỉ phép.", "LeaveRequest", id); TempData["SuccessMessage"] = "Đã duyệt nghỉ phép."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Reject(Guid id, string? reason)
    {
        if (!await _service.RejectLeaveAsync(id, reason)) { TempData["ErrorMessage"] = "Không thể từ chối."; }
        else { await _notif.BroadcastAsync($"❌ {_tenant.UserFullName} từ chối nghỉ phép", $"{_tenant.UserFullName} đã từ chối đơn nghỉ phép. Lý do: {reason ?? "N/A"}", "LeaveRequest", id); TempData["SuccessMessage"] = "Đã từ chối."; }
        return RedirectToAction(nameof(Index));
    }
}
