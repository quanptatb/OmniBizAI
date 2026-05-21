using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class WorkflowController : Controller
{
    private readonly WorkKanbanService _kanban;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public WorkflowController(WorkKanbanService kanban, NotificationService notif, ITenantContext tenant)
    {
        _kanban = kanban;
        _notif = notif;
        _tenant = tenant;
    }

    // ── Kanban Board ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Kanban(string? search, Guid? dept)
    {
        var vm = await _kanban.GetBoardAsync(search, dept);
        return View(vm);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkItemCreateViewModel input, string? search, Guid? dept)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin công việc chưa hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search, dept });
        }

        var result = await _kanban.CreateAsync(input);
        if (result.Success && input.OrganizationUnitId.HasValue)
        {
            await _notif.SendToDepartmentAsync(
                $"🎯 {_tenant.UserFullName} tạo công việc mới",
                $"{_tenant.UserFullName} đã tạo thẻ công việc \"{input.Title}\" trên Kanban.",
                input.OrganizationUnitId.Value, "WorkItem", null);
        }
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept });
    }

    // ── Move ──────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(WorkItemMoveViewModel input)
    {
        if (input.Id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Thông tin Kanban không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept });
        }

        (bool Success, string Message) result = (false, "Lỗi xử lý.");
        if (input.ColumnId != Guid.Empty)
        {
            result = await _kanban.MoveToColumnAsync(input.Id, input.ColumnId);
        }
        else if (Enum.IsDefined(typeof(WorkItemStatus), input.Status))
        {
            result = await _kanban.MoveAsync(input.Id, input.Status);
        }
        else
        {
            TempData["ErrorMessage"] = "Trạng thái Kanban không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept });
        }

        if (result.Success)
        {
            await _notif.SendToManagersAsync(
                $"📋 {_tenant.UserFullName} di chuyển công việc",
                $"{_tenant.UserFullName} đã di chuyển một thẻ Kanban.",
                "WorkItem", input.Id);
        }
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept });
    }

    // ── Details ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _kanban.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Edit ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _kanban.GetEditFormAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WorkItemEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _kanban.GetEditFormAsync(vm.Id);
            if (form == null) return NotFound();
            vm.Departments = form.Departments;
            vm.Assignees = form.Assignees;
            vm.StatusOptions = form.StatusOptions;
            return View(vm);
        }

        var result = await _kanban.UpdateAsync(vm);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _kanban.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban));
    }

    // ── Add Comment ───────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid workItemId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Nội dung bình luận không được trống.";
            return RedirectToAction(nameof(Details), new { id = workItemId });
        }
        var result = await _kanban.AddCommentAsync(workItemId, content);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    // ── Checklist CRUD ────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChecklist(Guid workItemId, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Tiêu đề checklist không được trống.";
            return RedirectToAction(nameof(Details), new { id = workItemId });
        }
        var result = await _kanban.AddChecklistAsync(workItemId, title);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleChecklist(Guid checklistId, Guid workItemId)
    {
        var result = await _kanban.ToggleChecklistAsync(checklistId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChecklist(Guid checklistId, Guid workItemId)
    {
        var result = await _kanban.DeleteChecklistAsync(checklistId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    // ── Kanban Column CRUD ──────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateColumn(string title, string? accentColor, string? search, Guid? dept)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Tiêu đề cột không được để trống.";
            return RedirectToAction(nameof(Kanban), new { search, dept });
        }
        var result = await _kanban.CreateColumnAsync(title, accentColor);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept });
    }

    [HttpPost]
    public async Task<IActionResult> RenameColumn([FromBody] RenameColumnRequest request)
    {
        if (request == null || request.ColumnId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title))
        {
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }
        var result = await _kanban.RenameColumnAsync(request.ColumnId, request.Title);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteColumn(Guid columnId, string? search, Guid? dept)
    {
        var result = await _kanban.DeleteColumnAsync(columnId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveColumn(Guid columnId, string direction, string? search, Guid? dept)
    {
        if (direction != "left" && direction != "right")
        {
            TempData["ErrorMessage"] = "Hướng di chuyển không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search, dept });
        }
        var result = await _kanban.MoveColumnAsync(columnId, direction);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept });
    }
}
