using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
public class CashBookController : Controller
{
    private readonly CashBookService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;
    public CashBookController(CashBookService service, NotificationService notif, ITenantContext tenant)
    { _service = service; _notif = notif; _tenant = tenant; }

    public async Task<IActionResult> Index(string? search, string? type, string? status, string? category, DateOnly? from, DateOnly? to)
    {
        var vm = await _service.GetDashboardAsync(search, type, status, category, from, to);
        return View(vm);
    }

    public async Task<IActionResult> Create(string? type)
    {
        var vm = await _service.GetCreateFormAsync();
        if (!string.IsNullOrEmpty(type)) vm.TransactionType = type;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashTransactionCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateFormAsync();
            vm.Customers = form.Customers; vm.Vendors = form.Vendors; vm.Budgets = form.Budgets; vm.Departments = form.Departments;
            return View(vm);
        }
        var id = await _service.CreateAsync(vm);
        var icon = vm.TransactionType == "Income" ? "📥" : "📤";
        await _notif.SendToManagersAsync($"{icon} {_tenant.UserFullName} ghi nhận {(vm.TransactionType == "Income" ? "thu" : "chi")}", $"{_tenant.UserFullName} ghi nhận {vm.Amount:N0} ₫ — {vm.Description}", "CashTransaction", id);
        TempData["SuccessMessage"] = "Ghi nhận giao dịch thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var ok = await _service.ApproveAsync(id);
        if (ok) await _notif.BroadcastAsync($"✅ {_tenant.UserFullName} duyệt giao dịch", $"{_tenant.UserFullName} đã duyệt một giao dịch thu chi.", "CashTransaction", id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã duyệt giao dịch." : "Không thể duyệt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Reject(Guid id, string? reason)
    {
        var ok = await _service.RejectAsync(id, reason);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã từ chối giao dịch." : "Không thể từ chối.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Void(Guid id)
    {
        var ok = await _service.VoidAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã hủy giao dịch." : "Không thể hủy.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
