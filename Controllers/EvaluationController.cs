using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class EvaluationController(EvaluationService evalService, NotificationService notif, ITenantContext tenant) : Controller
{
    public async Task<IActionResult> Index(string? periodId)
    {
        var vm = await evalService.GetListAsync(periodId);
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await evalService.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EvaluationCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await evalService.GetCreateFormAsync();
            vm.Users = form.Users; vm.Periods = form.Periods;
            return View(vm);
        }
        var id = await evalService.CreateAsync(vm);
        await notif.SendToManagersAsync($"📋 {tenant.UserFullName} tạo đánh giá", $"Tạo đánh giá hiệu suất mới.", "EvaluationResult", id);
        TempData["Success"] = "Đã tạo đánh giá thành công.";
        return RedirectToAction(nameof(Index));
    }
}
