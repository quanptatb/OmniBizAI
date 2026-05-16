using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly CrmService _service;
    public CustomersController(CrmService service) => _service = service;

    public async Task<IActionResult> Index(string? search, string? industry)
    {
        var vm = await _service.GetCustomersAsync(search, industry);
        return View(vm);
    }

    public IActionResult Create() => View(new CustomerCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var id = await _service.CreateCustomerAsync(vm);
        TempData["SuccessMessage"] = "Tạo khách hàng thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetCustomerDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _service.ToggleCustomerAsync(id);
        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class VendorsController : Controller
{
    private readonly CrmService _service;
    public VendorsController(CrmService service) => _service = service;

    public async Task<IActionResult> Index(string? search)
    {
        var vm = await _service.GetVendorsAsync(search);
        return View(vm);
    }

    public IActionResult Create() => View(new VendorCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.CreateVendorAsync(vm);
        TempData["SuccessMessage"] = "Tạo nhà cung cấp thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _service.ToggleVendorAsync(id);
        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class ProductsController : Controller
{
    private readonly CrmService _service;
    public ProductsController(CrmService service) => _service = service;

    public async Task<IActionResult> Index(string? search, string? type, Guid? category)
    {
        var vm = await _service.GetProductsAsync(search, type, category);
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var vm = await _service.GetProductCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.GetProductCreateFormAsync();
            vm.Categories = form.Categories;
            vm.Units = form.Units;
            return View(vm);
        }
        await _service.CreateProductAsync(vm);
        TempData["SuccessMessage"] = "Tạo sản phẩm/dịch vụ thành công.";
        return RedirectToAction(nameof(Index));
    }
}
