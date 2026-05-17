using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
public class GoodsIssueController : Controller
{
    private readonly InventoryService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;
    public GoodsIssueController(InventoryService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? type)
    {
        var vm = await _service.GetGoodsIssuesAsync(search, status, type);
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetGoodsIssueCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsIssueCreateViewModel vm)
    {
        if (!ModelState.IsValid || !vm.Lines.Any(l => l.IssuedQuantity > 0))
        {
            var form = await _service.GetGoodsIssueCreateFormAsync();
            vm.OperationRequests = form.OperationRequests;
            vm.Customers = form.Customers;
            vm.Departments = form.Departments;
            vm.Products = form.Products;
            if (!vm.Lines.Any(l => l.IssuedQuantity > 0))
                ModelState.AddModelError("", "Phải nhập số lượng xuất cho ít nhất một mục hàng.");
            return View(vm);
        }
        var id = await _service.CreateGoodsIssueAsync(vm);
        await _notif.SendToManagersAsync(
            $"📦 {_tenant.UserFullName} tạo phiếu xuất kho",
            $"{_tenant.UserFullName} đã tạo phiếu xuất kho mới.",
            "GoodsIssue", id);
        TempData["SuccessMessage"] = "Tạo phiếu xuất kho thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetGoodsIssueDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var ok = await _service.ConfirmGoodsIssueAsync(id);
        if (ok)
            await _notif.BroadcastAsync($"✅ {_tenant.UserFullName} xác nhận xuất kho", $"{_tenant.UserFullName} đã xác nhận phiếu xuất kho.", "GoodsIssue", id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã xác nhận xuất kho." : "Không thể xác nhận.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ok = await _service.CancelGoodsIssueAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã hủy phiếu xuất kho." : "Không thể hủy.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
