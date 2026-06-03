using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OmniBizAI.Hubs;
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
    private readonly IHubContext<KanbanHub> _kanbanHub;

    public WorkflowController(WorkKanbanService kanban, NotificationService notif, ITenantContext tenant, IHubContext<KanbanHub> kanbanHub)
    {
        _kanban = kanban;
        _notif = notif;
        _tenant = tenant;
        _kanbanHub = kanbanHub;
    }

    // ── Kanban Board ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Kanban(
        string? search,
        Guid? dept,
        Guid? sprint,
        Guid? assignedTo,
        PriorityLevel? priority,
        Guid? tag,
        DateOnly? dueFrom,
        DateOnly? dueTo,
        bool hasAttachment,
        string? quick,
        Guid? savedViewId)
    {
        var vm = await _kanban.GetBoardAsync(search, dept, sprint, assignedTo, priority, tag, dueFrom, dueTo, hasAttachment, quick, savedViewId);
        return View(vm);
    }

    public async Task<IActionResult> Analytics(DateOnly? from, DateOnly? to)
    {
        var vm = await _kanban.GetAnalyticsAsync(from, to);
        return View(vm);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkItemCreateViewModel input, string? search, Guid? dept, Guid? sprint)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin công việc chưa hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
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
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
    }

    // ── Move ──────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(WorkItemMoveViewModel input)
    {
        if (input.Id == Guid.Empty)
        {
            if (IsAjaxRequest()) return Json(new { success = false, message = "Thông tin Kanban không hợp lệ." });
            TempData["ErrorMessage"] = "Thông tin Kanban không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept, sprint = input.Sprint });
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
            if (IsAjaxRequest()) return Json(new { success = false, message = "Trạng thái Kanban không hợp lệ." });
            TempData["ErrorMessage"] = "Trạng thái Kanban không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept, sprint = input.Sprint });
        }

        if (result.Success)
        {
            await _kanbanHub.Clients
                .Group(KanbanHub.TenantBoardGroup(_tenant.TenantId))
                .SendAsync("WorkItemMoved", new
                {
                    workItemId = input.Id,
                    columnId = input.ColumnId,
                    movedBy = _tenant.UserFullName,
                    message = result.Message,
                    sourceClientId = input.ClientId
                });
            await _notif.SendToManagersAsync(
                $"📋 {_tenant.UserFullName} di chuyển công việc",
                $"{_tenant.UserFullName} đã di chuyển một thẻ Kanban.",
                "WorkItem", input.Id);
        }

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                workItemId = input.Id,
                columnId = input.ColumnId
            });
        }

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept, sprint = input.Sprint });
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
            vm.ColumnOptions = form.ColumnOptions;
            vm.SprintOptions = form.SprintOptions;
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
    public async Task<IActionResult> AddChecklist(Guid workItemId, string title, Guid? assignedToUserId, DateOnly? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Tiêu đề checklist không được trống.";
            return RedirectToAction(nameof(Details), new { id = workItemId });
        }
        var result = await _kanban.AddChecklistAsync(workItemId, title, assignedToUserId, dueDate);
        if (result.Success && result.AssignedToUserId.HasValue)
        {
            await _notif.SendAsync(
                $"📌 {_tenant.UserFullName} giao checklist",
                $"{_tenant.UserFullName} đã giao checklist \"{result.ChecklistTitle}\" trong công việc \"{result.WorkItemTitle}\".",
                "WorkItem", workItemId, result.AssignedToUserId.Value);
        }
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateChecklist(Guid checklistId, Guid workItemId, string title, Guid? assignedToUserId, DateOnly? dueDate, int sortOrder)
    {
        var result = await _kanban.UpdateChecklistAsync(checklistId, workItemId, title, assignedToUserId, dueDate, sortOrder);
        if (result.Success && result.AssignedToUserId.HasValue)
        {
            await _notif.SendAsync(
                $"📌 {_tenant.UserFullName} giao checklist",
                $"{_tenant.UserFullName} đã giao checklist \"{result.ChecklistTitle}\" trong công việc \"{result.WorkItemTitle}\".",
                "WorkItem", workItemId, result.AssignedToUserId.Value);
        }
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

    // ── Dependencies ──────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDependency(Guid workItemId, Guid blockerId, WorkItemDependencyType type)
    {
        var result = await _kanban.AddDependencyAsync(workItemId, blockerId, type);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDependency(Guid dependencyId, Guid workItemId)
    {
        var result = await _kanban.DeleteDependencyAsync(dependencyId, workItemId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = workItemId });
    }

    // ── Sprint / Iteration ────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSprint(WorkflowSprintCreateViewModel input, string? search, Guid? dept, Guid? sprint)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin sprint chưa hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
        }

        var result = await _kanban.CreateSprintAsync(input);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint = result.Success ? result.SprintId : sprint });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSprintStatus(Guid sprintId, SprintStatus status, string? search, Guid? dept, Guid? sprint)
    {
        var result = await _kanban.UpdateSprintStatusAsync(sprintId, status);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint = sprint ?? sprintId });
    }

    // ── Saved Kanban views ───────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveKanbanView(
        string name,
        string? search,
        Guid? dept,
        Guid? sprint,
        Guid? assignedTo,
        PriorityLevel? priority,
        Guid? tag,
        DateOnly? dueFrom,
        DateOnly? dueTo,
        bool hasAttachment,
        string? quick)
    {
        var result = await _kanban.SaveKanbanViewAsync(name, search, dept, sprint, assignedTo, priority, tag, dueFrom, dueTo, hasAttachment, quick);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint, assignedTo, priority, tag, dueFrom, dueTo, hasAttachment, quick });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteKanbanView(Guid id)
    {
        var result = await _kanban.DeleteKanbanViewAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban));
    }

    // ── Kanban Column CRUD ──────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateColumn(string title, string? accentColor, int? wipLimit, bool wipEnforced, string? search, Guid? dept, Guid? sprint)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Tiêu đề cột không được để trống.";
            return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
        }
        var result = await _kanban.CreateColumnAsync(title, accentColor, wipLimit, wipEnforced);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateColumnWip(Guid columnId, int? wipLimit, bool wipEnforced, string? search, Guid? dept, Guid? sprint)
    {
        var result = await _kanban.UpdateColumnWipLimitAsync(columnId, wipLimit, wipEnforced);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
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
    public async Task<IActionResult> DeleteColumn(Guid columnId, string? search, Guid? dept, Guid? sprint)
    {
        var result = await _kanban.DeleteColumnAsync(columnId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveColumn(Guid columnId, string direction, string? search, Guid? dept, Guid? sprint)
    {
        if (direction != "left" && direction != "right")
        {
            TempData["ErrorMessage"] = "Hướng di chuyển không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
        }
        var result = await _kanban.MoveColumnAsync(columnId, direction);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept, sprint });
    }

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || Request.Headers.Accept.Any(h => h?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
