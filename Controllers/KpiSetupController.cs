using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class KpiSetupController(KpiManagementService kpiService) : Controller
{
    public async Task<IActionResult> Index(string? search, string? status, string? periodId, string? ownerType)
    {
        var vm = await kpiService.GetListAsync(search, status, periodId, ownerType);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await kpiService.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await kpiService.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KpiCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await kpiService.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.OkrObjectives = form.OkrObjectives;
            vm.Periods = form.Periods;
            return View(vm);
        }
        var id = await kpiService.CreateAsync(vm);
        TempData["Success"] = "Đã tạo KPI thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
