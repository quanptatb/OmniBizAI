using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class CashBookService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<CashBookDashboardViewModel> GetDashboardAsync(string? search, string? type, string? status, string? category, DateOnly? from, DateOnly? to)
    {
        var tid = tenant.TenantId;
        var baseQ = db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted);

        var totalIncome = await baseQ.Where(t => t.TransactionType == "Income" && t.Status != CashTransactionStatus.Voided && t.Status != CashTransactionStatus.Rejected).SumAsync(t => t.Amount);
        var totalExpense = await baseQ.Where(t => t.TransactionType == "Expense" && t.Status != CashTransactionStatus.Voided && t.Status != CashTransactionStatus.Rejected).SumAsync(t => t.Amount);
        var recordedCount = await baseQ.CountAsync(t => t.Status == CashTransactionStatus.Recorded);
        var approvedCount = await baseQ.CountAsync(t => t.Status == CashTransactionStatus.Approved);

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.TransactionNo.Contains(search) || t.Description.Contains(search) || t.Category.Contains(search));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(t => t.TransactionType == type);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CashTransactionStatus>(status, out var st))
            q = q.Where(t => t.Status == st);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(t => t.Category == category);
        if (from.HasValue)
            q = q.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue)
            q = q.Where(t => t.TransactionDate <= to.Value);

        var items = await q.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAt)
            .Select(t => new CashTransactionListItem
            {
                Id = t.Id, TransactionNo = t.TransactionNo, TransactionType = t.TransactionType,
                Category = t.Category, Description = t.Description, Amount = t.Amount,
                TransactionDate = t.TransactionDate, PaymentMethod = t.PaymentMethod,
                ReferenceNo = t.ReferenceNo,
                CustomerName = t.Customer != null ? t.Customer.Name : null,
                VendorName = t.Vendor != null ? t.Vendor.Name : null,
                Department = t.OrganizationUnit != null ? t.OrganizationUnit.Name : null,
                Status = t.Status.ToString(),
                RecordedBy = t.RecordedByUser != null ? t.RecordedByUser.FullName : "",
                CreatedAt = t.CreatedAt
            }).ToListAsync();

        // Monthly summary (last 6 months)
        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5).AddDays(-(DateTime.Today.Day - 1)));
        var monthly = await baseQ.Where(t => t.TransactionDate >= sixMonthsAgo && t.Status != CashTransactionStatus.Voided && t.Status != CashTransactionStatus.Rejected)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.TransactionType })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.TransactionType, Total = g.Sum(t => t.Amount) })
            .ToListAsync();
        var months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).Select(d => new CashMonthSummary
        {
            Month = d.ToString("yyyy-MM"),
            Income = monthly.Where(m => m.Year == d.Year && m.Month == d.Month && m.TransactionType == "Income").Sum(m => m.Total),
            Expense = monthly.Where(m => m.Year == d.Year && m.Month == d.Month && m.TransactionType == "Expense").Sum(m => m.Total)
        }).ToList();

        var categories = await baseQ.Select(t => t.Category).Distinct().OrderBy(c => c)
            .Select(c => new SelectOption { Value = c, Text = c }).ToListAsync();

        return new CashBookDashboardViewModel
        {
            Items = items, TotalCount = items.Count, TotalIncome = totalIncome, TotalExpense = totalExpense,
            RecordedCount = recordedCount, ApprovedCount = approvedCount,
            SearchTerm = search, TypeFilter = type, StatusFilter = status, CategoryFilter = category,
            FromDate = from, ToDate = to, Categories = categories, MonthlySummary = months
        };
    }

    public async Task<CashTransactionDetailViewModel?> GetDetailAsync(Guid id)
    {
        var t = await db.CashTransactions
            .Include(x => x.Customer).Include(x => x.Vendor).Include(x => x.Budget)
            .Include(x => x.OrganizationUnit).Include(x => x.RecordedByUser).Include(x => x.ApprovedByUser)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (t is null) return null;

        var activityLog = await db.AuditLogs
            .Where(a => a.TenantId == tenant.TenantId && a.EntityId == id && a.EntityName == "CashTransaction")
            .OrderByDescending(a => a.CreatedAt).Take(15)
            .Select(a => new ActivityLogItem { UserName = a.UserName, Action = a.Action, Details = a.NewValuesJson, OccurredAt = a.CreatedAt })
            .ToListAsync();

        return new CashTransactionDetailViewModel
        {
            Id = t.Id, TransactionNo = t.TransactionNo, TransactionType = t.TransactionType,
            Category = t.Category, Description = t.Description, Amount = t.Amount,
            TransactionDate = t.TransactionDate, PaymentMethod = t.PaymentMethod,
            ReferenceNo = t.ReferenceNo, CustomerName = t.Customer?.Name,
            VendorName = t.Vendor?.Name, BudgetName = t.Budget?.Name,
            Department = t.OrganizationUnit?.Name, Note = t.Note,
            Status = t.Status.ToString(), RecordedBy = t.RecordedByUser?.FullName ?? "",
            ApprovedBy = t.ApprovedByUser?.FullName, ApprovedAt = t.ApprovedAt,
            CreatedAt = t.CreatedAt, ActivityLog = activityLog,
            CanApprove = t.Status == CashTransactionStatus.Recorded,
            CanReject = t.Status == CashTransactionStatus.Recorded,
            CanVoid = t.Status == CashTransactionStatus.Recorded || t.Status == CashTransactionStatus.Approved
        };
    }

    public async Task<CashTransactionCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new CashTransactionCreateViewModel
        {
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " - " + c.Name }).ToListAsync(),
            Vendors = await db.Vendors.Where(v => v.TenantId == tid && v.IsActive && !v.IsDeleted)
                .Select(v => new SelectOption { Value = v.Id.ToString(), Text = v.Code + " - " + v.Name }).ToListAsync(),
            Budgets = await db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.Status == BudgetStatus.Active)
                .Select(b => new SelectOption { Value = b.Id.ToString(), Text = b.Name }).ToListAsync(),
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateAsync(CashTransactionCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var prefix = vm.TransactionType == "Income" ? "TH" : "CH";
        var count = await db.CashTransactions.CountAsync(t => t.TenantId == tid && t.TransactionType == vm.TransactionType);
        var entity = new CashTransaction
        {
            TenantId = tid, TransactionNo = $"{prefix}-{DateTime.Today.Year}-{count + 1:D4}",
            TransactionType = vm.TransactionType, Category = vm.Category, Description = vm.Description,
            Amount = vm.Amount, TransactionDate = vm.TransactionDate, PaymentMethod = vm.PaymentMethod,
            ReferenceNo = vm.ReferenceNo, CustomerId = vm.CustomerId, VendorId = vm.VendorId,
            BudgetId = vm.BudgetId, OrganizationUnitId = vm.OrganizationUnitId,
            RecordedByUserId = tenant.UserId, Note = vm.Note,
            Status = CashTransactionStatus.Recorded, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.CashTransactions.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "CashTransaction", EntityId = entity.Id, NewValuesJson = $"{{\"No\":\"{entity.TransactionNo}\",\"Type\":\"{vm.TransactionType}\",\"Amount\":{vm.Amount}}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ApproveAsync(Guid id)
    {
        var t = await db.CashTransactions.FindAsync(id);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != CashTransactionStatus.Recorded) return false;
        t.Status = CashTransactionStatus.Approved; t.ApprovedByUserId = tenant.UserId; t.ApprovedAt = DateTimeOffset.UtcNow; t.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Approve", EntityName = "CashTransaction", EntityId = id, NewValuesJson = "{\"Status\":\"Approved\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> RejectAsync(Guid id, string? reason)
    {
        var t = await db.CashTransactions.FindAsync(id);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != CashTransactionStatus.Recorded) return false;
        t.Status = CashTransactionStatus.Rejected; t.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Reject", EntityName = "CashTransaction", EntityId = id, NewValuesJson = $"{{\"Status\":\"Rejected\",\"Reason\":\"{reason ?? ""}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> VoidAsync(Guid id)
    {
        var t = await db.CashTransactions.FindAsync(id);
        if (t is null || t.TenantId != tenant.TenantId || (t.Status != CashTransactionStatus.Recorded && t.Status != CashTransactionStatus.Approved)) return false;
        t.Status = CashTransactionStatus.Voided; t.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Void", EntityName = "CashTransaction", EntityId = id, NewValuesJson = "{\"Status\":\"Voided\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }
}
