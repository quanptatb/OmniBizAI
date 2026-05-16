using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class EmployeesController : Controller
{
    private readonly HrService _service;
    public EmployeesController(HrService service) => _service = service;

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
}

[Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
public class PositionsController : Controller
{
    private readonly HrService _service;
    public PositionsController(HrService service) => _service = service;

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
        if (!ModelState.IsValid)
        {
            var form = await _service.GetPositionCreateFormAsync();
            vm.Departments = form.Departments;
            return View(vm);
        }
        await _service.CreatePositionAsync(vm);
        TempData["SuccessMessage"] = "Tạo chức vụ thành công.";
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
}
