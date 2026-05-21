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
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Create()
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProcurementCreateViewModel vm)
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Details(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    { return RedirectToAction("Index", "Finance"); }
}

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
public class PurchaseOrdersController : Controller
{
    private readonly ProcurementService _service;
    public PurchaseOrdersController(ProcurementService service) => _service = service;

    public async Task<IActionResult> Index(string? search, string? status)
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Create()
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderCreateViewModel vm)
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Details(Guid id)
    { return RedirectToAction("Index", "Finance"); }
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
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Create(Guid? poId)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsReceiptCreateViewModel vm)
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Details(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Confirm(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    { return RedirectToAction("Index", "Finance"); }
}

