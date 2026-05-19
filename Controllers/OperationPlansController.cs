using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN,STAFF")]
public class OperationPlansController : Controller
{
    private readonly OperationPlanService _service;

    public OperationPlansController(OperationPlanService service)
    {
        _service = service;
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

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public IActionResult Create()
    {
        return View(new OperationPlanCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Create(OperationPlanCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var id = await _service.CreatePlanAsync(vm);
        TempData["SuccessMessage"] = "Khởi tạo Kế hoạch thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AddTask(Guid id)
    {
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

        if (await _service.CreateTaskAsync(vm))
        {
            TempData["SuccessMessage"] = "Đã phân công công việc mới.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể thêm công việc.";
        }

        return RedirectToAction(nameof(Details), new { id = vm.PlanId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(Guid id)
    {
        var analysis = await _service.AnalyzePlanWithAiAsync(id);
        TempData["AiAnalysis"] = analysis;
        return RedirectToAction(nameof(Details), new { id });
    }
}
