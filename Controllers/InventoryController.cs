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
    {
        // Auto-generate alerts on dashboard load
        var newAlerts = await _service.GenerateStockAlertsAsync();
        if (newAlerts > 0)
            await _notif.SendToManagersAsync($"⚠️ {newAlerts} cảnh báo tồn kho mới", $"Hệ thống phát hiện {newAlerts} sản phẩm cần chú ý về tồn kho.", "Inventory", null);

        var vm = await _service.GetStockDashboardAsync(search, stockFilter, categoryFilter);
        return View(vm);
    }

    public async Task<IActionResult> SetThresholds(Guid id)
    {
        var vm = await _service.GetThresholdsFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetThresholds(SetStockThresholdsViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetThresholdsFormAsync(vm.ProductId);
            if (form != null) { vm.ProductCode = form.ProductCode; vm.ProductName = form.ProductName; vm.CurrentStock = form.CurrentStock; }
            return View(vm);
        }
        await _service.SaveThresholdsAsync(vm);
        TempData["SuccessMessage"] = "Đã cập nhật ngưỡng tồn kho.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeAlert(Guid id)
    {
        var ok = await _service.AcknowledgeAlertAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã ghi nhận cảnh báo." : "Không thể ghi nhận.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveAlert(Guid id)
    {
        var ok = await _service.ResolveAlertAsync(id);
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Đã xử lý cảnh báo." : "Không thể xử lý.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshAlerts()
    {
        var count = await _service.GenerateStockAlertsAsync();
        TempData["SuccessMessage"] = count > 0 ? $"Phát hiện {count} cảnh báo mới." : "Không có cảnh báo mới.";
        return RedirectToAction(nameof(Index));
    }
}
