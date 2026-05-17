using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class KpiCheckInController(KpiCheckInService checkInService) : Controller
{
    // ── Index ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, string? reviewStatus, int page = 1)
    {
        var vm = await checkInService.GetListAsync(search, reviewStatus, page);
        return View(vm);
    }

    // ── Details ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await checkInService.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Edit ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await checkInService.GetEditFormAsync(id);
        if (vm == null)
        {
            TempData["Error"] = "Check-in không tồn tại hoặc đã được review.";
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(KpiCheckInEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await checkInService.GetEditFormAsync(vm.Id);
            if (form != null) { vm.FailReasons = form.FailReasons; vm.KpiName = form.KpiName; vm.KpiCode = form.KpiCode; vm.UserName = form.UserName; vm.CheckInDate = form.CheckInDate; }
            return View(vm);
        }
        var (success, message) = await checkInService.UpdateCheckInAsync(vm);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, message) = await checkInService.DeleteCheckInAsync(id);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    // ── Submit ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
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

    // ── Review ────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(KpiCheckInReviewViewModel vm)
    {
        var decision = vm.Decision == "Approved" ? CheckInReviewStatus.Approved : CheckInReviewStatus.Rejected;
        var (success, message) = await checkInService.ReviewCheckInAsync(vm.CheckInId, decision, vm.Comment, vm.Score);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Details), new { id = vm.CheckInId });
    }
}
