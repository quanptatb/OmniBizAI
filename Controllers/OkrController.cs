using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;

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
            vm.Employees = form.Employees;
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

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (await okrService.DeleteOkrAsync(id))
            TempData["Success"] = "Đã xóa OKR.";
        else
            TempData["Error"] = "Không thể xóa OKR này.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddKeyResult(Guid okrId, string keyResultName, string? unit, decimal targetValue, bool isInverse)
    {
        if (string.IsNullOrWhiteSpace(keyResultName))
        {
            TempData["Error"] = "Tên Key Result không được để trống.";
            return RedirectToAction(nameof(Details), new { id = okrId });
        }
        var result = await okrService.AddKeyResultAsync(okrId, keyResultName, unit, targetValue, isInverse);
        if (result)
        {
            TempData["Success"] = "Đã thêm Key Result.";
        }
        else
        {
            TempData["Error"] = "Thêm Key Result thất bại.";
        }
        return RedirectToAction(nameof(Details), new { id = okrId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllocations(Guid id, [FromServices] ApplicationDbContext db)
    {
        var okr = await db.OkrObjectives
            .Include(o => o.DepartmentAllocations)
            .Include(o => o.EmployeeAllocations)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenant.TenantId && !o.IsDeleted);

        if (okr == null) return NotFound();

        var tid = tenant.TenantId;
        var allDepartments = await db.OrganizationUnits
            .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
            .Select(o => new { id = o.Id, name = o.Name })
            .ToListAsync();

        var allEmployees = await db.AppUsers
            .Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == Models.Entities.Enums.UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new { id = u.Id, name = u.FullName })
            .ToListAsync();

        var selectedDepartmentIds = okr.DepartmentAllocations.Select(d => d.OrganizationUnitId).ToList();
        var selectedEmployeeIds = okr.EmployeeAllocations.Select(e => e.UserId).ToList();

        return Json(new {
            success = true,
            departments = allDepartments,
            employees = allEmployees,
            selectedDepartmentIds = selectedDepartmentIds,
            selectedEmployeeIds = selectedEmployeeIds
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAllocations(Guid okrId, List<Guid> departmentIds, List<Guid> employeeIds)
    {
        var result = await okrService.UpdateAllocationsAsync(okrId, departmentIds, employeeIds);
        if (result)
            TempData["Success"] = "Đã cập nhật phân bổ thành công.";
        else
            TempData["Error"] = "Cập nhật phân bổ thất bại.";

        return RedirectToAction(nameof(Details), new { id = okrId });
    }

    [HttpPost]
    public async Task<IActionResult> SuggestKeyResults(Guid id, [FromServices] ApplicationDbContext db, [FromServices] GeminiService gemini)
    {
        var okr = await db.OkrObjectives
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenant.TenantId && !o.IsDeleted);
            
        if (okr == null) return NotFound();

        var systemPrompt = "Bạn là chuyên gia về OKR (Objectives and Key Results). Hãy gợi ý từ 3 đến 5 Key Results (kết quả then chốt) đo lường được cho Objective (mục tiêu) được cung cấp. Phản hồi hoàn toàn bằng định dạng JSON với cấu trúc: {\"keyResults\": [{\"name\": \"Tên Key Result\", \"unit\": \"Đơn vị đo (vd: %, VNĐ, khách hàng, sản phẩm, giờ...)\", \"targetValue\": 100, \"isInverse\": false}]}. Lưu ý: targetValue phải là số lớn hơn 0, isInverse là true nếu giá trị thực tế thấp hơn là tốt (ví dụ: giảm chi phí, giảm tỷ lệ lỗi). Trả về duy nhất dữ liệu dạng JSON, không kèm giải thích, không bọc trong tag markdown code block.";
        var userPrompt = $"Objective: {okr.ObjectiveName}\nCấp độ: {okr.Level.ToString()}\nChu kỳ: {okr.Cycle ?? "Năm"}";

        var response = await gemini.GenerateAsync(systemPrompt, userPrompt, 0.4, 2000);
        if (!response.Success)
        {
            return Json(new { success = false, message = response.ErrorMessage });
        }

        try
        {
            var jsonText = response.Text.Trim();
            
            // Clean markdown code blocks if any
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
                if (jsonText.EndsWith("```"))
                {
                    jsonText = jsonText.Substring(0, jsonText.Length - 3);
                }
            }
            else if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
                if (jsonText.EndsWith("```"))
                {
                    jsonText = jsonText.Substring(0, jsonText.Length - 3);
                }
            }
            jsonText = jsonText.Trim();

            var result = JsonSerializer.Deserialize<GeminiKrSuggestion>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Json(new { success = true, keyResults = result?.KeyResults ?? new List<SuggestedKeyResult>() });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi phân tích gợi ý từ AI: " + ex.Message, raw = response.Text });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSuggestedKeyResults(Guid okrId, List<SuggestedKeyResult> keyResults)
    {
        if (keyResults == null || !keyResults.Any())
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một Key Result.";
            return RedirectToAction(nameof(Details), new { id = okrId });
        }

        foreach (var kr in keyResults)
        {
            await okrService.AddKeyResultAsync(okrId, kr.Name, kr.Unit, kr.TargetValue, kr.IsInverse);
        }

        TempData["Success"] = "Đã lưu các gợi ý Key Result thành công.";
        return RedirectToAction(nameof(Details), new { id = okrId });
    }
}

public class GeminiKrSuggestion
{
    public List<SuggestedKeyResult> KeyResults { get; set; } = new();
}

public class SuggestedKeyResult
{
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal TargetValue { get; set; }
    public bool IsInverse { get; set; }
}

