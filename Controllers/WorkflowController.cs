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

    public WorkflowController(WorkKanbanService kanban)
    {
        _kanban = kanban;
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
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Kanban), new { search = input.Search, dept = input.Dept });
    }
}
