using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class ProcurementController : Controller
{
    private readonly ProcurementService _service;
    public ProcurementController(ProcurementService service) => _service = service;

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var vm = await _service.GetListAsync(search, status);
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProcurementCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Products = form.Products;
            return View(vm);
        }
        var id = await _service.CreateAsync(vm);
        TempData["SuccessMessage"] = "Tạo đề xuất mua sắm thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var ok = await _service.SubmitAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã gửi duyệt." : "Không thể gửi duyệt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ok = await _service.CancelAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã hủy." : "Không thể hủy.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
public class PurchaseOrdersController : Controller
{
    private readonly ProcurementService _service;
    public PurchaseOrdersController(ProcurementService service) => _service = service;

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var vm = await _service.GetPurchaseOrdersAsync(search, status);
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetPOCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetPOCreateFormAsync();
            vm.Vendors = form.Vendors;
            vm.ProcurementRequests = form.ProcurementRequests;
            vm.Products = form.Products;
            return View(vm);
        }
        var id = await _service.CreatePOAsync(vm);
        TempData["SuccessMessage"] = "Tạo đơn mua hàng thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetPurchaseOrderDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }
}

[Authorize(Roles = "ACCOUNTANT,DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN")]
public class ExpensesController : Controller
{
    private readonly ProcurementService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public ExpensesController(ProcurementService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? status, Guid? budget)
    {
        var vm = await _service.GetExpensesAsync(status, budget);
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetExpenseCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetExpenseCreateFormAsync();
            vm.Budgets = form.Budgets;
            return View(vm);
        }
        await _service.CreateExpenseAsync(vm);
        await _notif.SendToManagersAsync($"💸 {_tenant.UserFullName} ghi nhận chi phí", $"{_tenant.UserFullName} đã ghi nhận khoản chi {vm.Amount:N0} ₫.", "Expense", null);
        TempData["SuccessMessage"] = "Ghi nhận chi phí thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (!await _service.ApproveExpenseAsync(id)) { TempData["ErrorMessage"] = "Không thể duyệt chi phí."; }
        else { await _notif.BroadcastAsync($"✅ {_tenant.UserFullName} duyệt chi phí", $"{_tenant.UserFullName} đã duyệt một khoản chi phí.", "Expense", id); TempData["SuccessMessage"] = "Đã duyệt chi phí thành công."; }
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
public class GoodsReceiptController : Controller
{
    private readonly ProcurementService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;
    public GoodsReceiptController(ProcurementService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var vm = await _service.GetGoodsReceiptsAsync(search, status);
        return View(vm);
    }

    public async Task<IActionResult> Create(Guid? poId)
    {
        var vm = await _service.GetGoodsReceiptCreateFormAsync(poId);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsReceiptCreateViewModel vm)
    {
        if (!ModelState.IsValid || !vm.Lines.Any(l => l.ReceivedQuantity > 0))
        {
            var form = await _service.GetGoodsReceiptCreateFormAsync(vm.PurchaseOrderId);
            vm.PurchaseOrders = form.PurchaseOrders;
            if (!vm.Lines.Any(l => l.ReceivedQuantity > 0))
                ModelState.AddModelError("", "Phải nhập số lượng nhận cho ít nhất một mục hàng.");
            return View(vm);
        }
        var id = await _service.CreateGoodsReceiptAsync(vm);
        await _notif.SendToManagersAsync(
            $"📦 {_tenant.UserFullName} tạo phiếu nhập kho",
            $"{_tenant.UserFullName} đã tạo phiếu nhập kho mới.",
            "GoodsReceipt", id);
        TempData["SuccessMessage"] = "Tạo phiếu nhập kho thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetGoodsReceiptDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var ok = await _service.ConfirmGoodsReceiptAsync(id);
        if (ok)
        {
            await _notif.BroadcastAsync(
                $"✅ {_tenant.UserFullName} xác nhận nhập kho",
                $"{_tenant.UserFullName} đã xác nhận phiếu nhập kho.",
                "GoodsReceipt", id);
        }
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã xác nhận nhập kho. Đơn hàng mua đã được cập nhật." : "Không thể xác nhận.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ok = await _service.CancelGoodsReceiptAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã hủy phiếu nhập kho." : "Không thể hủy.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

