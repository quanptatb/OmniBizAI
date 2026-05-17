using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
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

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var c = await db.Customers.FindAsync(id);
        if (c is null || c.TenantId != tenant.TenantId) return false;
        c.IsDeleted = true; c.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "Customer", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
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

    public async Task<bool> DeleteVendorAsync(Guid id)
    {
        var v = await db.Vendors.FindAsync(id);
        if (v is null || v.TenantId != tenant.TenantId) return false;
        v.IsDeleted = true; v.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "Vendor", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
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

    public async Task<bool> DeleteContactAsync(Guid contactId)
    {
        var c = await db.CustomerContacts.FirstOrDefaultAsync(cc => cc.Id == contactId && cc.TenantId == tenant.TenantId && !cc.IsDeleted);
        if (c is null) return false;
        c.IsDeleted = true; c.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "DeleteContact", EntityName = "CustomerContact", EntityId = contactId, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> TogglePrimaryContactAsync(Guid contactId, Guid customerId)
    {
        var contacts = await db.CustomerContacts.Where(cc => cc.CustomerId == customerId && cc.TenantId == tenant.TenantId && !cc.IsDeleted).ToListAsync();
        foreach (var cc in contacts) cc.IsPrimary = cc.Id == contactId;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeleteSiteAsync(Guid siteId)
    {
        var s = await db.CustomerSites.FirstOrDefaultAsync(cs => cs.Id == siteId && cs.TenantId == tenant.TenantId && !cs.IsDeleted);
        if (s is null) return false;
        s.IsDeleted = true; s.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "DeleteSite", EntityName = "CustomerSite", EntityId = siteId, CreatedAt = DateTimeOffset.UtcNow });
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

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var p = await db.ProductServices.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId) return false;
        p.IsDeleted = true; p.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "ProductService", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
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

    // ── Customer Care (Interactions) ────────────────────────────────────────
    private static string TypeLabel(string t) => t switch { "Call" => "Cuộc gọi", "Email" => "Email", "Meeting" => "Cuộc họp", "Visit" => "Thăm viếng", "Note" => "Ghi chú", "Complaint" => "Khiếu nại", "Feedback" => "Phản hồi", _ => t };
    private static string StatusLabel(string s) => s switch { "Planned" => "Kế hoạch", "InProgress" => "Đang thực hiện", "Completed" => "Hoàn thành", "Cancelled" => "Đã hủy", _ => s };
    private static string PriorityLabel(string p) => p switch { "Low" => "Thấp", "Normal" => "Bình thường", "High" => "Cao", "Urgent" => "Khẩn cấp", _ => p };

    public async Task<CustomerCareListViewModel> GetInteractionsAsync(string? search, string? type, string? status, Guid? customerId)
    {
        var tid = tenant.TenantId;
        IQueryable<CrmInteraction> q = db.CrmInteractions.Where(i => i.TenantId == tid && !i.IsDeleted)
            .Include(i => i.Customer).Include(i => i.CustomerContact).Include(i => i.AssignedToUser);

        var plannedCount = await q.CountAsync(i => i.Status == "Planned");
        var inProgressCount = await q.CountAsync(i => i.Status == "InProgress");
        var completedCount = await q.CountAsync(i => i.Status == "Completed");
        var overdueCount = await q.CountAsync(i => i.Status == "Planned" && i.ScheduledAt.HasValue && i.ScheduledAt < DateTimeOffset.UtcNow);
        var totalCount = await q.CountAsync();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(i => i.Subject.Contains(search) || (i.Customer != null && i.Customer.Name.Contains(search)));
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(i => i.Type == type);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(i => i.Status == status);
        if (customerId.HasValue) q = q.Where(i => i.CustomerId == customerId);

        var items = await q.OrderByDescending(i => i.CreatedAt).Take(200)
            .Select(i => new CrmInteractionItem
            {
                Id = i.Id, Type = i.Type, Subject = i.Subject, Description = i.Description,
                Status = i.Status, Priority = i.Priority,
                CustomerName = i.Customer != null ? i.Customer.Name : "",
                ContactName = i.CustomerContact != null ? i.CustomerContact.FullName : null,
                AssignedToName = i.AssignedToUser != null ? i.AssignedToUser.FullName : null,
                ScheduledAt = i.ScheduledAt, DurationMinutes = i.DurationMinutes,
                Outcome = i.Outcome, NextAction = i.NextAction, NextActionDate = i.NextActionDate,
                CompletedAt = i.CompletedAt, CreatedAt = i.CreatedAt
            }).ToListAsync();

        var customerOpts = await db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.Name).Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

        return new CustomerCareListViewModel
        {
            Items = items, TotalCount = totalCount, PlannedCount = plannedCount,
            InProgressCount = inProgressCount, CompletedCount = completedCount, OverdueCount = overdueCount,
            SearchTerm = search, TypeFilter = type, StatusFilter = status, CustomerFilter = customerId,
            Customers = customerOpts
        };
    }

    public async Task<CustomerCareCreateViewModel> GetInteractionCreateFormAsync(Guid? customerId = null)
    {
        var tid = tenant.TenantId;
        var vm = new CustomerCareCreateViewModel
        {
            PreselectedCustomerId = customerId,
            CustomerId = customerId ?? Guid.Empty,
            Customers = await db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted && c.IsActive).OrderBy(c => c.Name)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " — " + c.Name }).ToListAsync(),
            Users = await db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active).OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
        };
        if (customerId.HasValue)
        {
            vm.Contacts = await db.CustomerContacts.Where(cc => cc.CustomerId == customerId && cc.TenantId == tid && !cc.IsDeleted)
                .OrderBy(cc => cc.FullName).Select(cc => new SelectOption { Value = cc.Id.ToString(), Text = cc.FullName + (cc.JobTitle != null ? " (" + cc.JobTitle + ")" : "") }).ToListAsync();
        }
        return vm;
    }

    public async Task<Guid> CreateInteractionAsync(CustomerCareCreateViewModel vm)
    {
        var entity = new CrmInteraction
        {
            TenantId = tenant.TenantId, CustomerId = vm.CustomerId, CustomerContactId = vm.CustomerContactId,
            Type = vm.Type, Subject = vm.Subject, Description = vm.Description, Priority = vm.Priority,
            Status = vm.ScheduledAt.HasValue ? "Planned" : "InProgress",
            ScheduledAt = vm.ScheduledAt, DurationMinutes = vm.DurationMinutes,
            AssignedToUserId = vm.AssignedToUserId ?? tenant.UserId,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.CrmInteractions.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "CrmInteraction", EntityId = entity.Id, NewValuesJson = $"{{\"Type\":\"{vm.Type}\",\"Subject\":\"{vm.Subject}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<CustomerCareDetailViewModel?> GetInteractionDetailAsync(Guid id)
    {
        var i = await db.CrmInteractions.Include(x => x.Customer).Include(x => x.CustomerContact)
            .Include(x => x.AssignedToUser)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (i is null) return null;

        var createdByName = i.CreatedByUserId.HasValue ? await db.AppUsers.Where(u => u.Id == i.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() : null;
        var completedByName = i.CompletedByUserId.HasValue ? await db.AppUsers.Where(u => u.Id == i.CompletedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() : null;

        return new CustomerCareDetailViewModel
        {
            Id = i.Id, Type = i.Type, TypeLabel = TypeLabel(i.Type), Subject = i.Subject, Description = i.Description,
            Status = i.Status, StatusLabel = StatusLabel(i.Status), Priority = i.Priority, PriorityLabel = PriorityLabel(i.Priority),
            CustomerId = i.CustomerId, CustomerName = i.Customer?.Name ?? "",
            ContactName = i.CustomerContact?.FullName, AssignedToName = i.AssignedToUser?.FullName,
            AssignedToUserId = i.AssignedToUserId,
            ScheduledAt = i.ScheduledAt, DurationMinutes = i.DurationMinutes,
            Outcome = i.Outcome, NextAction = i.NextAction, NextActionDate = i.NextActionDate,
            CompletedAt = i.CompletedAt, CompletedByName = completedByName,
            CreatedAt = i.CreatedAt, CreatedByName = createdByName
        };
    }

    public async Task<CustomerCareEditViewModel?> GetInteractionEditFormAsync(Guid id)
    {
        var i = await db.CrmInteractions.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (i is null) return null;
        var tid = tenant.TenantId;
        return new CustomerCareEditViewModel
        {
            Id = i.Id, CustomerId = i.CustomerId, CustomerContactId = i.CustomerContactId,
            Type = i.Type, Subject = i.Subject, Description = i.Description, Priority = i.Priority,
            ScheduledAt = i.ScheduledAt, DurationMinutes = i.DurationMinutes,
            AssignedToUserId = i.AssignedToUserId, CustomerName = i.Customer?.Name ?? "",
            Contacts = await db.CustomerContacts.Where(cc => cc.CustomerId == i.CustomerId && cc.TenantId == tid && !cc.IsDeleted)
                .Select(cc => new SelectOption { Value = cc.Id.ToString(), Text = cc.FullName }).ToListAsync(),
            Users = await db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
        };
    }

    public async Task<bool> UpdateInteractionAsync(CustomerCareEditViewModel vm)
    {
        var i = await db.CrmInteractions.FindAsync(vm.Id);
        if (i is null || i.TenantId != tenant.TenantId || i.Status == "Completed" || i.Status == "Cancelled") return false;
        i.Type = vm.Type; i.Subject = vm.Subject; i.Description = vm.Description; i.Priority = vm.Priority;
        i.ScheduledAt = vm.ScheduledAt; i.DurationMinutes = vm.DurationMinutes;
        i.CustomerContactId = vm.CustomerContactId; i.AssignedToUserId = vm.AssignedToUserId;
        i.UpdatedAt = DateTimeOffset.UtcNow; i.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "CrmInteraction", EntityId = i.Id, NewValuesJson = $"{{\"Subject\":\"{vm.Subject}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CompleteInteractionAsync(CompleteInteractionViewModel vm)
    {
        var i = await db.CrmInteractions.FindAsync(vm.Id);
        if (i is null || i.TenantId != tenant.TenantId || i.Status == "Completed" || i.Status == "Cancelled") return false;
        i.Status = "Completed"; i.Outcome = vm.Outcome; i.NextAction = vm.NextAction; i.NextActionDate = vm.NextActionDate;
        i.CompletedAt = DateTimeOffset.UtcNow; i.CompletedByUserId = tenant.UserId;
        i.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Complete", EntityName = "CrmInteraction", EntityId = i.Id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> StartInteractionAsync(Guid id)
    {
        var i = await db.CrmInteractions.FindAsync(id);
        if (i is null || i.TenantId != tenant.TenantId || i.Status != "Planned") return false;
        i.Status = "InProgress"; i.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CancelInteractionAsync(Guid id)
    {
        var i = await db.CrmInteractions.FindAsync(id);
        if (i is null || i.TenantId != tenant.TenantId || i.Status == "Completed") return false;
        i.Status = "Cancelled"; i.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Cancel", EntityName = "CrmInteraction", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeleteInteractionAsync(Guid id)
    {
        var i = await db.CrmInteractions.FindAsync(id);
        if (i is null || i.TenantId != tenant.TenantId) return false;
        i.IsDeleted = true; i.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "CrmInteraction", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<List<SelectOption>> GetContactsForCustomerAsync(Guid customerId)
    {
        return await db.CustomerContacts.Where(cc => cc.CustomerId == customerId && cc.TenantId == tenant.TenantId && !cc.IsDeleted)
            .OrderBy(cc => cc.FullName)
            .Select(cc => new SelectOption { Value = cc.Id.ToString(), Text = cc.FullName + (cc.JobTitle != null ? " (" + cc.JobTitle + ")" : "") })
            .ToListAsync();
    }

    // ── Sales Opportunities ──────────────────────────────────────────────────
    private static string StageLabel(string s) => s switch { "Lead" => "Tiềm năng", "Qualified" => "Đủ điều kiện", "Proposal" => "Đề xuất", "Negotiation" => "Đàm phán", "ClosedWon" => "Thắng", "ClosedLost" => "Thua", _ => s };
    private static string TempLabel(string t) => t switch { "Hot" => "Nóng", "Warm" => "Ấm", "Cold" => "Lạnh", _ => t };

    public async Task<SalesOpportunityListViewModel> GetOpportunitiesAsync(string? search, string? stage, string? temp, Guid? customerId)
    {
        var tid = tenant.TenantId;
        IQueryable<SalesOpportunity> q = db.SalesOpportunities.Where(o => o.TenantId == tid && !o.IsDeleted)
            .Include(o => o.Customer).Include(o => o.AssignedToUser);

        var leadCount = await q.CountAsync(o => o.Stage == "Lead");
        var qualifiedCount = await q.CountAsync(o => o.Stage == "Qualified");
        var proposalCount = await q.CountAsync(o => o.Stage == "Proposal");
        var negotiationCount = await q.CountAsync(o => o.Stage == "Negotiation");
        var closedWonCount = await q.CountAsync(o => o.Stage == "ClosedWon");
        var closedLostCount = await q.CountAsync(o => o.Stage == "ClosedLost");
        var totalCount = await q.CountAsync();
        var pipelineValue = await q.Where(o => o.Stage != "ClosedWon" && o.Stage != "ClosedLost").SumAsync(o => o.EstimatedValue);
        var weightedValue = await q.Where(o => o.Stage != "ClosedWon" && o.Stage != "ClosedLost").SumAsync(o => o.EstimatedValue * o.Probability / 100m);

        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(o => o.Title.Contains(search) || o.Code.Contains(search) || (o.Customer != null && o.Customer.Name.Contains(search)));
        if (!string.IsNullOrWhiteSpace(stage)) q = q.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(temp)) q = q.Where(o => o.Temperature == temp);
        if (customerId.HasValue) q = q.Where(o => o.CustomerId == customerId);

        var items = await q.OrderByDescending(o => o.CreatedAt).Take(200)
            .Select(o => new SalesOpportunityItem
            {
                Id = o.Id, Code = o.Code, Title = o.Title, Stage = o.Stage,
                EstimatedValue = o.EstimatedValue, Probability = o.Probability, Temperature = o.Temperature,
                CustomerName = o.Customer != null ? o.Customer.Name : "",
                AssignedToName = o.AssignedToUser != null ? o.AssignedToUser.FullName : null,
                Source = o.Source, ExpectedCloseDate = o.ExpectedCloseDate, CreatedAt = o.CreatedAt
            }).ToListAsync();

        var customers = await db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted && c.IsActive).OrderBy(c => c.Name)
            .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

        return new SalesOpportunityListViewModel
        {
            Items = items, TotalCount = totalCount,
            LeadCount = leadCount, QualifiedCount = qualifiedCount, ProposalCount = proposalCount,
            NegotiationCount = negotiationCount, ClosedWonCount = closedWonCount, ClosedLostCount = closedLostCount,
            TotalPipelineValue = pipelineValue, WeightedValue = weightedValue,
            SearchTerm = search, StageFilter = stage, TempFilter = temp, CustomerFilter = customerId,
            Customers = customers
        };
    }

    public async Task<SalesOpportunityCreateViewModel> GetOpportunityCreateFormAsync(Guid? customerId = null)
    {
        var tid = tenant.TenantId;
        var vm = new SalesOpportunityCreateViewModel
        {
            CustomerId = customerId ?? Guid.Empty,
            Customers = await db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted && c.IsActive).OrderBy(c => c.Name)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " — " + c.Name }).ToListAsync(),
            Users = await db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active).OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
        };
        if (customerId.HasValue)
            vm.Contacts = await GetContactsForCustomerAsync(customerId.Value);
        return vm;
    }

    public async Task<Guid> CreateOpportunityAsync(SalesOpportunityCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var seq = await db.SalesOpportunities.CountAsync(o => o.TenantId == tid) + 1;
        var entity = new SalesOpportunity
        {
            TenantId = tid, Code = $"OPP-{seq:D4}", Title = vm.Title, Description = vm.Description,
            CustomerId = vm.CustomerId, CustomerContactId = vm.CustomerContactId,
            EstimatedValue = vm.EstimatedValue, Probability = vm.Probability, Temperature = vm.Temperature,
            Source = vm.Source, Stage = "Lead",
            ExpectedCloseDate = vm.ExpectedCloseDate,
            AssignedToUserId = vm.AssignedToUserId ?? tenant.UserId,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.SalesOpportunities.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "SalesOpportunity", EntityId = entity.Id, NewValuesJson = $"{{\"Code\":\"{entity.Code}\",\"Title\":\"{vm.Title}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<SalesOpportunityDetailViewModel?> GetOpportunityDetailAsync(Guid id)
    {
        var o = await db.SalesOpportunities.Include(x => x.Customer).Include(x => x.CustomerContact).Include(x => x.AssignedToUser)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (o is null) return null;
        var createdByName = o.CreatedByUserId.HasValue ? await db.AppUsers.Where(u => u.Id == o.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() : null;
        return new SalesOpportunityDetailViewModel
        {
            Id = o.Id, Code = o.Code, Title = o.Title, Description = o.Description,
            Stage = o.Stage, StageLabel = StageLabel(o.Stage),
            EstimatedValue = o.EstimatedValue, Probability = o.Probability,
            Temperature = o.Temperature, TemperatureLabel = TempLabel(o.Temperature),
            Source = o.Source, ExpectedCloseDate = o.ExpectedCloseDate, ActualCloseDate = o.ActualCloseDate,
            LostReason = o.LostReason, WonNote = o.WonNote,
            CustomerId = o.CustomerId, CustomerName = o.Customer?.Name ?? "",
            ContactName = o.CustomerContact?.FullName, AssignedToName = o.AssignedToUser?.FullName,
            AssignedToUserId = o.AssignedToUserId, CreatedAt = o.CreatedAt, CreatedByName = createdByName
        };
    }

    public async Task<SalesOpportunityEditViewModel?> GetOpportunityEditFormAsync(Guid id)
    {
        var o = await db.SalesOpportunities.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (o is null) return null;
        var tid = tenant.TenantId;
        return new SalesOpportunityEditViewModel
        {
            Id = o.Id, Title = o.Title, Description = o.Description,
            CustomerId = o.CustomerId, CustomerContactId = o.CustomerContactId,
            EstimatedValue = o.EstimatedValue, Probability = o.Probability, Temperature = o.Temperature,
            Source = o.Source, ExpectedCloseDate = o.ExpectedCloseDate,
            AssignedToUserId = o.AssignedToUserId, CustomerName = o.Customer?.Name ?? "",
            Contacts = await GetContactsForCustomerAsync(o.CustomerId),
            Users = await db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
        };
    }

    public async Task<bool> UpdateOpportunityAsync(SalesOpportunityEditViewModel vm)
    {
        var o = await db.SalesOpportunities.FindAsync(vm.Id);
        if (o is null || o.TenantId != tenant.TenantId || o.Stage == "ClosedWon" || o.Stage == "ClosedLost") return false;
        o.Title = vm.Title; o.Description = vm.Description;
        o.CustomerContactId = vm.CustomerContactId;
        o.EstimatedValue = vm.EstimatedValue; o.Probability = vm.Probability; o.Temperature = vm.Temperature;
        o.Source = vm.Source; o.ExpectedCloseDate = vm.ExpectedCloseDate;
        o.AssignedToUserId = vm.AssignedToUserId;
        o.UpdatedAt = DateTimeOffset.UtcNow; o.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "SalesOpportunity", EntityId = o.Id, NewValuesJson = $"{{\"Title\":\"{vm.Title}\",\"Stage\":\"{o.Stage}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<(bool Success, string Message)> ChangeStageAsync(ChangeStageViewModel vm)
    {
        var o = await db.SalesOpportunities.FindAsync(vm.Id);
        if (o is null || o.TenantId != tenant.TenantId) return (false, "Không tìm thấy cơ hội.");
        if (o.Stage == "ClosedWon" || o.Stage == "ClosedLost") return (false, "Cơ hội đã đóng.");

        var oldStage = o.Stage;
        o.Stage = vm.NewStage;
        o.UpdatedAt = DateTimeOffset.UtcNow;

        // Set probability based on stage
        o.Probability = vm.NewStage switch { "Lead" => 10, "Qualified" => 25, "Proposal" => 50, "Negotiation" => 75, "ClosedWon" => 100, "ClosedLost" => 0, _ => o.Probability };

        if (vm.NewStage == "ClosedWon") { o.WonNote = vm.Note; o.ActualCloseDate = DateOnly.FromDateTime(DateTime.Today); }
        if (vm.NewStage == "ClosedLost") { o.LostReason = vm.Note; o.ActualCloseDate = DateOnly.FromDateTime(DateTime.Today); }

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "ChangeStage", EntityName = "SalesOpportunity", EntityId = o.Id, OldValuesJson = $"{{\"Stage\":\"{oldStage}\"}}", NewValuesJson = $"{{\"Stage\":\"{vm.NewStage}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return (true, $"Đã chuyển sang giai đoạn: {StageLabel(vm.NewStage)}");
    }

    public async Task<bool> DeleteOpportunityAsync(Guid id)
    {
        var o = await db.SalesOpportunities.FindAsync(id);
        if (o is null || o.TenantId != tenant.TenantId) return false;
        o.IsDeleted = true; o.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "SalesOpportunity", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }
}

