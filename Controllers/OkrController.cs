using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OkrController(OkrService okrService) : Controller
{
    public async Task<IActionResult> Index(string? search, string? level, string? status)
    {
        var vm = await okrService.GetListAsync(search, level, status);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await okrService.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await okrService.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OkrCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await okrService.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Missions = form.Missions;
            return View(vm);
        }
        var id = await okrService.CreateAsync(vm);
        TempData["Success"] = "Đã tạo OKR thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
