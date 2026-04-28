using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public sealed class PaymentRequestsController(IPaymentRequestService paymentRequestService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] PaymentRequestFilter filter, CancellationToken cancellationToken)
    {
        var items = await paymentRequestService.GetListAsync(filter, GetUserId(), cancellationToken);
        return View(items);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await paymentRequestService.GetDetailAsync(id, GetUserId(), cancellationToken);
            return View(detail);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new PaymentRequestCreateViewModel();
        await PopulateLookupsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentRequestCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var detail = await paymentRequestService.CreateDraftAsync(new CreatePaymentRequestRequest
            {
                Title = model.Title,
                Description = model.Description,
                DepartmentId = model.DepartmentId,
                CategoryId = model.CategoryId,
                VendorId = model.VendorId,
                BudgetId = model.BudgetId,
                PaymentDueDate = model.PaymentDueDate,
                Priority = model.Priority,
                Items = model.Items
            }, GetUserId(), cancellationToken);

            TempData["SuccessMessage"] = "Tạo đề nghị thanh toán thành công.";
            return RedirectToAction(nameof(Details), new { id = detail.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await paymentRequestService.GetDetailAsync(id, GetUserId(), cancellationToken);
            if (!string.Equals(detail.Status, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ đề nghị ở trạng thái Draft mới được chỉnh sửa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new PaymentRequestCreateViewModel
            {
                Title = detail.Title,
                Description = detail.Description,
                DepartmentId = detail.DepartmentId,
                CategoryId = detail.CategoryId,
                VendorId = detail.VendorId,
                BudgetId = detail.BudgetId,
                PaymentDueDate = detail.PaymentDueDate,
                Priority = detail.Priority,
                Items = detail.Items.Select(x => new PaymentRequestItemRequest
                {
                    Description = x.Description,
                    Quantity = x.Quantity,
                    Unit = x.Unit,
                    UnitPrice = x.UnitPrice,
                    TaxRate = x.TaxRate
                }).ToList()
            };
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PaymentRequestCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var detail = await paymentRequestService.UpdateDraftAsync(id, new UpdatePaymentRequestRequest
            {
                Title = model.Title,
                Description = model.Description,
                DepartmentId = model.DepartmentId,
                CategoryId = model.CategoryId,
                VendorId = model.VendorId,
                BudgetId = model.BudgetId,
                PaymentDueDate = model.PaymentDueDate,
                Priority = model.Priority,
                Items = model.Items
            }, GetUserId(), cancellationToken);

            TempData["SuccessMessage"] = "Cập nhật đề nghị thanh toán thành công.";
            return RedirectToAction(nameof(Details), new { id = detail.Id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateLookupsAsync(model, cancellationToken);
            return View(model);
        }
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User is not authenticated.");
    }

    private async Task PopulateLookupsAsync(PaymentRequestCreateViewModel model, CancellationToken cancellationToken)
    {
        var lookups = await paymentRequestService.GetLookupsAsync(cancellationToken);
        model.Departments = ToSelectList(lookups.Departments, model.DepartmentId, includeEmpty: false);
        model.Categories = ToSelectList(lookups.Categories, model.CategoryId, includeEmpty: false);
        model.Vendors = ToSelectList(lookups.Vendors, model.VendorId, includeEmpty: true);
        model.Budgets = ToSelectList(lookups.Budgets, model.BudgetId, includeEmpty: true);
        model.Priorities =
        [
            new SelectListItem("Low", "Low", model.Priority == "Low"),
            new SelectListItem("Normal", "Normal", model.Priority == "Normal"),
            new SelectListItem("High", "High", model.Priority == "High"),
            new SelectListItem("Urgent", "Urgent", model.Priority == "Urgent")
        ];

        if (model.Items.Count == 0)
        {
            model.Items.Add(new PaymentRequestItemRequest());
        }
    }

    private static List<SelectListItem> ToSelectList(IReadOnlyList<LookupItemDto> items, Guid? selectedId, bool includeEmpty)
    {
        var list = items
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), selectedId == x.Id))
            .ToList();

        if (includeEmpty)
        {
            list.Insert(0, new SelectListItem("-- None --", string.Empty));
        }

        return list;
    }
}
