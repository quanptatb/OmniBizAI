using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class ApprovalsController : Controller
{
    private readonly ApprovalService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public ApprovalsController(ApprovalService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    // ── List ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> MyTasks(string? search, string? status)
    {
        var vm = await _service.GetMyTasksAsync(search, status);
        return View(vm);
    }

    // ── Details ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Approve ───────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid taskId, string? note, bool fromDetail = false)
    {
        var success = await _service.ApproveAsync(taskId, note);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"✅ {_tenant.UserFullName} đã phê duyệt yêu cầu",
                $"{_tenant.UserFullName} đã phê duyệt yêu cầu.{(note != null ? $" Ghi chú: {note}" : "")}",
                "ApprovalTask", taskId);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã phê duyệt thành công."
            : "Không thể phê duyệt yêu cầu này.";
        return fromDetail ? RedirectToAction(nameof(Details), new { id = taskId }) : RedirectToAction(nameof(MyTasks));
    }

    // ── Reject ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid taskId, string reason, bool fromDetail = false)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối (tối thiểu 10 ký tự).";
            return fromDetail ? RedirectToAction(nameof(Details), new { id = taskId }) : RedirectToAction(nameof(MyTasks));
        }

        var success = await _service.RejectAsync(taskId, reason);
        if (success)
        {
            await _notif.BroadcastAsync(
                $"❌ {_tenant.UserFullName} từ chối yêu cầu",
                $"{_tenant.UserFullName} đã từ chối yêu cầu. Lý do: {reason}",
                "ApprovalTask", taskId);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã từ chối yêu cầu."
            : "Không thể từ chối yêu cầu này.";
        return fromDetail ? RedirectToAction(nameof(Details), new { id = taskId }) : RedirectToAction(nameof(MyTasks));
    }

    // ── Reassign ──────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(Guid taskId, Guid newUserId)
    {
        var (success, message) = await _service.ReassignAsync(taskId, newUserId);
        if (success)
        {
            await _notif.SendAsync(
                $"📋 {_tenant.UserFullName} chuyển giao phê duyệt",
                $"{_tenant.UserFullName} đã chuyển giao một yêu cầu phê duyệt cho bạn.",
                "ApprovalTask", taskId, newUserId);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id = taskId });
    }

    // ── Return for Revision ──────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnForRevision(Guid taskId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do trả lại (tối thiểu 10 ký tự).";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }
        var (success, message) = await _service.ReturnForRevisionAsync(taskId, reason);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(MyTasks));
    }

    // ── Bulk Approve (F6.4) ──────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkApprove(List<Guid> selectedTaskIds, string? note)
    {
        if (selectedTaskIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Chưa chọn yêu cầu nào.";
            return RedirectToAction(nameof(MyTasks));
        }
        var result = await _service.BulkApproveAsync(selectedTaskIds, note);
        if (result.SuccessCount > 0)
        {
            await _notif.BroadcastAsync(
                $"✅ {_tenant.UserFullName} phê duyệt hàng loạt ({result.SuccessCount})",
                $"{_tenant.UserFullName} đã phê duyệt {result.SuccessCount} yêu cầu.{(note != null ? $" Ghi chú: {note}" : "")}");
        }
        TempData[result.Success ? "SuccessMessage" : "ToastWarningMessage"] = result.Message;
        return RedirectToAction(nameof(MyTasks));
    }

    // ── Bulk Reject (F6.4) ───────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkReject(List<Guid> selectedTaskIds, string reason)
    {
        if (selectedTaskIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Chưa chọn yêu cầu nào.";
            return RedirectToAction(nameof(MyTasks));
        }
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối (tối thiểu 10 ký tự).";
            return RedirectToAction(nameof(MyTasks));
        }
        var result = await _service.BulkRejectAsync(selectedTaskIds, reason);
        if (result.SuccessCount > 0)
        {
            await _notif.BroadcastAsync(
                $"❌ {_tenant.UserFullName} từ chối hàng loạt ({result.SuccessCount})",
                $"{_tenant.UserFullName} đã từ chối {result.SuccessCount} yêu cầu. Lý do: {reason}");
        }
        TempData[result.Success ? "SuccessMessage" : "ToastWarningMessage"] = result.Message;
        return RedirectToAction(nameof(MyTasks));
    }
}
