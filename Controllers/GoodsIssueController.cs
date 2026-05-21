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
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> Create()
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsIssueCreateViewModel vm)
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
