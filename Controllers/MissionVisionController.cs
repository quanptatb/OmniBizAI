using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class MissionVisionController(MissionVisionService mvService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = await mvService.GetListAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MissionVisionCreateViewModel vm)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Dữ liệu không hợp lệ."; return RedirectToAction(nameof(Index)); }
        await mvService.CreateAsync(vm);
        TempData["Success"] = "Đã tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await mvService.GetEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MissionVisionEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await mvService.UpdateAsync(vm)) { TempData["Error"] = "Cập nhật thất bại."; return View(vm); }
        TempData["Success"] = "Cập nhật thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await mvService.ToggleAsync(id);
        TempData["Success"] = "Cập nhật trạng thái thành công.";
        return RedirectToAction(nameof(Index));
    }
}
