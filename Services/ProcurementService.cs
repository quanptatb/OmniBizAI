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

    public async Task<bool> ApproveExpenseAsync(Guid id)
    {
        var e = await db.Expenses.FindAsync(id);
        if (e is null || e.TenantId != tenant.TenantId || e.Status != ExpenseStatus.Recorded) return false;
        e.Status = ExpenseStatus.Approved; e.UpdatedAt = DateTimeOffset.UtcNow; e.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Approve", EntityName = "Expense", EntityId = id, NewValuesJson = "{\"Status\":\"Approved\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
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
            Expenses = b.Expenses.OrderByDescending(e => e.ExpenseDate).Select(e => new ExpenseListItem
            {
                Id = e.Id, Description = e.Description, Amount = e.Amount,
                ExpenseDate = e.ExpenseDate, Status = e.Status.ToString(), CreatedAt = e.CreatedAt
            }).ToList()
        };
    }

    public async Task<BudgetEditViewModel?> GetBudgetEditFormAsync(Guid id)
    {
        var b = await db.Budgets.Include(b => b.OrganizationUnit).Include(b => b.Expenses.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenant.TenantId && !b.IsDeleted);
        if (b is null || b.Status != BudgetStatus.Active) return null;

        return new BudgetEditViewModel
        {
            Id = b.Id, CurrentName = b.Name, Name = b.Name,
            PlannedAmount = b.PlannedAmount, UsedAmount = b.Expenses.Sum(e => e.Amount),
            Department = b.OrganizationUnit?.Name ?? "", FiscalYear = b.FiscalYear
        };
    }

    public async Task<bool> UpdateBudgetAsync(BudgetEditViewModel vm)
    {
        var b = await db.Budgets.FindAsync(vm.Id);
        if (b is null || b.TenantId != tenant.TenantId || b.Status != BudgetStatus.Active) return false;

        var oldName = b.Name; var oldAmount = b.PlannedAmount;
        b.Name = vm.Name; b.PlannedAmount = vm.PlannedAmount;
        b.UpdatedAt = DateTimeOffset.UtcNow; b.UpdatedByUserId = tenant.UserId;

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "Budget", EntityId = b.Id, OldValuesJson = $"{{\"Name\":\"{oldName}\",\"Amount\":\"{oldAmount}\"}}", NewValuesJson = $"{{\"Name\":\"{b.Name}\",\"Amount\":\"{b.PlannedAmount}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CloseBudgetAsync(Guid id)
    {
        var b = await db.Budgets.FindAsync(id);
        if (b is null || b.TenantId != tenant.TenantId || b.Status != BudgetStatus.Active) return false;
        b.Status = BudgetStatus.Closed; b.UpdatedAt = DateTimeOffset.UtcNow; b.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Close", EntityName = "Budget", EntityId = id, NewValuesJson = "{\"Status\":\"Closed\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── Payment Requests ─────────────────────────────────────────────────────
    public async Task<PaymentRequestCreateViewModel> GetPaymentCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new PaymentRequestCreateViewModel
        {
            Vendors = await db.Vendors.Where(v => v.TenantId == tid && v.IsActive && !v.IsDeleted)
                .Select(v => new SelectOption { Value = v.Id.ToString(), Text = v.Name }).ToListAsync(),
            PurchaseOrders = await db.PurchaseOrders.Where(po => po.TenantId == tid && !po.IsDeleted && po.Status != PurchaseOrderStatus.Cancelled)
                .OrderByDescending(po => po.CreatedAt)
                .Select(po => new SelectOption { Value = po.Id.ToString(), Text = po.OrderNo }).ToListAsync()
        };
    }

    public async Task<Guid> CreatePaymentAsync(PaymentRequestCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.PaymentRequests.CountAsync(p => p.TenantId == tid);
        var entity = new PaymentRequest
        {
            TenantId = tid, RequestNo = $"PAY-{DateTime.Today.Year}-{count + 1:D3}",
            VendorId = vm.VendorId, PurchaseOrderId = vm.PurchaseOrderId,
            TotalAmount = vm.TotalAmount, DueDate = vm.DueDate,
            RequestedByUserId = tenant.UserId, Status = PaymentStatus.Draft,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.PaymentRequests.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "PaymentRequest", EntityId = entity.Id, NewValuesJson = $"{{\"RequestNo\":\"{entity.RequestNo}\",\"Amount\":\"{vm.TotalAmount}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<PaymentRequestListViewModel> GetPaymentListAsync(string? status)
    {
        var tid = tenant.TenantId;
        var q = db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, out var st))
            q = q.Where(p => p.Status == st);

        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentRequestListItem
            {
                Id = p.Id, RequestNo = p.RequestNo,
                Title = p.RequestNo, TotalAmount = p.TotalAmount,
                Status = p.Status.ToString(),
                VendorName = p.Vendor != null ? p.Vendor.Name : null,
                RequestedBy = p.RequestedByUser != null ? p.RequestedByUser.FullName : null,
                DueDate = p.DueDate, Department = "", CreatedAt = p.CreatedAt
            }).ToListAsync();

        return new PaymentRequestListViewModel { Items = items, TotalCount = items.Count, StatusFilter = status };
    }

    public async Task<PaymentRequestDetailViewModel?> GetPaymentDetailAsync(Guid id)
    {
        var p = await db.PaymentRequests
            .Include(p => p.Vendor)
            .Include(p => p.PurchaseOrder)
            .Include(p => p.RequestedByUser)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId && !p.IsDeleted);
        if (p is null) return null;

        var activityLog = await db.AuditLogs
            .Where(a => a.TenantId == tenant.TenantId && a.EntityId == id && a.EntityName == "PaymentRequest")
            .OrderByDescending(a => a.CreatedAt).Take(15)
            .Select(a => new ActivityLogItem { UserName = a.UserName, Action = a.Action, Details = a.NewValuesJson, OccurredAt = a.CreatedAt })
            .ToListAsync();

        return new PaymentRequestDetailViewModel
        {
            Id = p.Id, RequestNo = p.RequestNo, Title = p.RequestNo,
            TotalAmount = p.TotalAmount, Status = p.Status.ToString(),
            VendorName = p.Vendor?.Name, PurchaseOrderNo = p.PurchaseOrder?.OrderNo,
            RequestedBy = p.RequestedByUser?.FullName ?? "", DueDate = p.DueDate,
            CreatedAt = p.CreatedAt, ActivityLog = activityLog
        };
    }

    public async Task<bool> SubmitPaymentAsync(Guid id)
    {
        var p = await db.PaymentRequests.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || p.Status != PaymentStatus.Draft) return false;
        p.Status = PaymentStatus.Submitted; p.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Submit", EntityName = "PaymentRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Submitted\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> ApprovePaymentAsync(Guid id)
    {
        var p = await db.PaymentRequests.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || p.Status != PaymentStatus.Submitted) return false;
        p.Status = PaymentStatus.Approved; p.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Approve", EntityName = "PaymentRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Approved\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> RejectPaymentAsync(Guid id, string? reason)
    {
        var p = await db.PaymentRequests.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || p.Status != PaymentStatus.Submitted) return false;
        p.Status = PaymentStatus.Rejected; p.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Reject", EntityName = "PaymentRequest", EntityId = id, NewValuesJson = $"{{\"Status\":\"Rejected\",\"Reason\":\"{reason}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> MarkPaymentPaidAsync(Guid id)
    {
        var p = await db.PaymentRequests.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || p.Status != PaymentStatus.Approved) return false;
        p.Status = PaymentStatus.Paid; p.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "MarkPaid", EntityName = "PaymentRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Paid\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── Finance Dashboard ────────────────────────────────────────────────────
    public async Task<FinanceDashboardViewModel> GetDashboardAsync()
    {
        var tid = tenant.TenantId;
        var year = DateTime.Today.Year;
        var som = new DateOnly(year, DateTime.Today.Month, 1);

        var totalBudget = await db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.FiscalYear == year).SumAsync(b => b.PlannedAmount);
        var totalExpense = await db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == year).SumAsync(e => e.Amount);
        var activeBudgets = await db.Budgets.CountAsync(b => b.TenantId == tid && !b.IsDeleted && b.Status == BudgetStatus.Active);
        var expenseMonth = await db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate >= som).SumAsync(e => e.Amount);

        var pendingPay = await db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted && (p.Status == PaymentStatus.Draft || p.Status == PaymentStatus.Submitted)).ToListAsync();

        // Alert budgets (>70%)
        var alertBudgets = await db.Budgets
            .Include(b => b.Expenses.Where(e => !e.IsDeleted))
            .Include(b => b.OrganizationUnit)
            .Where(b => b.TenantId == tid && !b.IsDeleted && b.Status == BudgetStatus.Active)
            .ToListAsync();

        var alertItems = alertBudgets
            .Select(b => new BudgetListItem
            {
                Id = b.Id, Name = b.Name, Department = b.OrganizationUnit?.Name ?? "",
                TotalAmount = b.PlannedAmount, UsedAmount = b.Expenses.Sum(e => e.Amount),
                Status = b.Status.ToString(), PeriodStart = new DateOnly(b.FiscalYear, 1, 1), PeriodEnd = new DateOnly(b.FiscalYear, 12, 31)
            })
            .Where(b => b.UsagePercent > 70)
            .OrderByDescending(b => b.UsagePercent)
            .ToList();

        // Recent payments
        var recentPay = await db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt).Take(5)
            .Select(p => new PaymentRequestListItem
            {
                Id = p.Id, RequestNo = p.RequestNo, Title = p.RequestNo,
                TotalAmount = p.TotalAmount, Status = p.Status.ToString(),
                VendorName = p.Vendor != null ? p.Vendor.Name : null,
                DueDate = p.DueDate, Department = "", CreatedAt = p.CreatedAt
            }).ToListAsync();

        // Monthly expenses for chart
        var monthlyData = await db.Expenses
            .Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == year)
            .GroupBy(e => e.ExpenseDate.Month)
            .Select(g => new { Month = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToListAsync();

        var monthlyExpenses = Enumerable.Range(1, 12).Select(m => new ExpenseMonthItem
        {
            Month = new DateTime(year, m, 1).ToString("MMM"),
            Amount = monthlyData.FirstOrDefault(d => d.Month == m)?.Amount ?? 0
        }).ToList();

        return new FinanceDashboardViewModel
        {
            TotalBudget = totalBudget, TotalExpense = totalExpense,
            ActiveBudgets = activeBudgets, ExpenseThisMonth = expenseMonth,
            PendingPayments = pendingPay.Count, PendingPaymentAmount = pendingPay.Sum(p => p.TotalAmount),
            AlertBudgets = alertItems, RecentPayments = recentPay, MonthlyExpenses = monthlyExpenses
        };
    }

    // ── Goods Receipt (Nhập kho) ─────────────────────────────────────────────
    public async Task<GoodsReceiptListViewModel> GetGoodsReceiptsAsync(string? search, string? status)
    {
        var tid = tenant.TenantId;
        var baseQ = db.GoodsReceipts.Where(gr => gr.TenantId == tid && !gr.IsDeleted);

        var draftCount = await baseQ.CountAsync(gr => gr.Status == GoodsReceiptStatus.Draft);
        var confirmedCount = await baseQ.CountAsync(gr => gr.Status == GoodsReceiptStatus.Confirmed);

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(gr => gr.ReceiptNo.Contains(search) || gr.PurchaseOrder!.OrderNo.Contains(search));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GoodsReceiptStatus>(status, out var st))
            q = q.Where(gr => gr.Status == st);

        var items = await q.OrderByDescending(gr => gr.CreatedAt)
            .Select(gr => new GoodsReceiptListItem
            {
                Id = gr.Id, ReceiptNo = gr.ReceiptNo,
                PurchaseOrderNo = gr.PurchaseOrder != null ? gr.PurchaseOrder.OrderNo : "",
                VendorName = gr.PurchaseOrder != null && gr.PurchaseOrder.Vendor != null ? gr.PurchaseOrder.Vendor.Name : "",
                Status = gr.Status.ToString(), ReceiptDate = gr.ReceiptDate,
                WarehouseLocation = gr.WarehouseLocation,
                ReceivedBy = gr.ReceivedByUser != null ? gr.ReceivedByUser.FullName : "",
                LineCount = gr.Lines.Count(l => !l.IsDeleted),
                TotalReceivedQty = gr.Lines.Where(l => !l.IsDeleted).Sum(l => l.ReceivedQuantity),
                CreatedAt = gr.CreatedAt
            }).ToListAsync();

        return new GoodsReceiptListViewModel
        {
            Items = items, TotalCount = items.Count,
            DraftCount = draftCount, ConfirmedCount = confirmedCount,
            SearchTerm = search, StatusFilter = status
        };
    }

    public async Task<GoodsReceiptDetailViewModel?> GetGoodsReceiptDetailAsync(Guid id)
    {
        var gr = await db.GoodsReceipts
            .Include(g => g.Lines.Where(l => !l.IsDeleted)).ThenInclude(l => l.ProductService)
            .Include(g => g.PurchaseOrder).ThenInclude(po => po!.Vendor)
            .Include(g => g.ReceivedByUser)
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenant.TenantId && !g.IsDeleted);
        if (gr is null) return null;

        var activityLog = await db.AuditLogs
            .Where(a => a.TenantId == tenant.TenantId && a.EntityId == id && a.EntityName == "GoodsReceipt")
            .OrderByDescending(a => a.CreatedAt).Take(15)
            .Select(a => new ActivityLogItem { UserName = a.UserName, Action = a.Action, Details = a.NewValuesJson, OccurredAt = a.CreatedAt })
            .ToListAsync();

        return new GoodsReceiptDetailViewModel
        {
            Id = gr.Id, ReceiptNo = gr.ReceiptNo,
            PurchaseOrderNo = gr.PurchaseOrder?.OrderNo ?? "", PurchaseOrderId = gr.PurchaseOrderId,
            VendorName = gr.PurchaseOrder?.Vendor?.Name ?? "",
            Status = gr.Status.ToString(), ReceiptDate = gr.ReceiptDate,
            WarehouseLocation = gr.WarehouseLocation, Note = gr.Note,
            ReceivedBy = gr.ReceivedByUser?.FullName ?? "", CreatedAt = gr.CreatedAt,
            Lines = gr.Lines.Select(l => new GoodsReceiptLineDisplay
            {
                Id = l.Id, ItemName = l.ItemName, OrderedQuantity = l.OrderedQuantity,
                ReceivedQuantity = l.ReceivedQuantity, RejectedQuantity = l.RejectedQuantity,
                QualityPassed = l.QualityPassed, Note = l.Note,
                ProductName = l.ProductService?.Name, ProductCode = l.ProductService?.Code
            }).ToList(),
            ActivityLog = activityLog,
            CanConfirm = gr.Status == GoodsReceiptStatus.Draft,
            CanCancel = gr.Status == GoodsReceiptStatus.Draft,
            CanEdit = gr.Status == GoodsReceiptStatus.Draft
        };
    }

    public async Task<GoodsReceiptCreateViewModel> GetGoodsReceiptCreateFormAsync(Guid? poId = null)
    {
        var tid = tenant.TenantId;
        var vm = new GoodsReceiptCreateViewModel
        {
            PurchaseOrders = await db.PurchaseOrders
                .Include(po => po.Vendor)
                .Where(po => po.TenantId == tid && !po.IsDeleted && po.Status != PurchaseOrderStatus.Cancelled && po.Status != PurchaseOrderStatus.Completed)
                .OrderByDescending(po => po.CreatedAt)
                .Select(po => new SelectOption { Value = po.Id.ToString(), Text = po.OrderNo + " — " + (po.Vendor != null ? po.Vendor.Name : "") })
                .ToListAsync()
        };

        // If specific PO selected, auto-fill lines from PO lines
        if (poId.HasValue)
        {
            var po = await db.PurchaseOrders
                .Include(p => p.Lines).ThenInclude(l => l.ProductService)
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == poId.Value && p.TenantId == tid && !p.IsDeleted);
            if (po != null)
            {
                vm.PurchaseOrderId = po.Id;
                vm.SelectedPONo = po.OrderNo;
                vm.SelectedVendor = po.Vendor?.Name;

                // Get already received quantities per PO line
                var receivedQtys = await db.GoodsReceiptLines
                    .Where(l => l.GoodsReceipt!.PurchaseOrderId == po.Id && !l.IsDeleted && l.GoodsReceipt.Status == GoodsReceiptStatus.Confirmed)
                    .GroupBy(l => l.PurchaseOrderLineId)
                    .Select(g => new { POLineId = g.Key, TotalReceived = g.Sum(l => l.ReceivedQuantity) })
                    .ToListAsync();

                vm.Lines = po.Lines.Where(l => !l.IsDeleted).Select(l =>
                {
                    var alreadyReceived = receivedQtys.FirstOrDefault(r => r.POLineId == l.Id)?.TotalReceived ?? 0;
                    var remaining = l.Quantity - alreadyReceived;
                    return new GoodsReceiptLineInput
                    {
                        PurchaseOrderLineId = l.Id, ProductServiceId = l.ProductServiceId,
                        ItemName = l.ProductService?.Name ?? l.ItemName,
                        OrderedQuantity = l.Quantity, ReceivedQuantity = Math.Max(0, remaining)
                    };
                }).ToList();
            }
        }

        return vm;
    }

    public async Task<Guid> CreateGoodsReceiptAsync(GoodsReceiptCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.GoodsReceipts.CountAsync(gr => gr.TenantId == tid);
        var entity = new GoodsReceipt
        {
            TenantId = tid, ReceiptNo = $"GR-{DateTime.Today.Year}-{count + 1:D4}",
            PurchaseOrderId = vm.PurchaseOrderId, ReceivedByUserId = tenant.UserId,
            ReceiptDate = vm.ReceiptDate, WarehouseLocation = vm.WarehouseLocation, Note = vm.Note,
            Status = GoodsReceiptStatus.Draft, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        foreach (var line in vm.Lines.Where(l => l.ReceivedQuantity > 0))
        {
            entity.Lines.Add(new GoodsReceiptLine
            {
                TenantId = tid, PurchaseOrderLineId = line.PurchaseOrderLineId,
                ProductServiceId = line.ProductServiceId, ItemName = line.ItemName,
                OrderedQuantity = line.OrderedQuantity, ReceivedQuantity = line.ReceivedQuantity,
                RejectedQuantity = line.RejectedQuantity, QualityPassed = line.QualityPassed,
                Note = line.Note, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        db.GoodsReceipts.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "GoodsReceipt", EntityId = entity.Id, NewValuesJson = $"{{\"ReceiptNo\":\"{entity.ReceiptNo}\",\"PO\":\"{vm.PurchaseOrderId}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ConfirmGoodsReceiptAsync(Guid id)
    {
        var gr = await db.GoodsReceipts.Include(g => g.Lines.Where(l => !l.IsDeleted)).FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenant.TenantId);
        if (gr is null || gr.Status != GoodsReceiptStatus.Draft) return false;

        gr.Status = GoodsReceiptStatus.Confirmed; gr.UpdatedAt = DateTimeOffset.UtcNow;

        // Update PurchaseOrder status based on total received quantities
        var po = await db.PurchaseOrders.Include(p => p.Lines.Where(l => !l.IsDeleted)).FirstOrDefaultAsync(p => p.Id == gr.PurchaseOrderId);
        if (po != null)
        {
            // Sum all confirmed receipts for this PO
            var totalReceived = await db.GoodsReceiptLines
                .Where(l => l.GoodsReceipt!.PurchaseOrderId == po.Id && !l.IsDeleted && (l.GoodsReceipt.Status == GoodsReceiptStatus.Confirmed || l.GoodsReceipt.Id == gr.Id))
                .SumAsync(l => l.ReceivedQuantity);
            var totalOrdered = po.Lines.Sum(l => l.Quantity);

            po.Status = totalReceived >= totalOrdered ? PurchaseOrderStatus.Completed : PurchaseOrderStatus.PartiallyReceived;
            po.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Update Procurement status if applicable
        if (po?.ProcurementRequestId != null)
        {
            var pr = await db.ProcurementRequests.FindAsync(po.ProcurementRequestId);
            if (pr != null && pr.Status == ProcurementStatus.Ordered)
            {
                pr.Status = po.Status == PurchaseOrderStatus.Completed ? ProcurementStatus.Received : ProcurementStatus.Ordered;
                pr.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Confirm", EntityName = "GoodsReceipt", EntityId = id, NewValuesJson = "{\"Status\":\"Confirmed\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CancelGoodsReceiptAsync(Guid id)
    {
        var gr = await db.GoodsReceipts.FindAsync(id);
        if (gr is null || gr.TenantId != tenant.TenantId || gr.Status != GoodsReceiptStatus.Draft) return false;
        gr.Status = GoodsReceiptStatus.Cancelled; gr.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Cancel", EntityName = "GoodsReceipt", EntityId = id, NewValuesJson = "{\"Status\":\"Cancelled\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }
}
