using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class CrmService(ApplicationDbContext db, ITenantContext tenant)
{
    // ── Customers ────────────────────────────────────────────────────────────
    public async Task<CustomerListViewModel> GetCustomersAsync(string? search, string? industry)
    {
        var tid = tenant.TenantId;
        var q = db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search) || c.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(industry))
            q = q.Where(c => c.Industry == industry);

        var items = await q.OrderBy(c => c.Name)
            .Select(c => new CustomerListItem
            {
                Id = c.Id, Code = c.Code, Name = c.Name, TaxCode = c.TaxCode,
                Industry = c.Industry, IsActive = c.IsActive,
                ContactCount = c.Contacts.Count(cc => !cc.IsDeleted),
                SiteCount = c.Sites.Count(cs => !cs.IsDeleted),
                CreatedAt = c.CreatedAt
            }).ToListAsync();

        return new CustomerListViewModel { Items = items, TotalCount = items.Count, SearchTerm = search, IndustryFilter = industry };
    }

    public async Task<CustomerDetailViewModel?> GetCustomerDetailAsync(Guid id)
    {
        var c = await db.Customers
            .Include(c => c.Contacts.Where(cc => !cc.IsDeleted))
            .Include(c => c.Sites.Where(cs => !cs.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (c is null) return null;

        var opCount = await db.OperationRequests.CountAsync(r => r.CustomerId == id && !r.IsDeleted);
        return new CustomerDetailViewModel
        {
            Id = c.Id, Code = c.Code, Name = c.Name, TaxCode = c.TaxCode,
            Industry = c.Industry, IsActive = c.IsActive, CreatedAt = c.CreatedAt,
            OperationRequestCount = opCount,
            Contacts = c.Contacts.Select(cc => new CustomerContactItem
            {
                Id = cc.Id, FullName = cc.FullName, Email = cc.Email,
                PhoneNumber = cc.PhoneNumber, JobTitle = cc.JobTitle, IsPrimary = cc.IsPrimary
            }).ToList(),
            Sites = c.Sites.Select(cs => new CustomerSiteItem
            {
                Id = cs.Id, Name = cs.Name, Address = cs.Address, City = cs.City
            }).ToList()
        };
    }

    public async Task<Guid> CreateCustomerAsync(CustomerCreateViewModel vm)
    {
        var entity = new Customer
        {
            TenantId = tenant.TenantId, Code = vm.Code, Name = vm.Name,
            TaxCode = vm.TaxCode, Industry = vm.Industry,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Customers.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "Customer", EntityId = entity.Id, NewValuesJson = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ToggleCustomerAsync(Guid id)
    {
        var c = await db.Customers.FindAsync(id);
        if (c is null || c.TenantId != tenant.TenantId) return false;
        c.IsActive = !c.IsActive;
        c.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    // ── Vendors ──────────────────────────────────────────────────────────────
    public async Task<VendorListViewModel> GetVendorsAsync(string? search)
    {
        var tid = tenant.TenantId;
        var q = db.Vendors.Where(v => v.TenantId == tid && !v.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(v => v.Name.Contains(search) || v.Code.Contains(search));

        var items = await q.OrderBy(v => v.Name)
            .Select(v => new VendorListItem
            {
                Id = v.Id, Code = v.Code, Name = v.Name, TaxCode = v.TaxCode,
                Email = v.Email, PhoneNumber = v.PhoneNumber, IsActive = v.IsActive,
                PurchaseOrderCount = v.PurchaseOrders.Count(po => !po.IsDeleted),
                CreatedAt = v.CreatedAt
            }).ToListAsync();

        return new VendorListViewModel { Items = items, TotalCount = items.Count, SearchTerm = search };
    }

    public async Task<Guid> CreateVendorAsync(VendorCreateViewModel vm)
    {
        var entity = new Vendor
        {
            TenantId = tenant.TenantId, Code = vm.Code, Name = vm.Name,
            TaxCode = vm.TaxCode, Email = vm.Email, PhoneNumber = vm.PhoneNumber,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Vendors.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "Vendor", EntityId = entity.Id, NewValuesJson = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ToggleVendorAsync(Guid id)
    {
        var v = await db.Vendors.FindAsync(id);
        if (v is null || v.TenantId != tenant.TenantId) return false;
        v.IsActive = !v.IsActive;
        v.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    // ── Products ─────────────────────────────────────────────────────────────
    public async Task<ProductListViewModel> GetProductsAsync(string? search, string? type, Guid? categoryId)
    {
        var tid = tenant.TenantId;
        var q = db.ProductServices.Where(p => p.TenantId == tid && !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(p => p.Type == type);
        if (categoryId.HasValue)
            q = q.Where(p => p.ProductCategoryId == categoryId);

        var items = await q.OrderBy(p => p.Code)
            .Select(p => new ProductListItem
            {
                Id = p.Id, Code = p.Code, Name = p.Name, Type = p.Type,
                CategoryName = p.ProductCategory != null ? p.ProductCategory.Name : null,
                UnitName = p.UnitOfMeasure != null ? p.UnitOfMeasure.Name : null,
                StandardPrice = p.StandardPrice, IsActive = p.IsActive
            }).ToListAsync();

        var categories = await db.ProductCategories
            .Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted)
            .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

        return new ProductListViewModel { Items = items, TotalCount = items.Count, SearchTerm = search, TypeFilter = type, CategoryFilter = categoryId, Categories = categories };
    }

    public async Task<ProductCreateViewModel> GetProductCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new ProductCreateViewModel
        {
            Categories = await db.ProductCategories.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync(),
            Units = await db.UnitsOfMeasure.Where(u => u.TenantId == tid && !u.IsDeleted)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateProductAsync(ProductCreateViewModel vm)
    {
        var entity = new ProductService
        {
            TenantId = tenant.TenantId, Code = vm.Code, Name = vm.Name, Type = vm.Type,
            ProductCategoryId = vm.ProductCategoryId, UnitOfMeasureId = vm.UnitOfMeasureId,
            StandardPrice = vm.StandardPrice,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProductServices.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "ProductService", EntityId = entity.Id, NewValuesJson = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    // ── Customer Edit ───────────────────────────────────────────────────────
    public async Task<CustomerEditViewModel?> GetCustomerEditFormAsync(Guid id)
    {
        var c = await db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (c is null) return null;
        return new CustomerEditViewModel { Id = c.Id, Code = c.Code, Name = c.Name, TaxCode = c.TaxCode, Industry = c.Industry };
    }

    public async Task<bool> UpdateCustomerAsync(CustomerEditViewModel vm)
    {
        var c = await db.Customers.FindAsync(vm.Id);
        if (c is null || c.TenantId != tenant.TenantId) return false;
        c.Code = vm.Code; c.Name = vm.Name; c.TaxCode = vm.TaxCode; c.Industry = vm.Industry;
        c.UpdatedAt = DateTimeOffset.UtcNow; c.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "Customer", EntityId = c.Id, NewValuesJson = $"{{\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> AddContactAsync(AddContactViewModel vm)
    {
        var c = await db.Customers.FindAsync(vm.CustomerId);
        if (c is null || c.TenantId != tenant.TenantId) return false;
        var contact = new CustomerContact
        {
            TenantId = tenant.TenantId, CustomerId = vm.CustomerId,
            FullName = vm.FullName, JobTitle = vm.JobTitle, Email = vm.Email,
            PhoneNumber = vm.PhoneNumber, IsPrimary = vm.IsPrimary,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.CustomerContacts.Add(contact);
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> AddSiteAsync(AddSiteViewModel vm)
    {
        var c = await db.Customers.FindAsync(vm.CustomerId);
        if (c is null || c.TenantId != tenant.TenantId) return false;
        var site = new CustomerSite
        {
            TenantId = tenant.TenantId, CustomerId = vm.CustomerId,
            Name = vm.Name, Address = vm.Address, City = vm.City,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.CustomerSites.Add(site);
        await db.SaveChangesAsync(); return true;
    }

    // ── Vendor Edit & Detail ────────────────────────────────────────────────
    public async Task<VendorDetailViewModel?> GetVendorDetailAsync(Guid id)
    {
        var v = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenant.TenantId && !v.IsDeleted);
        if (v is null) return null;
        var poCount = await db.PurchaseOrders.CountAsync(po => po.VendorId == id && !po.IsDeleted);
        var prCount = await db.PaymentRequests.CountAsync(pr => pr.VendorId == id && !pr.IsDeleted);
        return new VendorDetailViewModel
        {
            Id = v.Id, Code = v.Code, Name = v.Name, TaxCode = v.TaxCode,
            Email = v.Email, PhoneNumber = v.PhoneNumber, IsActive = v.IsActive,
            CreatedAt = v.CreatedAt, PurchaseOrderCount = poCount, PaymentRequestCount = prCount
        };
    }

    public async Task<VendorEditViewModel?> GetVendorEditFormAsync(Guid id)
    {
        var v = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenant.TenantId && !v.IsDeleted);
        if (v is null) return null;
        return new VendorEditViewModel { Id = v.Id, Code = v.Code, Name = v.Name, TaxCode = v.TaxCode, Email = v.Email, PhoneNumber = v.PhoneNumber };
    }

    public async Task<bool> UpdateVendorAsync(VendorEditViewModel vm)
    {
        var v = await db.Vendors.FindAsync(vm.Id);
        if (v is null || v.TenantId != tenant.TenantId) return false;
        v.Code = vm.Code; v.Name = vm.Name; v.TaxCode = vm.TaxCode; v.Email = vm.Email; v.PhoneNumber = vm.PhoneNumber;
        v.UpdatedAt = DateTimeOffset.UtcNow; v.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "Vendor", EntityId = v.Id, NewValuesJson = $"{{\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── Product Edit ────────────────────────────────────────────────────────
    public async Task<ProductEditViewModel?> GetProductEditFormAsync(Guid id)
    {
        var p = await db.ProductServices.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId && !p.IsDeleted);
        if (p is null) return null;
        var tid = tenant.TenantId;
        return new ProductEditViewModel
        {
            Id = p.Id, Code = p.Code, Name = p.Name, Type = p.Type,
            ProductCategoryId = p.ProductCategoryId, UnitOfMeasureId = p.UnitOfMeasureId,
            StandardPrice = p.StandardPrice,
            Categories = await db.ProductCategories.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted).Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync(),
            Units = await db.UnitsOfMeasure.Where(u => u.TenantId == tid && !u.IsDeleted).Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.Name }).ToListAsync()
        };
    }

    public async Task<bool> UpdateProductAsync(ProductEditViewModel vm)
    {
        var p = await db.ProductServices.FindAsync(vm.Id);
        if (p is null || p.TenantId != tenant.TenantId) return false;
        p.Code = vm.Code; p.Name = vm.Name; p.Type = vm.Type;
        p.ProductCategoryId = vm.ProductCategoryId; p.UnitOfMeasureId = vm.UnitOfMeasureId;
        p.StandardPrice = vm.StandardPrice;
        p.UpdatedAt = DateTimeOffset.UtcNow; p.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "ProductService", EntityId = p.Id, NewValuesJson = $"{{\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── CRM Dashboard ───────────────────────────────────────────────────────
    public async Task<CrmDashboardViewModel> GetDashboardAsync()
    {
        var tid = tenant.TenantId;
        var customers = await db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted).ToListAsync();
        var vendors = await db.Vendors.Where(v => v.TenantId == tid && !v.IsDeleted).ToListAsync();
        var prodCount = await db.ProductServices.CountAsync(p => p.TenantId == tid && !p.IsDeleted);
        var contactCount = await db.CustomerContacts.CountAsync(c => c.TenantId == tid && !c.IsDeleted);

        return new CrmDashboardViewModel
        {
            TotalCustomers = customers.Count, ActiveCustomers = customers.Count(c => c.IsActive),
            TotalVendors = vendors.Count, ActiveVendors = vendors.Count(v => v.IsActive),
            TotalProducts = prodCount, TotalContacts = contactCount,
            RecentCustomers = customers.OrderByDescending(c => c.CreatedAt).Take(5).Select(c => new CustomerListItem
            {
                Id = c.Id, Code = c.Code, Name = c.Name, Industry = c.Industry, IsActive = c.IsActive, CreatedAt = c.CreatedAt
            }).ToList(),
            RecentVendors = vendors.OrderByDescending(v => v.CreatedAt).Take(5).Select(v => new VendorListItem
            {
                Id = v.Id, Code = v.Code, Name = v.Name, Email = v.Email, IsActive = v.IsActive, CreatedAt = v.CreatedAt
            }).ToList()
        };
    }
}

