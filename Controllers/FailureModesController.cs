using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class FailureModesController : Controller
{
    private readonly FailureModeService _service;

    public FailureModesController(FailureModeService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? search, FailureModeCategory? category, bool? activeOnly)
    {
        var items = await _service.GetListAsync(search, category, activeOnly);
        ViewBag.Search = search;
        ViewBag.CategoryFilter = category;
        ViewBag.ActiveOnly = activeOnly;
        return View(items);
    }

    public IActionResult Create() => View(new FailureModeEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FailureModeEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var (success, _, message) = await _service.CreateAsync(vm);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        if (!success) return View(vm);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Statistics(int months = 6)
    {
        var vm = await _service.GetStatisticsAsync(months);
        return View(vm);
    }
}
