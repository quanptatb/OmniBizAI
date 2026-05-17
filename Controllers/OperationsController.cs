using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OperationsController : Controller
{
    private readonly OperationRequestService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public OperationsController(OperationRequestService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? priority, Guid? dept, int page = 1)
    {
        var vm = await _service.GetListAsync(search, status, priority, dept, page);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Create(OperationRequestCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            return View(vm);
        }

        if (vm.DueDate.HasValue && vm.DueDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("DueDate", "Hạn xử lý không được nhỏ hơn ngày hôm nay.");
            var form = await _service.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            return View(vm);
        }

        var id = await _service.CreateAsync(vm);

        // Notify managers about new operation request
        await _notif.SendToManagersAsync(
            $"📋 {_tenant.UserFullName} tạo yêu cầu mới",
            $"{_tenant.UserFullName} đã tạo yêu cầu vận hành \"{vm.Title}\" (ưu tiên: {vm.Priority})",
            "OperationRequest", id);

        TempData["SuccessMessage"] = "Tạo yêu cầu thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Edit(OperationRequestEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetEditFormAsync(vm.Id);
            if (form is null) return NotFound();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            return View(vm);
        }

        var success = await _service.UpdateAsync(vm);
        if (!success)
        {
            TempData["ErrorMessage"] = "Không thể cập nhật yêu cầu này.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        await _notif.SendToManagersAsync(
            $"📝 {_tenant.UserFullName} cập nhật yêu cầu",
            $"{_tenant.UserFullName} đã cập nhật yêu cầu vận hành \"{vm.Title}\".",
            "OperationRequest", vm.Id);

        TempData["SuccessMessage"] = "Cập nhật yêu cầu thành công!";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var success = await _service.SubmitAsync(id);
        if (success)
        {
            await _notif.SendToManagersAsync(
                $"📤 {_tenant.UserFullName} gửi yêu cầu chờ duyệt",
                $"{_tenant.UserFullName} đã gửi yêu cầu vận hành #{id.ToString()[..8]} để phê duyệt.",
                "OperationRequest", id);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Yêu cầu đã được gửi duyệt."
            : "Không thể gửi duyệt yêu cầu này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var success = await _service.CancelAsync(id);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"🚫 {_tenant.UserFullName} hủy yêu cầu",
                $"{_tenant.UserFullName} đã hủy yêu cầu vận hành #{id.ToString()[..8]}.",
                "OperationRequest", id);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Yêu cầu đã bị hủy."
            : "Không thể hủy yêu cầu này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã xóa yêu cầu."
            : "Không thể xóa yêu cầu này.";
        return RedirectToAction(nameof(Index));
    }
}

