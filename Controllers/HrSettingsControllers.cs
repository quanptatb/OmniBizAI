using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class EmployeesController : Controller
{
    private readonly HrService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public EmployeesController(HrService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Dashboard()
    {
        var vm = await _service.GetDashboardAsync();
        return View(vm);
    }

    public async Task<IActionResult> Index(string? search, Guid? dept)
    {
        var vm = await _service.GetEmployeesAsync(search, dept);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetEmployeeDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Create() => View(await _service.GetEmployeeCreateFormAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Departments = (await _service.GetEmployeeCreateFormAsync()).Departments; return View(vm); }
        var id = await _service.CreateEmployeeAsync(vm);
        await _notif.SendToManagersAsync($"👤 {_tenant.UserFullName} thêm nhân viên", $"{_tenant.UserFullName} đã thêm nhân viên {vm.FullName} ({vm.EmployeeCode}).", "Employee", id);
        TempData["SuccessMessage"] = "Thêm nhân viên thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetEmployeeEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeEditViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Departments = (await _service.GetEmployeeEditFormAsync(vm.ProfileId))?.Departments ?? new(); return View(vm); }
        if (!await _service.UpdateEmployeeAsync(vm)) { TempData["ErrorMessage"] = "Không thể cập nhật."; return View(vm); }
        await _notif.SendToManagersAsync($"📝 {_tenant.UserFullName} cập nhật nhân viên", $"{_tenant.UserFullName} đã cập nhật hồ sơ {vm.FullName}.", "Employee", vm.ProfileId);
        TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.ProfileId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        if (!await _service.DeactivateEmployeeAsync(id)) { TempData["ErrorMessage"] = "Không thể vô hiệu hóa."; }
        else { await _notif.SendToManagersAsync($"🚫 {_tenant.UserFullName} vô hiệu hóa NV", $"{_tenant.UserFullName} đã chuyển trạng thái nghỉ việc cho một nhân viên.", "Employee", id); TempData["SuccessMessage"] = "Đã vô hiệu hóa nhân viên."; }
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> AddContract(Guid id)
    {
        var vm = await _service.GetAddContractFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddContract(AddContractViewModel vm)
    {
        if (!ModelState.IsValid) { vm.EmployeeName = (await _service.GetAddContractFormAsync(vm.EmployeeProfileId))?.EmployeeName ?? ""; return View(vm); }
        if (!await _service.AddContractAsync(vm)) { TempData["ErrorMessage"] = "Không thể thêm hợp đồng."; }
        else { TempData["SuccessMessage"] = "Thêm hợp đồng thành công."; }
        return RedirectToAction(nameof(Details), new { id = vm.EmployeeProfileId });
    }
}

[Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
public class PositionsController : Controller
{
    private readonly HrService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public PositionsController(HrService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _service.GetPositionsAsync();
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetPositionCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PositionCreateViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Departments = (await _service.GetPositionCreateFormAsync()).Departments; return View(vm); }
        await _service.CreatePositionAsync(vm);
        TempData["SuccessMessage"] = "Tạo chức vụ thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetPositionEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PositionEditViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Departments = (await _service.GetPositionEditFormAsync(vm.Id))?.Departments ?? new(); return View(vm); }
        if (!await _service.UpdatePositionAsync(vm)) { TempData["ErrorMessage"] = "Cập nhật thất bại."; return View(vm); }
        TempData["SuccessMessage"] = "Cập nhật chức vụ thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _service.DeletePositionAsync(id)) TempData["ErrorMessage"] = "Xóa thất bại.";
        else TempData["SuccessMessage"] = "Đã xóa chức vụ.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
public class SettingsController : Controller
{
    private readonly SettingsService _service;
    public SettingsController(SettingsService service) => _service = service;

    public async Task<IActionResult> Company()
    {
        var vm = await _service.GetCompanySettingsAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Company(CompanySettingsViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var ok = await _service.SaveCompanySettingsAsync(vm);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Cập nhật thành công." : "Cập nhật thất bại.";
        return RedirectToAction(nameof(Company));
    }

    public async Task<IActionResult> Modules()
    {
        var vm = await _service.GetModulesAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleModule(Guid id)
    {
        await _service.ToggleModuleAsync(id);
        TempData["SuccessMessage"] = "Cập nhật module thành công.";
        return RedirectToAction(nameof(Modules));
    }

    public async Task<IActionResult> Parameters()
    {
        var vm = await _service.GetParametersAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateParameter(Guid id, string value)
    {
        await _service.UpdateParameterAsync(id, value);
        TempData["SuccessMessage"] = "Cập nhật tham số thành công.";
        return RedirectToAction(nameof(Parameters));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateParameter(string group, string key, string? value, string valueType)
    {
        await _service.CreateParameterAsync(group, key, value, valueType);
        TempData["SuccessMessage"] = "Tạo tham số mới thành công.";
        return RedirectToAction(nameof(Parameters));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteParameter(Guid id)
    {
        await _service.DeleteParameterAsync(id);
        TempData["SuccessMessage"] = "Đã xóa tham số.";
        return RedirectToAction(nameof(Parameters));
    }

    // ── Enum Label Management ────────────────────────────────────────────────
    public async Task<IActionResult> EnumLabels()
    {
        var vm = await _service.GetEnumLabelsAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEnumLabels(IFormCollection form)
    {
        var labels = new Dictionary<string, string>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("label.") && !string.IsNullOrWhiteSpace(form[key]))
            {
                var fullKey = key[6..]; // remove "label." prefix
                labels[fullKey] = form[key].ToString();
            }
        }
        await _service.SaveEnumLabelsAsync(labels);
        TempData["SuccessMessage"] = $"Đã lưu {labels.Count} nhãn tùy chỉnh.";
        return RedirectToAction(nameof(EnumLabels));
    }

    // ── Appearance / Theme ───────────────────────────────────────────────────
    public async Task<IActionResult> Appearance()
    {
        var vm = await _service.GetThemeAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Appearance(ThemeSettingsViewModel vm)
    {
        await _service.SaveThemeAsync(vm);
        TempData["SuccessMessage"] = "Cập nhật giao diện thành công! Tải lại trang để áp dụng.";
        return RedirectToAction(nameof(Appearance));
    }

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> ThemeCss()
    {
        try { var css = await _service.GetThemeCssAsync(); return Content(css, "text/css"); }
        catch { return Content("/* no theme */", "text/css"); }
    }
}
