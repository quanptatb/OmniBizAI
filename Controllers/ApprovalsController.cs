using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class ApprovalsController : Controller
{
    private readonly ApprovalService _service;

    public ApprovalsController(ApprovalService service)
    {
        _service = service;
    }

    public async Task<IActionResult> MyTasks()
    {
        var vm = await _service.GetMyTasksAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid taskId, string? note)
    {
        var success = await _service.ApproveAsync(taskId, note);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã phê duyệt thành công."
            : "Không thể phê duyệt yêu cầu này.";
        return RedirectToAction(nameof(MyTasks));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid taskId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối (tối thiểu 10 ký tự).";
            return RedirectToAction(nameof(MyTasks));
        }

        var success = await _service.RejectAsync(taskId, reason);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã từ chối yêu cầu."
            : "Không thể từ chối yêu cầu này.";
        return RedirectToAction(nameof(MyTasks));
    }
}
