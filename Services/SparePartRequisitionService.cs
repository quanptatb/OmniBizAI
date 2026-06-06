using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Domain.StateMachines;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

/// <summary>F5.3 — Phiếu yêu cầu phụ tùng</summary>
public class SparePartRequisitionService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly INumberingService _numbering;
    private readonly IAuditService _audit;
    private readonly NotificationService _notifications;

    public SparePartRequisitionService(
        ApplicationDbContext db,
        ITenantContext tenant,
        INumberingService numbering,
        IAuditService audit,
        NotificationService notifications)
    {
        _db = db;
        _tenant = tenant;
        _numbering = numbering;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<SparePartRequisitionListViewModel> GetListAsync(SparePartRequisitionStatus? status)
    {
        var tid = _tenant.TenantId;
        var q = _db.SparePartRequisitions
            .Include(r => r.RequestedByUser)
            .Include(r => r.LinkedWorkOrder)
            .Include(r => r.Lines)
            .Where(r => r.TenantId == tid && !r.IsDeleted);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);

        var items = await q.OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new SparePartRequisitionListItem
            {
                Id = r.Id,
                Code = r.Code,
                Status = r.Status,
                Reason = r.Reason,
                RequestedByName = r.RequestedByUser != null ? r.RequestedByUser.FullName : null,
                LinkedWorkOrderCode = r.LinkedWorkOrder != null ? r.LinkedWorkOrder.Code : null,
                CreatedAt = r.CreatedAt,
                LineCount = r.Lines.Count(l => !l.IsDeleted),
                TotalQuantity = r.Lines.Where(l => !l.IsDeleted).Sum(l => l.Quantity),
                IsAutoReorder = r.IsAutoReorder
            }).ToListAsync();

        return new SparePartRequisitionListViewModel
        {
            Items = items,
            StatusFilter = status,
            DraftCount = await q.CountAsync(r => r.Status == SparePartRequisitionStatus.Draft),
            SubmittedCount = await q.CountAsync(r => r.Status == SparePartRequisitionStatus.Submitted),
            ApprovedCount = await q.CountAsync(r => r.Status == SparePartRequisitionStatus.Approved),
            IssuedCount = await q.CountAsync(r => r.Status == SparePartRequisitionStatus.Issued)
        };
    }

    public async Task<SparePartRequisitionDetailViewModel?> GetDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var r = await _db.SparePartRequisitions
            .Include(x => x.RequestedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.LinkedWorkOrder)
            .Include(x => x.IssuedGoodsIssue)
            .Include(x => x.Lines).ThenInclude(l => l.SparePart)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid && !x.IsDeleted);
        if (r == null) return null;

        return new SparePartRequisitionDetailViewModel
        {
            Id = r.Id,
            Code = r.Code,
            Status = r.Status,
            Reason = r.Reason,
            RequestedByName = r.RequestedByUser?.FullName,
            ApprovedByName = r.ApprovedByUser?.FullName,
            ApprovedAt = r.ApprovedAt,
            RejectionReason = r.RejectionReason,
            LinkedWorkOrderId = r.LinkedWorkOrderId,
            LinkedWorkOrderCode = r.LinkedWorkOrder?.Code,
            IssuedGoodsIssueNo = r.IssuedGoodsIssue?.IssueNo,
            IssuedAt = r.IssuedAt,
            IsAutoReorder = r.IsAutoReorder,
            Lines = r.Lines.Where(l => !l.IsDeleted).Select(l => new SparePartRequisitionLineViewModel
            {
                Id = l.Id,
                SparePartId = l.SparePartId,
                SparePartCode = l.SparePart?.Code ?? "",
                SparePartName = l.SparePart?.Name ?? "",
                Unit = l.SparePart?.Unit ?? "",
                Quantity = l.Quantity,
                StockOnHand = l.SparePart?.StockQuantity ?? 0,
                UnitCost = l.UnitCost,
                Notes = l.Notes
            }).ToList(),
            NextStatuses = SparePartRequisitionStateMachine.NextStates(r.Status).ToList()
        };
    }

    public async Task<SparePartRequisitionFormViewModel> GetCreateFormAsync(Guid? linkedWorkOrderId)
    {
        var tid = _tenant.TenantId;
        return new SparePartRequisitionFormViewModel
        {
            LinkedWorkOrderId = linkedWorkOrderId,
            Parts = await _db.SpareParts.Where(p => p.TenantId == tid && !p.IsDeleted)
                .OrderBy(p => p.Code)
                .Select(p => new WorkOrderSparePartOption
                {
                    Id = p.Id, Code = p.Code, Name = p.Name, Unit = p.Unit,
                    StockQuantity = p.StockQuantity, UnitPrice = p.UnitPrice
                }).ToListAsync(),
            WorkOrders = await _db.WorkOrders.Where(w => w.TenantId == tid && !w.IsDeleted && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)
                .OrderByDescending(w => w.CreatedAt).Take(50)
                .Select(w => new SelectOption { Value = w.Id.ToString(), Text = $"{w.Code} — {w.Title}" })
                .ToListAsync()
        };
    }

    public async Task<(bool Success, Guid Id, string Message)> CreateAsync(SparePartRequisitionFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Reason)) return (false, Guid.Empty, "Lý do bắt buộc.");
        var validLines = (vm.Lines ?? new()).Where(l => l.SparePartId != Guid.Empty && l.Quantity > 0).ToList();
        if (validLines.Count == 0) return (false, Guid.Empty, "Cần ít nhất 1 dòng phụ tùng.");

        var tid = _tenant.TenantId;
        var code = await _numbering.NextAsync(NumberingSequenceKeys.SparePartRequisition, "SPR-", 4, DateTime.UtcNow.Year);
        var entity = new SparePartRequisition
        {
            TenantId = tid,
            Code = code,
            Status = SparePartRequisitionStatus.Draft,
            Reason = vm.Reason,
            RequestedByUserId = _tenant.UserId,
            LinkedWorkOrderId = vm.LinkedWorkOrderId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        foreach (var l in validLines)
        {
            entity.Lines.Add(new SparePartRequisitionLine
            {
                TenantId = tid,
                SparePartId = l.SparePartId,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                Notes = l.Notes,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
            });
        }
        _db.SparePartRequisitions.Add(entity);
        await _audit.LogAsync("SparePartRequisition", entity.Id, "Create",
            newValueObj: new { entity.Code, entity.Reason, LineCount = validLines.Count, entity.LinkedWorkOrderId });
        await _db.SaveChangesAsync();
        return (true, entity.Id, $"Đã tạo phiếu {entity.Code}.");
    }

    public async Task<(bool Success, string Message)> SubmitAsync(Guid id)
        => await TransitionAsync(id, SparePartRequisitionStatus.Submitted, notifyManagers: true,
            notifyTitle: r => $"Phiếu phụ tùng {r.Code} chờ duyệt",
            notifyBody: r => r.Reason);

    public async Task<(bool Success, string Message)> ApproveAsync(Guid id, string? note)
    {
        var r = await Load(id);
        if (r == null) return (false, "Không tìm thấy phiếu.");
        if (!SparePartRequisitionStateMachine.CanTransition(r.Status, SparePartRequisitionStatus.Approved))
            return (false, $"Không thể duyệt ở trạng thái {r.Status}.");
        var old = r.Status;
        r.Status = SparePartRequisitionStatus.Approved;
        r.ApprovedByUserId = _tenant.UserId;
        r.ApprovedAt = DateTimeOffset.UtcNow;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("SparePartRequisition", r.Id, "Approve",
            oldValueObj: new { Status = old },
            newValueObj: new { r.Status, r.ApprovedByUserId, r.ApprovedAt },
            extra: new { Note = note });
        var ok = await _db.SaveChangesWithConcurrencyAsync();
        if (ok)
        {
            await _notifications.SendAsync($"Phiếu {r.Code} đã duyệt", r.Reason, "SparePartRequisition", r.Id, r.RequestedByUserId);
        }
        return (ok, ok ? "Đã duyệt phiếu." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    public async Task<(bool Success, string Message)> RejectAsync(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return (false, "Cần nhập lý do từ chối.");
        var r = await Load(id);
        if (r == null) return (false, "Không tìm thấy phiếu.");
        if (!SparePartRequisitionStateMachine.CanTransition(r.Status, SparePartRequisitionStatus.Rejected))
            return (false, $"Không thể từ chối ở trạng thái {r.Status}.");
        r.Status = SparePartRequisitionStatus.Rejected;
        r.RejectionReason = reason;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("SparePartRequisition", r.Id, "Reject", newValueObj: new { r.Status, r.RejectionReason });
        var ok = await _db.SaveChangesWithConcurrencyAsync();
        if (ok)
        {
            await _notifications.SendAsync($"Phiếu {r.Code} bị từ chối", reason, "SparePartRequisition", r.Id, r.RequestedByUserId);
        }
        return (ok, ok ? "Đã từ chối." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    public async Task<(bool Success, string Message)> CancelAsync(Guid id)
    {
        var r = await Load(id);
        if (r == null) return (false, "Không tìm thấy phiếu.");
        if (!SparePartRequisitionStateMachine.CanTransition(r.Status, SparePartRequisitionStatus.Cancelled))
            return (false, $"Không thể huỷ ở trạng thái {r.Status}.");
        r.Status = SparePartRequisitionStatus.Cancelled;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("SparePartRequisition", r.Id, "Cancel");
        var ok = await _db.SaveChangesWithConcurrencyAsync();
        return (ok, ok ? "Đã huỷ phiếu." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    /// <summary>Xuất kho → sinh GoodsIssue + giảm stock</summary>
    public async Task<(bool Success, string Message)> IssueAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var r = await _db.SparePartRequisitions
            .Include(x => x.Lines).ThenInclude(l => l.SparePart)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid && !x.IsDeleted);
        if (r == null) return (false, "Không tìm thấy phiếu.");
        if (!SparePartRequisitionStateMachine.CanTransition(r.Status, SparePartRequisitionStatus.Issued))
            return (false, $"Không thể xuất kho ở trạng thái {r.Status}.");

        // Validate stock
        foreach (var line in r.Lines.Where(l => !l.IsDeleted))
        {
            if (line.SparePart == null) continue;
            if (line.SparePart.StockQuantity < line.Quantity)
                return (false, $"Phụ tùng {line.SparePart.Code} chỉ còn {line.SparePart.StockQuantity}, không đủ {line.Quantity}.");
        }

        // Sinh GoodsIssue (link Inventory module)
        var gi = new GoodsIssue
        {
            TenantId = tid,
            IssueNo = $"GI-SPR-{r.Code}",
            IssueType = "Internal",
            IssuedByUserId = _tenant.UserId,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = GoodsIssueStatus.Confirmed,
            Note = $"Xuất kho theo phiếu {r.Code}: {r.Reason}",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        foreach (var line in r.Lines.Where(l => !l.IsDeleted))
        {
            if (line.SparePart == null) continue;
            gi.Lines.Add(new GoodsIssueLine
            {
                TenantId = tid,
                ItemName = $"{line.SparePart.Code} — {line.SparePart.Name}",
                RequestedQuantity = line.Quantity,
                IssuedQuantity = line.Quantity,
                UnitCost = line.UnitCost ?? line.SparePart.UnitPrice,
                LineAmount = (line.UnitCost ?? line.SparePart.UnitPrice) * line.Quantity,
                UnitOfMeasure = line.SparePart.Unit,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
            });
            // Giảm tồn kho
            var oldStock = line.SparePart.StockQuantity;
            line.SparePart.StockQuantity = Math.Max(0, line.SparePart.StockQuantity - line.Quantity);
            line.SparePart.UpdatedAt = DateTimeOffset.UtcNow;
            await _audit.LogAsync("SparePart", line.SparePart.Id, "IssueFromRequisition",
                oldValueObj: new { StockQuantity = oldStock },
                newValueObj: new { line.SparePart.StockQuantity },
                extra: new { RequisitionCode = r.Code, line.Quantity });
        }
        _db.GoodsIssues.Add(gi);

        r.Status = SparePartRequisitionStatus.Issued;
        r.IssuedGoodsIssueId = gi.Id;
        r.IssuedAt = DateTimeOffset.UtcNow;
        r.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.LogAsync("SparePartRequisition", r.Id, "Issue",
            newValueObj: new { r.Status, r.IssuedAt, GoodsIssueId = gi.Id });
        var ok = await _db.SaveChangesWithConcurrencyAsync();
        if (ok)
        {
            await _notifications.SendAsync($"Phiếu {r.Code} đã xuất kho", $"GI {gi.IssueNo}", "SparePartRequisition", r.Id, r.RequestedByUserId);
        }
        return (ok, ok ? $"Đã xuất kho theo phiếu {r.Code}." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    /// <summary>Reorder alert: tự sinh SPR Draft khi stock < min — gọi từ background hoặc nút thủ công</summary>
    public async Task<int> GenerateAutoReorderDraftsAsync()
    {
        var tid = _tenant.TenantId;
        var lowStockParts = await _db.SpareParts
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.StockQuantity < p.MinimumStock)
            .ToListAsync();
        if (lowStockParts.Count == 0) return 0;

        // Avoid duplicate: skip parts already in pending auto-reorder
        var pendingPartIds = await _db.SparePartRequisitions
            .Where(r => r.TenantId == tid && !r.IsDeleted && r.IsAutoReorder
                && (r.Status == SparePartRequisitionStatus.Draft || r.Status == SparePartRequisitionStatus.Submitted))
            .SelectMany(r => r.Lines.Where(l => !l.IsDeleted).Select(l => l.SparePartId))
            .Distinct().ToListAsync();
        var newParts = lowStockParts.Where(p => !pendingPartIds.Contains(p.Id)).ToList();
        if (newParts.Count == 0) return 0;

        var code = await _numbering.NextAsync(NumberingSequenceKeys.SparePartRequisition, "SPR-", 4, DateTime.UtcNow.Year);
        var req = new SparePartRequisition
        {
            TenantId = tid,
            Code = code,
            Status = SparePartRequisitionStatus.Draft,
            Reason = $"Tự sinh: {newParts.Count} phụ tùng dưới ngưỡng tồn kho.",
            RequestedByUserId = _tenant.UserId == Guid.Empty ? Guid.Empty : _tenant.UserId,
            IsAutoReorder = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        foreach (var p in newParts)
        {
            var topUpQty = Math.Max(1, p.MinimumStock * 2 - p.StockQuantity);
            req.Lines.Add(new SparePartRequisitionLine
            {
                TenantId = tid,
                SparePartId = p.Id,
                Quantity = topUpQty,
                UnitCost = p.UnitPrice,
                Notes = $"Tồn {p.StockQuantity} / min {p.MinimumStock}",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
            });
        }
        _db.SparePartRequisitions.Add(req);
        await _audit.LogAsync("SparePartRequisition", req.Id, "AutoReorderDraft",
            newValueObj: new { req.Code, PartCount = newParts.Count });
        await _db.SaveChangesAsync();
        return newParts.Count;
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private async Task<SparePartRequisition?> Load(Guid id)
    {
        var tid = _tenant.TenantId;
        return await _db.SparePartRequisitions
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tid && !r.IsDeleted);
    }

    private async Task<(bool Success, string Message)> TransitionAsync(
        Guid id, SparePartRequisitionStatus next,
        bool notifyManagers = false,
        Func<SparePartRequisition, string>? notifyTitle = null,
        Func<SparePartRequisition, string>? notifyBody = null)
    {
        var r = await Load(id);
        if (r == null) return (false, "Không tìm thấy phiếu.");
        if (!SparePartRequisitionStateMachine.CanTransition(r.Status, next))
            return (false, $"Không thể chuyển {r.Status} → {next}.");
        var old = r.Status;
        r.Status = next;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("SparePartRequisition", r.Id, $"Transition.{next}",
            oldValueObj: new { Status = old },
            newValueObj: new { Status = next });
        var ok = await _db.SaveChangesWithConcurrencyAsync();
        if (ok && notifyManagers && notifyTitle != null && notifyBody != null)
        {
            await _notifications.SendToManagersAsync(notifyTitle(r), notifyBody(r), "SparePartRequisition", r.Id);
        }
        return (ok, ok ? $"Đã chuyển sang {next}." : ConcurrencySaveExtensions.StaleRecordMessage);
    }
}
