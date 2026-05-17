using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly CrmService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public CustomersController(CrmService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

    public async Task<IActionResult> Dashboard()
    {
        var vm = await _service.GetDashboardAsync();
        return View(vm);
    }

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
        await _notif.SendToManagersAsync($"🏢 {_tenant.UserFullName} thêm khách hàng", $"{_tenant.UserFullName} đã thêm KH {vm.Name} ({vm.Code}).", "Customer", id);
        TempData["SuccessMessage"] = "Tạo khách hàng thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetCustomerDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetCustomerEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await _service.UpdateCustomerAsync(vm)) { TempData["ErrorMessage"] = "Cập nhật thất bại."; return View(vm); }
        TempData["SuccessMessage"] = "Cập nhật khách hàng thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _service.ToggleCustomerAsync(id);
        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult AddContact(Guid id, string? name) => View(new AddContactViewModel { CustomerId = id, CustomerName = name ?? "" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddContact(AddContactViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await _service.AddContactAsync(vm)) { TempData["ErrorMessage"] = "Thêm liên hệ thất bại."; }
        else { TempData["SuccessMessage"] = "Thêm liên hệ thành công."; }
        return RedirectToAction(nameof(Details), new { id = vm.CustomerId });
    }

    public IActionResult AddSite(Guid id, string? name) => View(new AddSiteViewModel { CustomerId = id, CustomerName = name ?? "" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSite(AddSiteViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await _service.AddSiteAsync(vm)) { TempData["ErrorMessage"] = "Thêm chi nhánh thất bại."; }
        else { TempData["SuccessMessage"] = "Thêm chi nhánh thành công."; }
        return RedirectToAction(nameof(Details), new { id = vm.CustomerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(Guid contactId, Guid customerId)
    {
        if (!await _service.DeleteContactAsync(contactId)) { TempData["ErrorMessage"] = "Không thể xóa liên hệ."; }
        else { TempData["SuccessMessage"] = "Đã xóa liên hệ."; }
        return RedirectToAction(nameof(Details), new { id = customerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSite(Guid siteId, Guid customerId)
    {
        if (!await _service.DeleteSiteAsync(siteId)) { TempData["ErrorMessage"] = "Không thể xóa chi nhánh."; }
        else { TempData["SuccessMessage"] = "Đã xóa chi nhánh."; }
        return RedirectToAction(nameof(Details), new { id = customerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePrimary(Guid contactId, Guid customerId)
    {
        await _service.TogglePrimaryContactAsync(contactId, customerId);
        TempData["SuccessMessage"] = "Đã đặt liên hệ chính.";
        return RedirectToAction(nameof(Details), new { id = customerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _service.DeleteCustomerAsync(id)) { TempData["ErrorMessage"] = "Không thể xóa khách hàng."; }
        else { TempData["SuccessMessage"] = "Đã xóa khách hàng."; }
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class VendorsController : Controller
{
    private readonly CrmService _service;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public VendorsController(CrmService service, NotificationService notif, ITenantContext tenant)
    {
        _service = service; _notif = notif; _tenant = tenant;
    }

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
        var id = await _service.CreateVendorAsync(vm);
        await _notif.SendToManagersAsync($"📦 {_tenant.UserFullName} thêm NCC", $"{_tenant.UserFullName} đã thêm NCC {vm.Name} ({vm.Code}).", "Vendor", id);
        TempData["SuccessMessage"] = "Tạo nhà cung cấp thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetVendorDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetVendorEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await _service.UpdateVendorAsync(vm)) { TempData["ErrorMessage"] = "Cập nhật thất bại."; return View(vm); }
        TempData["SuccessMessage"] = "Cập nhật NCC thành công.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _service.ToggleVendorAsync(id);
        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _service.DeleteVendorAsync(id)) { TempData["ErrorMessage"] = "Không thể xóa NCC."; }
        else { TempData["SuccessMessage"] = "Đã xóa nhà cung cấp."; }
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
        if (!ModelState.IsValid) { var f = await _service.GetProductCreateFormAsync(); vm.Categories = f.Categories; vm.Units = f.Units; return View(vm); }
        await _service.CreateProductAsync(vm);
        TempData["SuccessMessage"] = "Tạo sản phẩm/dịch vụ thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _service.GetProductEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid) { var f = await _service.GetProductEditFormAsync(vm.Id); vm.Categories = f?.Categories ?? new(); vm.Units = f?.Units ?? new(); return View(vm); }
        if (!await _service.UpdateProductAsync(vm)) { TempData["ErrorMessage"] = "Cập nhật thất bại."; return View(vm); }
        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _service.DeleteProductAsync(id)) { TempData["ErrorMessage"] = "Không thể xóa sản phẩm."; }
        else { TempData["SuccessMessage"] = "Đã xóa sản phẩm/dịch vụ."; }
        return RedirectToAction(nameof(Index));
    }
}
