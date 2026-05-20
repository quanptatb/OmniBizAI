using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "DEPARTMENT_MANAGER,EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN,STAFF")]
public class OrderManagementController : Controller
{
    private readonly OrderManagementService _service;

    public OrderManagementController(OrderManagementService service)
    {
        _service = service;
    }

    // ── Index ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, string? statusFilter)
    {
        var vm = await _service.GetIndexDataAsync(search, statusFilter);
        return View(vm);
    }

    // ── Details ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        
        // Pass available users for production step assignments
        ViewBag.Users = await _service.GetUsersAsync();
        return View(vm);
    }

    // ── Create ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrderCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetCreateFormAsync();
            vm.Customers = form.Customers;
            vm.Products = form.Products;
            return View(vm);
        }

        try
        {
            var orderId = await _service.CreateAsync(vm);
            TempData["SuccessMessage"] = "Đã khởi tạo đơn hàng thành công.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi khi tạo đơn hàng: {ex.Message}";
            var form = await _service.GetCreateFormAsync();
            vm.Customers = form.Customers;
            vm.Products = form.Products;
            return View(vm);
        }
    }

    // ── Submit for Approval ──────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var success = await _service.SubmitForApprovalAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Đã gửi đơn hàng phê duyệt quy trình 2 cấp.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể gửi đơn hàng phê duyệt.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Start Production ────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProduction(Guid id)
    {
        var success = await _service.StartProductionAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Khởi động quy trình sản xuất & QA/QC thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể khởi động quy trình sản xuất.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Update Production Step ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStep(Guid stepId, Guid orderId, ProductionStepStatus status, Guid? assignedUserId)
    {
        var success = await _service.UpdateProductionStepAsync(stepId, status, assignedUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Cập nhật công đoạn sản xuất thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = "Lỗi khi cập nhật công đoạn.";
        }
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    // ── Submit QC ────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQc(SalesOrderQcInputViewModel vm, Guid orderId)
    {
        var success = await _service.SubmitQcAsync(vm);
        if (success)
        {
            if (vm.QcStatus == QcStatus.Passed)
            {
                TempData["SuccessMessage"] = "Thẩm định chất lượng ĐẠT (Passed).";
            }
            else
            {
                TempData["WarningMessage"] = "Đã ghi nhận kết quả KHÔNG ĐẠT (Failed). Công đoạn được hoàn lại để xử lý.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Lỗi khi đánh giá chất lượng QA/QC.";
        }
        return RedirectToAction(nameof(Details), new { id = orderId });
    }
}
