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
    public async Task<IActionResult> Create(string? type = null)
    {
        var vm = await _service.GetCreateFormAsync();
        if (!string.IsNullOrWhiteSpace(type)) vm.Type = type;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> StartWork(Guid id)
    {
        var success = await _service.StartWorkAsync(id);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"🔧 {_tenant.UserFullName} bắt đầu xử lý",
                $"{_tenant.UserFullName} đã bắt đầu xử lý yêu cầu #{id.ToString()[..8]}.",
                "OperationRequest", id);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã bắt đầu xử lý yêu cầu."
            : "Không thể bắt đầu xử lý.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var success = await _service.CompleteAsync(id);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"✅ {_tenant.UserFullName} hoàn thành yêu cầu",
                $"{_tenant.UserFullName} đã hoàn thành yêu cầu #{id.ToString()[..8]}.",
                "OperationRequest", id);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã hoàn thành yêu cầu."
            : "Không thể hoàn thành.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(Guid requestId, OrderLineInputViewModel input)
    {
        await _service.AddLineAsync(requestId, input);
        TempData["SuccessMessage"] = "Đã thêm mục hàng.";
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLine(Guid lineId, Guid requestId)
    {
        await _service.RemoveLineAsync(lineId);
        TempData["SuccessMessage"] = "Đã xóa mục hàng.";
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid requestId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Nội dung bình luận không được để trống.";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        var success = await _service.AddCommentAsync(requestId, content);
        if (success)
        {
            TempData["SuccessMessage"] = "Đã thêm bình luận.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể thêm bình luận.";
        }
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    public async Task<IActionResult> Statistics()
    {
        var vm = await _service.GetStatisticsAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Hold(Guid id)
    {
        var success = await _service.HoldAsync(id);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"⏸️ {_tenant.UserFullName} tạm dừng yêu cầu",
                $"{_tenant.UserFullName} đã tạm dừng yêu cầu #{id.ToString()[..8]}.",
                "OperationRequest", id);
            TempData["SuccessMessage"] = "Đã tạm dừng yêu cầu xử lý.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể tạm dừng yêu cầu này.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Resume(Guid id)
    {
        var success = await _service.ResumeAsync(id);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"▶️ {_tenant.UserFullName} tiếp tục yêu cầu",
                $"{_tenant.UserFullName} đã tiếp tục yêu cầu #{id.ToString()[..8]}.",
                "OperationRequest", id);
            TempData["SuccessMessage"] = "Đã tiếp tục xử lý yêu cầu.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể tiếp tục xử lý yêu cầu này.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Reopen(Guid id)
    {
        var success = await _service.ReopenAsync(id);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"🔄 {_tenant.UserFullName} mở lại yêu cầu",
                $"{_tenant.UserFullName} đã mở lại yêu cầu #{id.ToString()[..8]}.",
                "OperationRequest", id);
            TempData["SuccessMessage"] = "Đã mở lại yêu cầu xử lý.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể mở lại yêu cầu này.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}

