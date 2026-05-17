using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class InventoryService(ApplicationDbContext db, ITenantContext tenant)
{
    // ── Goods Issue (Xuất kho) ──────────────────────────────────────────────
    public async Task<GoodsIssueListViewModel> GetGoodsIssuesAsync(string? search, string? status, string? type)
    {
        var tid = tenant.TenantId;
        var baseQ = db.GoodsIssues.Where(gi => gi.TenantId == tid && !gi.IsDeleted);
        var draftCount = await baseQ.CountAsync(gi => gi.Status == GoodsIssueStatus.Draft);
        var confirmedCount = await baseQ.CountAsync(gi => gi.Status == GoodsIssueStatus.Confirmed);
        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(gi => gi.IssueNo.Contains(search) || (gi.OperationRequest != null && gi.OperationRequest.RequestNo.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<GoodsIssueStatus>(status, out var st))
            q = q.Where(gi => gi.Status == st);
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(gi => gi.IssueType == type);
        var items = await q.OrderByDescending(gi => gi.CreatedAt)
            .Select(gi => new GoodsIssueListItem
            {
                Id = gi.Id, IssueNo = gi.IssueNo, IssueType = gi.IssueType,
                OperationRequestNo = gi.OperationRequest != null ? gi.OperationRequest.RequestNo : null,
                CustomerName = gi.Customer != null ? gi.Customer.Name : null,
                Department = gi.OrganizationUnit != null ? gi.OrganizationUnit.Name : null,
                Status = gi.Status.ToString(), IssueDate = gi.IssueDate, WarehouseLocation = gi.WarehouseLocation,
                IssuedBy = gi.IssuedByUser != null ? gi.IssuedByUser.FullName : "",
                LineCount = gi.Lines.Count(l => !l.IsDeleted),
                TotalIssuedQty = gi.Lines.Where(l => !l.IsDeleted).Sum(l => l.IssuedQuantity),
                CreatedAt = gi.CreatedAt
            }).ToListAsync();
        return new GoodsIssueListViewModel
        {
            Items = items, TotalCount = items.Count, DraftCount = draftCount, ConfirmedCount = confirmedCount,
            SearchTerm = search, StatusFilter = status, TypeFilter = type
        };
    }

    public async Task<GoodsIssueDetailViewModel?> GetGoodsIssueDetailAsync(Guid id)
    {
        var gi = await db.GoodsIssues
            .Include(g => g.Lines.Where(l => !l.IsDeleted)).ThenInclude(l => l.ProductService)
            .Include(g => g.OperationRequest).Include(g => g.Customer)
            .Include(g => g.OrganizationUnit).Include(g => g.IssuedByUser)
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenant.TenantId && !g.IsDeleted);
        if (gi is null) return null;

        var activityLog = await db.AuditLogs
            .Where(a => a.TenantId == tenant.TenantId && a.EntityId == id && a.EntityName == "GoodsIssue")
            .OrderByDescending(a => a.CreatedAt).Take(15)
            .Select(a => new ActivityLogItem { UserName = a.UserName, Action = a.Action, Details = a.NewValuesJson, OccurredAt = a.CreatedAt })
            .ToListAsync();

        return new GoodsIssueDetailViewModel
        {
            Id = gi.Id, IssueNo = gi.IssueNo, IssueType = gi.IssueType,
            OperationRequestNo = gi.OperationRequest?.RequestNo, OperationRequestId = gi.OperationRequestId,
            CustomerName = gi.Customer?.Name, Department = gi.OrganizationUnit?.Name,
            Status = gi.Status.ToString(), IssueDate = gi.IssueDate,
            WarehouseLocation = gi.WarehouseLocation, Destination = gi.Destination, Note = gi.Note,
            IssuedBy = gi.IssuedByUser?.FullName ?? "", CreatedAt = gi.CreatedAt,
            Lines = gi.Lines.Select(l => new GoodsIssueLineDisplay
            {
                Id = l.Id, ItemName = l.ItemName, RequestedQuantity = l.RequestedQuantity,
                IssuedQuantity = l.IssuedQuantity, UnitOfMeasure = l.UnitOfMeasure, Note = l.Note,
                ProductName = l.ProductService?.Name, ProductCode = l.ProductService?.Code
            }).ToList(),
            ActivityLog = activityLog,
            CanConfirm = gi.Status == GoodsIssueStatus.Draft,
            CanCancel = gi.Status == GoodsIssueStatus.Draft,
            CanEdit = gi.Status == GoodsIssueStatus.Draft
        };
    }

    public async Task<GoodsIssueCreateViewModel> GetGoodsIssueCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new GoodsIssueCreateViewModel
        {
            OperationRequests = await db.OperationRequests
                .Where(o => o.TenantId == tid && !o.IsDeleted && (o.Status == OperationStatus.Approved || o.Status == OperationStatus.InProgress))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.RequestNo + " — " + o.Title })
                .ToListAsync(),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " - " + c.Name }).ToListAsync(),
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            Products = await db.ProductServices.Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.Code + " - " + p.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateGoodsIssueAsync(GoodsIssueCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.GoodsIssues.CountAsync(gi => gi.TenantId == tid);
        var entity = new GoodsIssue
        {
            TenantId = tid, IssueNo = $"GI-{DateTime.Today.Year}-{count + 1:D4}", IssueType = vm.IssueType,
            OperationRequestId = vm.OperationRequestId, CustomerId = vm.CustomerId, OrganizationUnitId = vm.OrganizationUnitId,
            IssuedByUserId = tenant.UserId, IssueDate = vm.IssueDate, WarehouseLocation = vm.WarehouseLocation,
            Destination = vm.Destination, Note = vm.Note, Status = GoodsIssueStatus.Draft,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        foreach (var line in vm.Lines.Where(l => l.IssuedQuantity > 0))
        {
            entity.Lines.Add(new GoodsIssueLine
            {
                TenantId = tid, ProductServiceId = line.ProductServiceId, ItemName = line.ItemName,
                RequestedQuantity = line.RequestedQuantity, IssuedQuantity = line.IssuedQuantity,
                UnitOfMeasure = line.UnitOfMeasure, Note = line.Note,
                CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        db.GoodsIssues.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "GoodsIssue", EntityId = entity.Id, NewValuesJson = $"{{\"IssueNo\":\"{entity.IssueNo}\",\"Type\":\"{vm.IssueType}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ConfirmGoodsIssueAsync(Guid id)
    {
        var gi = await db.GoodsIssues.FindAsync(id);
        if (gi is null || gi.TenantId != tenant.TenantId || gi.Status != GoodsIssueStatus.Draft) return false;
        gi.Status = GoodsIssueStatus.Confirmed; gi.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Confirm", EntityName = "GoodsIssue", EntityId = id, NewValuesJson = "{\"Status\":\"Confirmed\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CancelGoodsIssueAsync(Guid id)
    {
        var gi = await db.GoodsIssues.FindAsync(id);
        if (gi is null || gi.TenantId != tenant.TenantId || gi.Status != GoodsIssueStatus.Draft) return false;
        gi.Status = GoodsIssueStatus.Cancelled; gi.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Cancel", EntityName = "GoodsIssue", EntityId = id, NewValuesJson = "{\"Status\":\"Cancelled\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── Stock Dashboard & Alerts ─────────────────────────────────────────────
    private async Task<decimal> ComputeCurrentStockAsync(Guid productId)
    {
        var tid = tenant.TenantId;
        var received = await db.GoodsReceiptLines
            .Where(l => l.ProductServiceId == productId && !l.IsDeleted && l.GoodsReceipt!.TenantId == tid && l.GoodsReceipt.Status == GoodsReceiptStatus.Confirmed)
            .SumAsync(l => l.ReceivedQuantity - (l.RejectedQuantity ?? 0));
        var issued = await db.GoodsIssueLines
            .Where(l => l.ProductServiceId == productId && !l.IsDeleted && l.GoodsIssue!.TenantId == tid && l.GoodsIssue.Status == GoodsIssueStatus.Confirmed)
            .SumAsync(l => l.IssuedQuantity);
        return received - issued;
    }

    public async Task<StockDashboardViewModel> GetStockDashboardAsync(string? search, string? stockFilter, string? categoryFilter)
    {
        var tid = tenant.TenantId;
        var products = await db.ProductServices.Include(p => p.ProductCategory)
            .Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted && p.Type == "Product")
            .ToListAsync();

        // Batch compute stock levels
        var productIds = products.Select(p => p.Id).ToList();
        var receivedByProduct = await db.GoodsReceiptLines
            .Where(l => productIds.Contains(l.ProductServiceId!.Value) && !l.IsDeleted && l.GoodsReceipt!.TenantId == tid && l.GoodsReceipt.Status == GoodsReceiptStatus.Confirmed)
            .GroupBy(l => l.ProductServiceId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(l => l.ReceivedQuantity - (l.RejectedQuantity ?? 0)) })
            .ToListAsync();
        var issuedByProduct = await db.GoodsIssueLines
            .Where(l => productIds.Contains(l.ProductServiceId!.Value) && !l.IsDeleted && l.GoodsIssue!.TenantId == tid && l.GoodsIssue.Status == GoodsIssueStatus.Confirmed)
            .GroupBy(l => l.ProductServiceId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(l => l.IssuedQuantity) })
            .ToListAsync();

        var items = products.Select(p =>
        {
            var recv = receivedByProduct.FirstOrDefault(r => r.ProductId == p.Id)?.Total ?? 0;
            var iss = issuedByProduct.FirstOrDefault(i => i.ProductId == p.Id)?.Total ?? 0;
            return new StockItemViewModel
            {
                Id = p.Id, Code = p.Code, Name = p.Name, Type = p.Type,
                CategoryName = p.ProductCategory?.Name,
                CurrentStock = recv - iss, ReorderPoint = p.ReorderPoint,
                SafetyStock = p.SafetyStock, MaxStock = p.MaxStock,
                StandardPrice = p.StandardPrice
            };
        }).ToList();

        // Filters
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(i => i.Code.Contains(search, StringComparison.OrdinalIgnoreCase) || i.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(categoryFilter))
            items = items.Where(i => i.CategoryName == categoryFilter).ToList();
        if (!string.IsNullOrWhiteSpace(stockFilter) && stockFilter != "All")
            items = items.Where(i => i.StockStatus == stockFilter).ToList();

        var allItems = products.Select(p =>
        {
            var recv = receivedByProduct.FirstOrDefault(r => r.ProductId == p.Id)?.Total ?? 0;
            var iss = issuedByProduct.FirstOrDefault(i => i.ProductId == p.Id)?.Total ?? 0;
            return new StockItemViewModel { CurrentStock = recv - iss, ReorderPoint = p.ReorderPoint, SafetyStock = p.SafetyStock, MaxStock = p.MaxStock };
        }).ToList();

        var activeAlerts = await db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active);
        var recentAlerts = await db.StockAlerts.Include(a => a.ProductService).Include(a => a.AcknowledgedByUser)
            .Where(a => a.TenantId == tid && !a.IsDeleted).OrderByDescending(a => a.CreatedAt).Take(10)
            .Select(a => new StockAlertListItem
            {
                Id = a.Id, ProductCode = a.ProductService!.Code, ProductName = a.ProductService.Name,
                AlertType = a.AlertType, CurrentStock = a.CurrentStock, Threshold = a.Threshold,
                Message = a.Message, Status = a.Status.ToString(), CreatedAt = a.CreatedAt,
                AcknowledgedAt = a.AcknowledgedAt, AcknowledgedBy = a.AcknowledgedByUser != null ? a.AcknowledgedByUser.FullName : null
            }).ToListAsync();

        var categories = products.Where(p => p.ProductCategory != null).Select(p => p.ProductCategory!.Name).Distinct()
            .Select(c => new SelectOption { Value = c, Text = c }).ToList();

        return new StockDashboardViewModel
        {
            Items = items, TotalProducts = allItems.Count,
            LowStockCount = allItems.Count(i => i.StockStatus == "Low"),
            CriticalStockCount = allItems.Count(i => i.StockStatus == "Critical"),
            OverstockCount = allItems.Count(i => i.StockStatus == "Overstock"),
            HealthyCount = allItems.Count(i => i.StockStatus == "Healthy"),
            ActiveAlertCount = activeAlerts, RecentAlerts = recentAlerts,
            SearchTerm = search, StockFilter = stockFilter, CategoryFilter = categoryFilter,
            Categories = categories
        };
    }

    public async Task<int> GenerateStockAlertsAsync()
    {
        var tid = tenant.TenantId;
        var products = await db.ProductServices
            .Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted && p.Type == "Product" && (p.ReorderPoint > 0 || p.SafetyStock > 0 || p.MaxStock > 0))
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();
        var receivedMap = await db.GoodsReceiptLines
            .Where(l => productIds.Contains(l.ProductServiceId!.Value) && !l.IsDeleted && l.GoodsReceipt!.TenantId == tid && l.GoodsReceipt.Status == GoodsReceiptStatus.Confirmed)
            .GroupBy(l => l.ProductServiceId).Select(g => new { PId = g.Key, T = g.Sum(l => l.ReceivedQuantity - (l.RejectedQuantity ?? 0)) }).ToListAsync();
        var issuedMap = await db.GoodsIssueLines
            .Where(l => productIds.Contains(l.ProductServiceId!.Value) && !l.IsDeleted && l.GoodsIssue!.TenantId == tid && l.GoodsIssue.Status == GoodsIssueStatus.Confirmed)
            .GroupBy(l => l.ProductServiceId).Select(g => new { PId = g.Key, T = g.Sum(l => l.IssuedQuantity) }).ToListAsync();

        // Get existing active alerts to avoid duplicates
        var existingAlerts = await db.StockAlerts
            .Where(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active)
            .Select(a => new { a.ProductServiceId, a.AlertType }).ToListAsync();

        int created = 0;
        foreach (var p in products)
        {
            var recv = receivedMap.FirstOrDefault(r => r.PId == p.Id)?.T ?? 0;
            var iss = issuedMap.FirstOrDefault(i => i.PId == p.Id)?.T ?? 0;
            var stock = recv - iss;

            // Critical: stock <= SafetyStock
            if (p.SafetyStock > 0 && stock <= p.SafetyStock && !existingAlerts.Any(e => e.ProductServiceId == p.Id && e.AlertType == "Critical"))
            {
                db.StockAlerts.Add(new StockAlert { TenantId = tid, ProductServiceId = p.Id, AlertType = "Critical", CurrentStock = stock, Threshold = p.SafetyStock, Message = $"⚠️ {p.Code} - {p.Name}: Tồn kho {stock:N0} ≤ mức an toàn {p.SafetyStock:N0}", CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow });
                created++;
            }
            // Low: stock <= ReorderPoint (but not Critical)
            else if (p.ReorderPoint > 0 && stock <= p.ReorderPoint && stock > p.SafetyStock && !existingAlerts.Any(e => e.ProductServiceId == p.Id && e.AlertType == "Low"))
            {
                db.StockAlerts.Add(new StockAlert { TenantId = tid, ProductServiceId = p.Id, AlertType = "Low", CurrentStock = stock, Threshold = p.ReorderPoint, Message = $"📉 {p.Code} - {p.Name}: Tồn kho {stock:N0} ≤ điểm đặt hàng {p.ReorderPoint:N0}", CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow });
                created++;
            }
            // Overstock: stock >= MaxStock
            if (p.MaxStock > 0 && stock >= p.MaxStock && !existingAlerts.Any(e => e.ProductServiceId == p.Id && e.AlertType == "Overstock"))
            {
                db.StockAlerts.Add(new StockAlert { TenantId = tid, ProductServiceId = p.Id, AlertType = "Overstock", CurrentStock = stock, Threshold = p.MaxStock, Message = $"📈 {p.Code} - {p.Name}: Tồn kho {stock:N0} ≥ mức tối đa {p.MaxStock:N0}", CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow });
                created++;
            }

            // Auto-resolve alerts that are no longer applicable
            var resolved = await db.StockAlerts.Where(a => a.TenantId == tid && a.ProductServiceId == p.Id && !a.IsDeleted && a.Status == StockAlertStatus.Active).ToListAsync();
            foreach (var alert in resolved)
            {
                bool shouldResolve = alert.AlertType switch
                {
                    "Critical" => stock > p.SafetyStock,
                    "Low" => stock > p.ReorderPoint,
                    "Overstock" => stock < p.MaxStock,
                    _ => false
                };
                if (shouldResolve) { alert.Status = StockAlertStatus.Resolved; alert.ResolvedAt = DateTimeOffset.UtcNow; }
            }
        }

        if (created > 0) await db.SaveChangesAsync();
        return created;
    }

    public async Task<bool> AcknowledgeAlertAsync(Guid alertId)
    {
        var alert = await db.StockAlerts.FindAsync(alertId);
        if (alert is null || alert.TenantId != tenant.TenantId || alert.Status != StockAlertStatus.Active) return false;
        alert.Status = StockAlertStatus.Acknowledged; alert.AcknowledgedAt = DateTimeOffset.UtcNow; alert.AcknowledgedByUserId = tenant.UserId;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> ResolveAlertAsync(Guid alertId)
    {
        var alert = await db.StockAlerts.FindAsync(alertId);
        if (alert is null || alert.TenantId != tenant.TenantId) return false;
        alert.Status = StockAlertStatus.Resolved; alert.ResolvedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<SetStockThresholdsViewModel?> GetThresholdsFormAsync(Guid productId)
    {
        var p = await db.ProductServices.FirstOrDefaultAsync(x => x.Id == productId && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (p is null) return null;
        var stock = await ComputeCurrentStockAsync(productId);
        return new SetStockThresholdsViewModel { ProductId = p.Id, ProductCode = p.Code, ProductName = p.Name, CurrentStock = stock, ReorderPoint = p.ReorderPoint, SafetyStock = p.SafetyStock, MaxStock = p.MaxStock };
    }

    public async Task<bool> SaveThresholdsAsync(SetStockThresholdsViewModel vm)
    {
        var p = await db.ProductServices.FindAsync(vm.ProductId);
        if (p is null || p.TenantId != tenant.TenantId) return false;
        p.ReorderPoint = vm.ReorderPoint; p.SafetyStock = vm.SafetyStock; p.MaxStock = vm.MaxStock; p.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }
}
