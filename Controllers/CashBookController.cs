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
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Create(string? type)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashTransactionCreateViewModel vm)
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Details(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Approve(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "DEPARTMENT_MANAGER,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Reject(Guid id, string? reason)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Void(Guid id)
    { return RedirectToAction("Index", "Finance"); }
}
