using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class KpiSetupController(
    KpiManagementService kpiService,
    ApplicationDbContext db,
    ITenantContext tenant,
    NotificationService notif,
    MeetingSummaryImportService meetingImportService) : Controller
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

    [HttpGet]
    public async Task<IActionResult> ImportMeetingSummary()
    {
        var vm = await meetingImportService.GetFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportMeetingSummary(MeetingSummaryImportViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm = await meetingImportService.PopulateLookupAsync(vm);
            return View(vm);
        }

        vm = await meetingImportService.AnalyzeAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CommitImportedMeetingSummary(MeetingSummaryImportCommitViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Preview import không hợp lệ. Vui lòng phân tích lại summary cuộc họp.";
            return RedirectToAction(nameof(ImportMeetingSummary));
        }

        try
        {
            var result = await meetingImportService.CommitAsync(vm);
            await notif.SendToManagersAsync(
                $"🧠 {tenant.UserFullName} import OKR/KPI từ cuộc họp",
                $"{tenant.UserFullName} đã import 1 OKR và {result.KpiIds.Count} KPI từ biên bản cuộc họp ({result.ParseMode}).",
                "OkrObjective",
                result.OkrId);

            TempData["SuccessMessage"] = $"Đã import thành công 1 OKR và {result.KpiIds.Count} KPI từ summary cuộc họp.";
            return RedirectToAction("Details", "Okr", new { id = result.OkrId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(ImportMeetingSummary));
        }
    }

    [HttpGet]
    public async Task<IActionResult> KeyResults(Guid okrObjectiveId)
    {
        var items = await db.OkrKeyResults
            .Where(kr => kr.TenantId == tenant.TenantId && !kr.IsDeleted && kr.OkrObjectiveId == okrObjectiveId)
            .OrderBy(kr => kr.KeyResultName)
            .Select(kr => new { value = kr.Id, text = kr.KeyResultName })
            .ToListAsync();
        return Json(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KpiCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await kpiService.GetCreateFormAsync();
            vm.Departments = form.Departments; vm.OkrObjectives = form.OkrObjectives;
            vm.OkrKeyResults = form.OkrKeyResults; vm.Periods = form.Periods;
            return View(vm);
        }
        try
        {
            var id = await kpiService.CreateAsync(vm);
            await notif.SendToManagersAsync($"📊 {tenant.UserFullName} tạo KPI", $"Tạo KPI: {vm.Name}", "KpiDefinition", id);
            TempData["Success"] = "Đã tạo KPI thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.OkrKeyResultId), ex.Message);
            var form = await kpiService.GetCreateFormAsync();
            vm.Departments = form.Departments; vm.OkrObjectives = form.OkrObjectives;
            vm.OkrKeyResults = form.OkrKeyResults; vm.Periods = form.Periods;
            return View(vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        if (await kpiService.ActivateAsync(id))
        {
            await notif.BroadcastAsync($"✅ KPI được kích hoạt", $"{tenant.UserFullName} kích hoạt KPI.", "KpiDefinition", id);
            TempData["Success"] = "Đã kích hoạt KPI.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id)
    {
        if (await kpiService.CloseAsync(id)) TempData["Success"] = "Đã đóng KPI.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
