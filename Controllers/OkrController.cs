using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OkrController(OkrService okrService, OkrProgressService progressService, NotificationService notif, ITenantContext tenant) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var dashService = HttpContext.RequestServices.GetRequiredService<KpiOkrDashboardService>();
        var vm = await dashService.GetDashboardAsync();
        return View(vm);
    }

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OkrCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await okrService.GetCreateFormAsync();
            vm.Departments = form.Departments; vm.Missions = form.Missions;
            return View(vm);
        }
        var id = await okrService.CreateAsync(vm);
        await notif.SendToManagersAsync($"🎯 {tenant.UserFullName} tạo OKR", $"Tạo OKR: {vm.ObjectiveName}", "OkrObjective", id);
        TempData["Success"] = "Đã tạo OKR thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await okrService.GetEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OkrEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await okrService.UpdateAsync(vm)) { TempData["Error"] = "Cập nhật thất bại."; return View(vm); }
        TempData["Success"] = "Cập nhật OKR thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        if (await okrService.ActivateAsync(id))
        {
            await notif.BroadcastAsync($"✅ OKR được kích hoạt", $"{tenant.UserFullName} kích hoạt OKR.", "OkrObjective", id);
            TempData["Success"] = "Đã kích hoạt OKR.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id)
    {
        if (await okrService.CloseAsync(id)) TempData["Success"] = "Đã đóng OKR.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateKeyResult(UpdateKrProgressViewModel vm)
    {
        if (await okrService.UpdateKeyResultAsync(vm))
        {
            await progressService.RecalculateAsync(vm.OkrId);
            TempData["Success"] = "Đã cập nhật tiến độ KR.";
        }
        return RedirectToAction(nameof(Details), new { id = vm.OkrId });
    }
}
