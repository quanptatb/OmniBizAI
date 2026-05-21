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
    { return RedirectToAction("Index", "Customers"); }

    public async Task<IActionResult> Create(Guid? customerId)
    { return RedirectToAction("Index", "Customers"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCareCreateViewModel vm)
    { return RedirectToAction("Index", "Customers"); }

    public async Task<IActionResult> Details(Guid id)
    { return RedirectToAction("Index", "Customers"); }

    public async Task<IActionResult> Edit(Guid id)
    { return RedirectToAction("Index", "Customers"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerCareEditViewModel vm)
    { return RedirectToAction("Index", "Customers"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteInteractionViewModel vm)
    { return RedirectToAction("Index", "Customers"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid id)
    { return RedirectToAction("Index", "Customers"); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    { return RedirectToAction("Index", "Customers"); }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN,DEPARTMENT_MANAGER")]
    public async Task<IActionResult> Delete(Guid id)
    { return RedirectToAction("Index", "Customers"); }

    [HttpGet]
    public async Task<IActionResult> GetContacts(Guid customerId)
    { return RedirectToAction("Index", "Customers"); }
}
