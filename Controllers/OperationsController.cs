using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OperationsController : Controller
{
    private readonly OperationRequestService _service;

    public OperationsController(OperationRequestService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? priority, Guid? dept, int page = 1)
    {
        var vm = await _service.GetListAsync(search, status, priority, dept, page);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Create(OperationRequestCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            return View(vm);
        }

        if (vm.DueDate.HasValue && vm.DueDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("DueDate", "Hạn xử lý không được nhỏ hơn ngày hôm nay.");
            var form = await _service.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            return View(vm);
        }

        var id = await _service.CreateAsync(vm);
        TempData["SuccessMessage"] = "Tạo yêu cầu thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var success = await _service.SubmitAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Yêu cầu đã được gửi duyệt."
            : "Không thể gửi duyệt yêu cầu này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var success = await _service.CancelAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Yêu cầu đã bị hủy."
            : "Không thể hủy yêu cầu này.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
