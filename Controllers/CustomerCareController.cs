using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class CustomerCareController : Controller
{
    private readonly CrmService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public CustomerCareController(CrmService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

    // ── List ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, string? type, string? status, Guid? customer)
    {
        var vm = await _service.GetInteractionsAsync(search, type, status, customer);
        return View(vm);
    }

    // ── Create GET ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Create(Guid? customerId)
    {
        var vm = await _service.GetInteractionCreateFormAsync(customerId);
        return View(vm);
    }

    // ── Create POST ───────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCareCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetInteractionCreateFormAsync(vm.CustomerId);
            vm.Customers = form.Customers; vm.Users = form.Users; vm.Contacts = form.Contacts;
            return View(vm);
        }
        var id = await _service.CreateInteractionAsync(vm);
        await _notif.SendToManagersAsync(
            $"📞 {_tenant.UserFullName} tạo tương tác CSKH",
            $"{_tenant.UserFullName} đã tạo tương tác: {vm.Subject}",
            "CrmInteraction", id);
        TempData["SuccessMessage"] = "Tạo tương tác CSKH thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Details ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetInteractionDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Edit GET ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetInteractionEditFormAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Edit POST ─────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerCareEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetInteractionEditFormAsync(vm.Id);
            if (form != null) { vm.Contacts = form.Contacts; vm.Users = form.Users; vm.CustomerName = form.CustomerName; }
            return View(vm);
        }
        if (!await _service.UpdateInteractionAsync(vm)) { TempData["ErrorMessage"] = "Không thể cập nhật."; return View(vm); }
        TempData["SuccessMessage"] = "Cập nhật tương tác thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // ── Complete ──────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteInteractionViewModel vm)
    {
        if (!await _service.CompleteInteractionAsync(vm)) { TempData["ErrorMessage"] = "Không thể hoàn thành."; }
        else { TempData["SuccessMessage"] = "Đã hoàn thành tương tác."; }
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // ── Start ─────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid id)
    {
        if (!await _service.StartInteractionAsync(id)) { TempData["ErrorMessage"] = "Không thể bắt đầu."; }
        else { TempData["SuccessMessage"] = "Đã bắt đầu tương tác."; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Cancel ─────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (!await _service.CancelInteractionAsync(id)) { TempData["ErrorMessage"] = "Không thể hủy."; }
        else { TempData["SuccessMessage"] = "Đã hủy tương tác."; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Delete ─────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN,DEPARTMENT_MANAGER")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _service.DeleteInteractionAsync(id)) { TempData["ErrorMessage"] = "Không thể xóa."; }
        else { TempData["SuccessMessage"] = "Đã xóa tương tác."; }
        return RedirectToAction(nameof(Index));
    }

    // ── AJAX: Get contacts for customer ──────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetContacts(Guid customerId)
    {
        var contacts = await _service.GetContactsForCustomerAsync(customerId);
        return Json(contacts);
    }
}
