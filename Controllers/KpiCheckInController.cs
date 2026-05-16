using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class KpiCheckInController(KpiCheckInService checkInService) : Controller
{
    public async Task<IActionResult> Index(string? search, string? reviewStatus, int page = 1)
    {
        var vm = await checkInService.GetListAsync(search, reviewStatus, page);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(KpiCheckInSubmitViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        var (success, message) = await checkInService.SubmitCheckInAsync(vm);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(KpiCheckInReviewViewModel vm)
    {
        var decision = vm.Decision == "Approved" ? CheckInReviewStatus.Approved : CheckInReviewStatus.Rejected;
        var (success, message) = await checkInService.ReviewCheckInAsync(vm.CheckInId, decision, vm.Comment, vm.Score);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }
}
