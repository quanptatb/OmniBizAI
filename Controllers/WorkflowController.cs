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

    public async Task<IActionResult> Kanban(string? search, Guid? dept)
    {
        var vm = await _kanban.GetBoardAsync(search, dept);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkItemCreateViewModel input, string? search, Guid? dept)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin công việc chưa hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search, dept });
        }

        var result = await _kanban.CreateAsync(input);
        if (result.Success)
        {
            // Notify department about new work item
            if (input.OrganizationUnitId.HasValue)
            {
                await _notif.SendToDepartmentAsync(
                    $"🎯 {_tenant.UserFullName} tạo công việc mới",
                    $"{_tenant.UserFullName} đã tạo thẻ công việc \"{input.Title}\" trên Kanban.",
                    input.OrganizationUnitId.Value, "WorkItem", null);
            }
        }
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search, dept });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(WorkItemMoveViewModel input)
    {
        if (input.Id == Guid.Empty || !Enum.IsDefined(typeof(WorkItemStatus), input.Status))
        {
            TempData["ErrorMessage"] = "Trạng thái Kanban không hợp lệ.";
            return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept });
        }

        var result = await _kanban.MoveAsync(input.Id, input.Status);
        if (result.Success)
        {
            var statusLabel = input.Status switch
            {
                WorkItemStatus.Todo => "Cần làm",
                WorkItemStatus.InProgress => "Đang xử lý",
                WorkItemStatus.Blocked => "Đang vướng",
                WorkItemStatus.Done => "Hoàn thành",
                WorkItemStatus.Cancelled => "Đã hủy",
                _ => input.Status.ToString()
            };
            await _notif.SendToManagersAsync(
                $"📋 {_tenant.UserFullName} di chuyển công việc",
                $"{_tenant.UserFullName} đã chuyển một thẻ Kanban sang trạng thái \"{statusLabel}\".",
                "WorkItem", input.Id);
        }
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept });
    }
}

