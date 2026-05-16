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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MissionVisionCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        await mvService.CreateAsync(vm);
        TempData["Success"] = "Đã tạo thành công.";
        return RedirectToAction(nameof(Index));
    }
}
