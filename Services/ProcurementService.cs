using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class ProcurementService(ApplicationDbContext db, ITenantContext tenant)
{
    // ── Procurement Requests ─────────────────────────────────────────────────
    public async Task<ProcurementListViewModel> GetListAsync(string? search, string? status)
    {
        var tid = tenant.TenantId;
        var q = db.ProcurementRequests.Include(p => p.Lines).Where(p => p.TenantId == tid && !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Title.Contains(search) || p.RequestNo.Contains(search));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProcurementStatus>(status, out var st))
            q = q.Where(p => p.Status == st);

        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Join(db.AppUsers, p => p.RequestedByUserId, u => u.Id, (p, u) => new { p, UserName = u.FullName })
            .Select(x => new ProcurementListItem
            {
                Id = x.p.Id, RequestNo = x.p.RequestNo, Title = x.p.Title,
                Status = x.p.Status.ToString(), RequestedBy = x.UserName,
                Department = x.p.OrganizationUnit != null ? x.p.OrganizationUnit.Name : null,
                NeededByDate = x.p.NeededByDate,
                TotalEstimated = x.p.Lines.Sum(l => l.Quantity * (l.EstimatedUnitPrice ?? 0)),
                LineCount = x.p.Lines.Count, CreatedAt = x.p.CreatedAt
            }).ToListAsync();

        return new ProcurementListViewModel { Items = items, TotalCount = items.Count, SearchTerm = search, StatusFilter = status };
    }

    public async Task<ProcurementDetailViewModel?> GetDetailAsync(Guid id)
    {
        var p = await db.ProcurementRequests
            .Include(p => p.Lines).ThenInclude(l => l.ProductService)
            .Include(p => p.RequestedByUser)
            .Include(p => p.OrganizationUnit)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId && !p.IsDeleted);
        if (p is null) return null;

        return new ProcurementDetailViewModel
        {
            Id = p.Id, RequestNo = p.RequestNo, Title = p.Title, Status = p.Status.ToString(),
            Department = p.OrganizationUnit?.Name, RequestedBy = p.RequestedByUser?.FullName ?? "",
            NeededByDate = p.NeededByDate, CreatedAt = p.CreatedAt,
            CanSubmit = p.Status == ProcurementStatus.Draft,
            CanCancel = p.Status is ProcurementStatus.Draft or ProcurementStatus.Submitted,
            Lines = p.Lines.Select(l => new ProcurementLineItem
            {
                Id = l.Id, ProductName = l.ProductService?.Name,
                ItemName = l.ItemName, Quantity = l.Quantity, EstimatedUnitPrice = l.EstimatedUnitPrice
            }).ToList()
        };
    }

    public async Task<ProcurementCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new ProcurementCreateViewModel
        {
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            Products = await db.ProductServices.Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.Code + " - " + p.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateAsync(ProcurementCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.ProcurementRequests.CountAsync(p => p.TenantId == tid);
        var entity = new ProcurementRequest
        {
            TenantId = tid, RequestNo = $"PR-{DateTime.Today.Year}-{count + 1:D3}",
            Title = vm.Title, RequestedByUserId = tenant.UserId,
            OrganizationUnitId = vm.OrganizationUnitId, NeededByDate = vm.NeededByDate,
            Status = ProcurementStatus.Draft, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        foreach (var line in vm.Lines)
        {
            entity.Lines.Add(new ProcurementRequestLine
            {
                TenantId = tid, ProductServiceId = line.ProductServiceId,
                ItemName = line.ItemName, Quantity = line.Quantity, EstimatedUnitPrice = line.EstimatedUnitPrice,
                CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        db.ProcurementRequests.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "ProcurementRequest", EntityId = entity.Id, NewValuesJson = $"{{\"RequestNo\":\"{entity.RequestNo}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> SubmitAsync(Guid id)
    {
        var p = await db.ProcurementRequests.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || p.Status != ProcurementStatus.Draft) return false;
        p.Status = ProcurementStatus.Submitted; p.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var p = await db.ProcurementRequests.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || p.Status is not (ProcurementStatus.Draft or ProcurementStatus.Submitted)) return false;
        p.Status = ProcurementStatus.Cancelled; p.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    // ── Purchase Orders ──────────────────────────────────────────────────────
    public async Task<PurchaseOrderListViewModel> GetPurchaseOrdersAsync(string? search, string? status)
    {
        var tid = tenant.TenantId;
        var q = db.PurchaseOrders.Include(po => po.Lines).Where(po => po.TenantId == tid && !po.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(po => po.OrderNo.Contains(search));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseOrderStatus>(status, out var st))
            q = q.Where(po => po.Status == st);

        var items = await q.OrderByDescending(po => po.CreatedAt)
            .Join(db.Vendors, po => po.VendorId, v => v.Id, (po, v) => new PurchaseOrderListItem
            {
                Id = po.Id, OrderNo = po.OrderNo, VendorName = v.Name,
                Status = po.Status.ToString(), OrderDate = po.OrderDate,
                TotalAmount = po.TotalAmount, LineCount = po.Lines.Count, CreatedAt = po.CreatedAt
            }).ToListAsync();

        return new PurchaseOrderListViewModel { Items = items, TotalCount = items.Count, SearchTerm = search, StatusFilter = status };
    }

    public async Task<PurchaseOrderDetailViewModel?> GetPurchaseOrderDetailAsync(Guid id)
    {
        var po = await db.PurchaseOrders
            .Include(p => p.Lines).ThenInclude(l => l.ProductService)
            .Include(p => p.Vendor)
            .Include(p => p.ProcurementRequest)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId && !p.IsDeleted);
        if (po is null) return null;

        return new PurchaseOrderDetailViewModel
        {
            Id = po.Id, OrderNo = po.OrderNo, VendorName = po.Vendor?.Name ?? "",
            ProcurementRequestNo = po.ProcurementRequest?.RequestNo,
            Status = po.Status.ToString(), OrderDate = po.OrderDate,
            TotalAmount = po.TotalAmount, CreatedAt = po.CreatedAt,
            Lines = po.Lines.Select(l => new PurchaseOrderLineItem
            {
                Id = l.Id, ProductName = l.ProductService?.Name,
                ItemName = l.ItemName, Quantity = l.Quantity, UnitPrice = l.UnitPrice
            }).ToList()
        };
    }

    public async Task<PurchaseOrderCreateViewModel> GetPOCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new PurchaseOrderCreateViewModel
        {
            Vendors = await db.Vendors.Where(v => v.TenantId == tid && v.IsActive && !v.IsDeleted)
                .Select(v => new SelectOption { Value = v.Id.ToString(), Text = v.Name }).ToListAsync(),
            ProcurementRequests = await db.ProcurementRequests.Where(p => p.TenantId == tid && p.Status == ProcurementStatus.Approved && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.RequestNo + " - " + p.Title }).ToListAsync(),
            Products = await db.ProductServices.Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.Code + " - " + p.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreatePOAsync(PurchaseOrderCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.PurchaseOrders.CountAsync(po => po.TenantId == tid);
        var total = vm.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var entity = new PurchaseOrder
        {
            TenantId = tid, OrderNo = $"PO-{DateTime.Today.Year}-{count + 1:D3}",
            VendorId = vm.VendorId, ProcurementRequestId = vm.ProcurementRequestId,
            OrderDate = vm.OrderDate, TotalAmount = total,
            Status = PurchaseOrderStatus.Draft, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        foreach (var line in vm.Lines)
        {
            entity.Lines.Add(new PurchaseOrderLine
            {
                TenantId = tid, ProductServiceId = line.ProductServiceId,
                ItemName = line.ItemName, Quantity = line.Quantity, UnitPrice = line.UnitPrice,
                CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        db.PurchaseOrders.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "PurchaseOrder", EntityId = entity.Id, NewValuesJson = $"{{\"OrderNo\":\"{entity.OrderNo}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    // ── Expenses ─────────────────────────────────────────────────────────────
    public async Task<ExpenseListViewModel> GetExpensesAsync(string? status, Guid? budgetId)
    {
        var tid = tenant.TenantId;
        var q = db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ExpenseStatus>(status, out var st))
            q = q.Where(e => e.Status == st);
        if (budgetId.HasValue)
            q = q.Where(e => e.BudgetId == budgetId);

        var items = await q.OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseListItem
            {
                Id = e.Id, Description = e.Description, Amount = e.Amount,
                ExpenseDate = e.ExpenseDate, Status = e.Status.ToString(),
                BudgetName = e.Budget != null ? e.Budget.Name : null,
                CreatedAt = e.CreatedAt
            }).ToListAsync();

        var budgets = await db.Budgets.Where(b => b.TenantId == tid && b.Status == BudgetStatus.Active && !b.IsDeleted)
            .Select(b => new SelectOption { Value = b.Id.ToString(), Text = b.Name }).ToListAsync();

        return new ExpenseListViewModel { Items = items, TotalCount = items.Count, StatusFilter = status, BudgetFilter = budgetId, Budgets = budgets };
    }

    public async Task<ExpenseCreateViewModel> GetExpenseCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new ExpenseCreateViewModel
        {
            Budgets = await db.Budgets.Where(b => b.TenantId == tid && b.Status == BudgetStatus.Active && !b.IsDeleted)
                .Select(b => new SelectOption { Value = b.Id.ToString(), Text = b.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateExpenseAsync(ExpenseCreateViewModel vm)
    {
        var entity = new Expense
        {
            TenantId = tenant.TenantId, Description = vm.Description, Amount = vm.Amount,
            ExpenseDate = vm.ExpenseDate, BudgetId = vm.BudgetId,
            Status = ExpenseStatus.Recorded, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Expenses.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "Expense", EntityId = entity.Id, NewValuesJson = $"{{\"Amount\":\"{vm.Amount}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    // ── Budget CRUD ──────────────────────────────────────────────────────────
    public async Task<BudgetCreateViewModel> GetBudgetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new BudgetCreateViewModel
        {
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateBudgetAsync(BudgetCreateViewModel vm)
    {
        var entity = new Budget
        {
            TenantId = tenant.TenantId, Name = vm.Name,
            OrganizationUnitId = vm.OrganizationUnitId, FiscalYear = vm.FiscalYear,
            PlannedAmount = vm.PlannedAmount, Status = BudgetStatus.Active,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Budgets.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "Budget", EntityId = entity.Id, NewValuesJson = $"{{\"Name\":\"{vm.Name}\",\"Amount\":\"{vm.PlannedAmount}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<BudgetDetailViewModel?> GetBudgetDetailAsync(Guid id)
    {
        var b = await db.Budgets
            .Include(b => b.Expenses.Where(e => !e.IsDeleted))
            .Include(b => b.OrganizationUnit)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenant.TenantId && !b.IsDeleted);
        if (b is null) return null;

        return new BudgetDetailViewModel
        {
            Id = b.Id, Name = b.Name, Department = b.OrganizationUnit?.Name ?? "",
            FiscalYear = b.FiscalYear, PlannedAmount = b.PlannedAmount,
            UsedAmount = b.Expenses.Sum(e => e.Amount), Status = b.Status.ToString(),
            Expenses = b.Expenses.Select(e => new ExpenseListItem
            {
                Id = e.Id, Description = e.Description, Amount = e.Amount,
                ExpenseDate = e.ExpenseDate, Status = e.Status.ToString(), CreatedAt = e.CreatedAt
            }).ToList()
        };
    }
}
