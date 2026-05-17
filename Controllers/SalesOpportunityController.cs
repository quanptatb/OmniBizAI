using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class SalesOpportunityController : Controller
{
    private readonly CrmService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public SalesOpportunityController(CrmService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? search, string? stage, string? temp, Guid? customer)
    {
        var vm = await _service.GetOpportunitiesAsync(search, stage, temp, customer);
        return View(vm);
    }

    public async Task<IActionResult> Create(Guid? customerId)
    {
        var vm = await _service.GetOpportunityCreateFormAsync(customerId);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOpportunityCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetOpportunityCreateFormAsync(vm.CustomerId);
            vm.Customers = form.Customers; vm.Users = form.Users; vm.Contacts = form.Contacts;
            return View(vm);
        }
        var id = await _service.CreateOpportunityAsync(vm);
        TempData["SuccessMessage"] = "Tạo cơ hội bán hàng thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetOpportunityDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetOpportunityEditFormAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SalesOpportunityEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetOpportunityEditFormAsync(vm.Id);
            if (form != null) { vm.Contacts = form.Contacts; vm.Users = form.Users; vm.CustomerName = form.CustomerName; }
            return View(vm);
        }
        if (!await _service.UpdateOpportunityAsync(vm)) { TempData["ErrorMessage"] = "Không thể cập nhật."; return View(vm); }
        TempData["SuccessMessage"] = "Cập nhật cơ hội thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStage(ChangeStageViewModel vm)
    {
        var (success, msg) = await _service.ChangeStageAsync(vm);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = msg;
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN,DEPARTMENT_MANAGER")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _service.DeleteOpportunityAsync(id)) { TempData["ErrorMessage"] = "Không thể xóa."; }
        else { TempData["SuccessMessage"] = "Đã xóa cơ hội."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts(Guid customerId)
    {
        var contacts = await _service.GetContactsForCustomerAsync(customerId);
        return Json(contacts);
    }
}
