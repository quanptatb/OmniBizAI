using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "STAFF,DEPARTMENT_MANAGER,EXECUTIVE,ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN")]
public class InventoryController : Controller
{
    private readonly InventoryService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;
    public InventoryController(InventoryService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? search, string? stockFilter, string? categoryFilter)
    { return RedirectToAction("Index", "Finance"); }

    public async Task<IActionResult> SetThresholds(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetThresholds(SetStockThresholdsViewModel vm)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeAlert(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveAlert(Guid id)
    { return RedirectToAction("Index", "Finance"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshAlerts()
    { return RedirectToAction("Index", "Finance"); }
}
