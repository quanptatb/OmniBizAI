using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN,STAFF")]
public class OperationPlansController : Controller
{
    private readonly OperationPlanService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public OperationPlansController(OperationPlanService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index()
    {
        var plans = await _service.GetPlansAsync();
        return View(plans);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var plan = await _service.GetPlanDetailAsync(id);
        if (plan == null) return NotFound();
        return View(plan);
    }

    public async Task<IActionResult> Gantt(Guid id)
    {
        var plan = await _service.GetPlanGanttAsync(id);
        if (plan == null) return NotFound();
        return View(plan);
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public IActionResult Create()
    {
        return View(new OperationPlanCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Create(OperationPlanCreateViewModel vm)
    {
        if (vm.StartDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(vm.StartDate), "Ngày bắt đầu không được nhỏ hơn hôm nay.");
        if (vm.EndDate.Date < vm.StartDate.Date)
            ModelState.AddModelError(nameof(vm.EndDate), "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

        if (!ModelState.IsValid) return View(vm);
        var id = await _service.CreatePlanAsync(vm);
        TempData["SuccessMessage"] = "Khởi tạo Kế hoạch thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AddTask(Guid id)
    {
        var plan = await _service.GetPlanDetailAsync(id);
        if (plan == null) return NotFound();
        if (!plan.CanAddTasks)
        {
            TempData["ErrorMessage"] = "Chỉ được thêm công việc khi kế hoạch còn ở trạng thái nháp.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var form = await _service.GetCreateTaskFormAsync(id);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AddTask(PlanTaskCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateTaskFormAsync(vm.PlanId);
            vm.Users = form.Users;
            vm.Equipments = form.Equipments;
            return View(vm);
        }

        var (success, message) = await _service.CreateTaskAsync(vm);
        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = vm.PlanId });
        }
        else
        {
            TempData["ErrorMessage"] = message;
            var form = await _service.GetCreateTaskFormAsync(vm.PlanId);
            vm.Users = form.Users;
            vm.Equipments = form.Equipments;
            return View(vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> UpdateTask(
        Guid id,
        Guid taskId,
        PlanTaskStatus status,
        int progressPercent,
        DateTime? actualStartTime,
        DateTime? actualEndTime,
        decimal? unitsProduced,
        decimal? unitsGood)
    {
        var (success, message) = await _service.UpdateTaskStatusAsync(
            id,
            taskId,
            status,
            progressPercent,
            actualStartTime,
            actualEndTime,
            unitsProduced,
            unitsGood);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> ChangeTaskPlan(
        Guid id,
        Guid taskId,
        DateTime startTime,
        DateTime endTime,
        Guid? assignedUserId,
        Guid? equipmentId,
        string reason)
    {
        var (success, message) = await _service.ApplyTaskChangeOrderAsync(
            id,
            taskId,
            startTime,
            endTime,
            assignedUserId,
            equipmentId,
            reason);

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> UpdateGanttTask(Guid id, Guid taskId, DateTime startTime, DateTime endTime)
    {
        var (success, message) = await _service.UpdateTaskScheduleFromGanttAsync(id, taskId, startTime, endTime);
        return Json(new { success, message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AddDependency(
        Guid id,
        Guid predecessorTaskId,
        Guid successorTaskId,
        PlanTaskDependencyType type)
    {
        var (success, message) = await _service.AddTaskDependencyAsync(id, predecessorTaskId, successorTaskId, type);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> DeleteDependency(Guid id, Guid dependencyId)
    {
        var (success, message) = await _service.DeleteTaskDependencyAsync(id, dependencyId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(Guid id)
    {
        var analysis = await _service.AnalyzePlanWithAiAsync(id);
        TempData["AiAnalysis"] = analysis;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var (success, message) = await _service.SubmitPlanAsync(id);
        if (success)
        {
            await _notif.SendToManagersAsync(
                $"{_tenant.UserFullName} gửi kế hoạch chờ duyệt",
                $"{_tenant.UserFullName} đã gửi kế hoạch vận hành #{id.ToString()[..8]} để phê duyệt.",
                "OperationPlan",
                id);
        }

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Approve(Guid id, string? note)
    {
        var (success, message) = await _service.ApprovePlanAsync(id, note);
        if (success)
        {
            await _notif.SendToManagersAsync(
                $"{_tenant.UserFullName} phê duyệt kế hoạch",
                $"{_tenant.UserFullName} đã phê duyệt kế hoạch vận hành #{id.ToString()[..8]}.",
                "OperationPlan",
                id);
        }

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Start(Guid id)
    {
        var (success, message) = await _service.StartPlanAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var (success, message) = await _service.CompletePlanAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (success, message) = await _service.CancelPlanAsync(id);
        if (success)
        {
            await _notif.SendToManagersAsync(
                $"{_tenant.UserFullName} hủy kế hoạch",
                $"{_tenant.UserFullName} đã hủy kế hoạch vận hành #{id.ToString()[..8]}.",
                "OperationPlan",
                id);
        }

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
