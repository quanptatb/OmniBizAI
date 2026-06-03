using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Domain.StateMachines;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

/// <summary>F5.1/F5.2 — Work Order + Spare Part Usage</summary>
public class WorkOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly INumberingService _numbering;
    private readonly IAuditService _audit;
    private readonly NotificationService _notifications;

    public WorkOrderService(
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

    // ── LIST / DETAIL ─────────────────────────────────────────────────────────

    public async Task<WorkOrderListViewModel> GetListAsync(WorkOrderStatus? status, Guid? equipmentId, Guid? technicianId)
    {
        var tid = _tenant.TenantId;
        var q = _db.WorkOrders
            .Include(w => w.Equipment)
            .Include(w => w.AssignedTechnician)
            .Where(w => w.TenantId == tid && !w.IsDeleted);

        if (status.HasValue) q = q.Where(w => w.Status == status.Value);
        if (equipmentId.HasValue) q = q.Where(w => w.EquipmentId == equipmentId.Value);
        if (technicianId.HasValue) q = q.Where(w => w.AssignedTechnicianId == technicianId.Value);

        var items = await q.OrderByDescending(w => w.CreatedAt)
            .Take(150)
            .Select(w => new WorkOrderListItem
            {
                Id = w.Id,
                Code = w.Code,
                Title = w.Title,
                EquipmentName = w.Equipment != null ? w.Equipment.Name : "",
                Type = w.Type,
                Status = w.Status,
                Priority = w.Priority,
                TechnicianName = w.AssignedTechnician != null ? w.AssignedTechnician.FullName : null,
                ScheduledStart = w.ScheduledStart,
                ScheduledEnd = w.ScheduledEnd,
                ActualHours = w.ActualHours,
                ActualCost = w.ActualCost
            })
            .ToListAsync();

        var counts = await _db.WorkOrders
            .Where(w => w.TenantId == tid && !w.IsDeleted)
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new WorkOrderListViewModel
        {
            Items = items,
            StatusFilter = status,
            EquipmentFilter = equipmentId,
            TechnicianFilter = technicianId,
            OpenCount = counts.FirstOrDefault(c => c.Status == WorkOrderStatus.Open)?.Count ?? 0,
            AssignedCount = counts.FirstOrDefault(c => c.Status == WorkOrderStatus.Assigned)?.Count ?? 0,
            InProgressCount = counts.FirstOrDefault(c => c.Status == WorkOrderStatus.InProgress)?.Count ?? 0,
            OnHoldCount = counts.FirstOrDefault(c => c.Status == WorkOrderStatus.OnHold)?.Count ?? 0,
            CompletedCount = counts.FirstOrDefault(c => c.Status == WorkOrderStatus.Completed)?.Count ?? 0,
            Equipments = await GetEquipmentOptionsAsync(),
            Technicians = await GetTechnicianOptionsAsync()
        };
    }

    public async Task<WorkOrderDetailViewModel?> GetDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var w = await _db.WorkOrders
            .Include(x => x.Equipment)
            .Include(x => x.RequestedByUser)
            .Include(x => x.AssignedTechnician)
            .Include(x => x.CompletedByUser)
            .Include(x => x.Incident)
            .Include(x => x.PmSchedule)
            .Include(x => x.ChecklistItems.OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.CompletedByUser)
            .Include(x => x.PartUsages)
                .ThenInclude(p => p.SparePart)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid && !x.IsDeleted);
        if (w == null) return null;

        return new WorkOrderDetailViewModel
        {
            Id = w.Id,
            Code = w.Code,
            Title = w.Title,
            Description = w.Description,
            EquipmentId = w.EquipmentId,
            EquipmentName = w.Equipment?.Name ?? "",
            Type = w.Type,
            Status = w.Status,
            Priority = w.Priority,
            RowVersion = w.RowVersion,
            RequestedByName = w.RequestedByUser?.FullName,
            TechnicianId = w.AssignedTechnicianId,
            TechnicianName = w.AssignedTechnician?.FullName,
            ScheduledStart = w.ScheduledStart,
            ScheduledEnd = w.ScheduledEnd,
            ActualStart = w.ActualStart,
            ActualEnd = w.ActualEnd,
            EstimatedHours = w.EstimatedHours,
            ActualHours = w.ActualHours,
            EstimatedCost = w.EstimatedCost,
            ActualCost = w.ActualCost,
            WorkDone = w.WorkDone,
            IncidentId = w.IncidentId,
            IncidentTitle = w.Incident?.Title,
            PmScheduleId = w.PmScheduleId,
            PmTaskName = w.PmSchedule?.TaskName,
            CompletedAt = w.CompletedAt,
            CompletedByName = w.CompletedByUser?.FullName,
            ChecklistItems = w.ChecklistItems.Select(c => new WorkOrderChecklistItemViewModel
            {
                Id = c.Id,
                Title = c.Title,
                SortOrder = c.SortOrder,
                IsCompleted = c.IsCompleted,
                CompletedAt = c.CompletedAt,
                CompletedByName = c.CompletedByUser?.FullName,
                Notes = c.Notes
            }).ToList(),
            PartUsages = w.PartUsages.Select(p => new WorkOrderPartUsageViewModel
            {
                Id = p.Id,
                SparePartId = p.SparePartId,
                SparePartCode = p.SparePart?.Code ?? "",
                SparePartName = p.SparePart?.Name ?? "",
                QuantityUsed = p.QuantityUsed,
                UnitCost = p.UnitCost,
                LineTotal = p.LineTotal
            }).ToList(),
            NextStatuses = WorkOrderStateMachine.NextStates(w.Status).ToList(),
            Technicians = await GetTechnicianOptionsAsync(),
            AvailableSpareParts = await GetSparePartOptionsAsync()
        };
    }

    // ── CREATE / EDIT ────────────────────────────────────────────────────────

    public async Task<WorkOrderCreateFormViewModel> GetCreateFormAsync(Guid? incidentId = null, Guid? pmScheduleId = null)
    {
        return new WorkOrderCreateFormViewModel
        {
            IncidentId = incidentId,
            PmScheduleId = pmScheduleId,
            Equipments = await GetEquipmentOptionsAsync(),
            Technicians = await GetTechnicianOptionsAsync(),
            ScheduledStart = DateTimeOffset.UtcNow,
            ScheduledEnd = DateTimeOffset.UtcNow.AddHours(2)
        };
    }

    public async Task<(bool Success, Guid Id, string Message)> CreateAsync(WorkOrderCreateFormViewModel vm)
    {
        var tid = _tenant.TenantId;
        if (vm.EquipmentId == Guid.Empty)
            return (false, Guid.Empty, "Thiết bị bắt buộc.");

        var code = await _numbering.NextAsync(NumberingSequenceKeys.WorkOrder, "WO-", 4, DateTime.UtcNow.Year);
        var wo = new WorkOrder
        {
            TenantId = tid,
            Code = code,
            EquipmentId = vm.EquipmentId,
            Type = vm.Type,
            Status = vm.AssignedTechnicianId.HasValue ? WorkOrderStatus.Assigned : WorkOrderStatus.Open,
            Priority = vm.Priority,
            Title = vm.Title,
            Description = vm.Description,
            RequestedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId,
            AssignedTechnicianId = vm.AssignedTechnicianId,
            ScheduledStart = vm.ScheduledStart,
            ScheduledEnd = vm.ScheduledEnd,
            EstimatedHours = vm.EstimatedHours,
            EstimatedCost = vm.EstimatedCost,
            IncidentId = vm.IncidentId,
            PmScheduleId = vm.PmScheduleId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };

        if (vm.ChecklistTitles != null)
        {
            var order = 0;
            foreach (var title in vm.ChecklistTitles.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                wo.ChecklistItems.Add(new WorkOrderChecklistItem
                {
                    TenantId = tid,
                    Title = title.Trim(),
                    SortOrder = order++,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
                });
            }
        }

        _db.WorkOrders.Add(wo);
        await _audit.LogAsync("WorkOrder", wo.Id, "Create",
            newValueObj: new { wo.Code, wo.EquipmentId, wo.Type, wo.Status, wo.AssignedTechnicianId, wo.Priority, wo.IncidentId, wo.PmScheduleId });
        await _db.SaveChangesAsync();

        if (wo.AssignedTechnicianId.HasValue)
        {
            await _notifications.SendAsync(
                $"Bạn được giao Work Order {wo.Code}",
                wo.Title,
                "WorkOrder",
                wo.Id,
                wo.AssignedTechnicianId.Value);
        }

        return (true, wo.Id, $"Đã tạo Work Order {wo.Code}.");
    }

    // ── TRANSITION ───────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> AssignAsync(Guid id, Guid technicianId)
    {
        var wo = await LoadAsync(id);
        if (wo == null) return (false, "Không tìm thấy Work Order.");
        if (!WorkOrderStateMachine.CanTransition(wo.Status, WorkOrderStatus.Assigned) && wo.Status != WorkOrderStatus.Assigned)
            return (false, $"Không thể giao ở trạng thái {wo.Status}.");

        var oldTech = wo.AssignedTechnicianId;
        var oldStatus = wo.Status;
        wo.AssignedTechnicianId = technicianId;
        if (wo.Status == WorkOrderStatus.Open) wo.Status = WorkOrderStatus.Assigned;
        wo.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.LogAsync("WorkOrder", wo.Id, "Assign",
            oldValueObj: new { OldTechnicianId = oldTech, OldStatus = oldStatus },
            newValueObj: new { wo.AssignedTechnicianId, wo.Status });

        var ok = await _db.SaveChangesWithConcurrencyAsync();
        if (ok)
        {
            await _notifications.SendAsync(
                $"Work Order {wo.Code} được giao cho bạn", wo.Title, "WorkOrder", wo.Id, technicianId);
        }
        return (ok, ok ? "Đã giao technician." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    public async Task<(bool Success, string Message)> StartAsync(Guid id)
        => await TransitionAsync(id, WorkOrderStatus.InProgress, applyTimestamp: w => w.ActualStart ??= DateTimeOffset.UtcNow);

    public async Task<(bool Success, string Message)> HoldAsync(Guid id)
        => await TransitionAsync(id, WorkOrderStatus.OnHold);

    public async Task<(bool Success, string Message)> CancelAsync(Guid id, string? reason)
        => await TransitionAsync(id, WorkOrderStatus.Cancelled, extra: new { Reason = reason });

    public async Task<(bool Success, string Message)> CompleteAsync(Guid id, WorkOrderCompleteViewModel vm)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.PartUsages)
            .Include(w => w.Equipment)
            .Include(w => w.ChecklistItems)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == _tenant.TenantId && !w.IsDeleted);
        if (wo == null) return (false, "Không tìm thấy Work Order.");
        if (!WorkOrderStateMachine.CanTransition(wo.Status, WorkOrderStatus.Completed))
            return (false, $"Không thể hoàn thành ở trạng thái {wo.Status}.");

        var partsCost = wo.PartUsages.Sum(p => p.LineTotal ?? 0m);
        var oldStatus = wo.Status;
        wo.Status = WorkOrderStatus.Completed;
        wo.ActualEnd = DateTimeOffset.UtcNow;
        wo.ActualStart ??= vm.ActualStart ?? DateTimeOffset.UtcNow.AddHours(-1);
        wo.ActualHours = vm.ActualHours ?? (decimal)(wo.ActualEnd!.Value - wo.ActualStart!.Value).TotalHours;
        wo.ActualCost = (vm.LaborCost ?? 0m) + partsCost;
        wo.WorkDone = vm.WorkDone;
        wo.CompletedAt = DateTimeOffset.UtcNow;
        wo.CompletedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId;
        wo.UpdatedAt = DateTimeOffset.UtcNow;

        // Cost ledger cho equipment
        if (wo.Equipment != null && wo.ActualCost.HasValue && wo.ActualCost.Value > 0)
        {
            var costType = wo.Type switch
            {
                WorkOrderType.Preventive => EquipmentCostType.Maintenance,
                WorkOrderType.Corrective or WorkOrderType.Emergency => EquipmentCostType.Repair,
                _ => EquipmentCostType.Other
            };
            _db.EquipmentCostLedgers.Add(new EquipmentCostLedger
            {
                TenantId = wo.TenantId,
                EquipmentId = wo.EquipmentId,
                CostType = costType,
                Amount = wo.ActualCost.Value,
                OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow),
                SourceType = "WorkOrder",
                SourceId = wo.Id,
                Notes = $"WO {wo.Code}: {wo.Title}",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
            });
        }

        // Tạo MaintenanceRecord history
        var record = new MaintenanceRecord
        {
            TenantId = wo.TenantId,
            EquipmentId = wo.EquipmentId,
            MaintenanceType = wo.Type switch
            {
                WorkOrderType.Preventive => MaintenanceType.Preventive,
                WorkOrderType.Corrective or WorkOrderType.Emergency => MaintenanceType.Corrective,
                WorkOrderType.Predictive => MaintenanceType.Predictive,
                _ => MaintenanceType.Inspection
            },
            ScheduledDate = DateOnly.FromDateTime((wo.ScheduledStart ?? wo.CreatedAt).Date),
            CompletedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = MaintenanceRecordStatus.Completed,
            Description = wo.Title,
            WorkDone = wo.WorkDone,
            Cost = wo.ActualCost,
            TechnicianUserId = wo.AssignedTechnicianId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        _db.MaintenanceRecords.Add(record);

        await _audit.LogAsync("WorkOrder", wo.Id, "Complete",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { wo.Status, wo.ActualHours, wo.ActualCost, wo.WorkDone },
            extra: new { PartsCost = partsCost, LaborCost = vm.LaborCost, MaintenanceRecordId = record.Id });

        var ok = await _db.SaveChangesWithConcurrencyAsync();
        return (ok, ok ? $"Đã hoàn thành Work Order {wo.Code}." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    private async Task<(bool Success, string Message)> TransitionAsync(
        Guid id, WorkOrderStatus next, Action<WorkOrder>? applyTimestamp = null, object? extra = null)
    {
        var wo = await LoadAsync(id);
        if (wo == null) return (false, "Không tìm thấy Work Order.");
        if (!WorkOrderStateMachine.CanTransition(wo.Status, next))
            return (false, $"Không thể chuyển {wo.Status} → {next}.");
        var oldStatus = wo.Status;
        wo.Status = next;
        applyTimestamp?.Invoke(wo);
        wo.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("WorkOrder", wo.Id, $"Transition.{next}",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = next },
            extra: extra);
        var ok = await _db.SaveChangesWithConcurrencyAsync();
        return (ok, ok ? $"Đã chuyển sang {next}." : ConcurrencySaveExtensions.StaleRecordMessage);
    }

    // ── CHECKLIST ────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> ToggleChecklistAsync(Guid workOrderId, Guid itemId, bool completed)
    {
        var item = await _db.WorkOrderChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == workOrderId && i.TenantId == _tenant.TenantId && !i.IsDeleted);
        if (item == null) return (false, "Không tìm thấy mục checklist.");

        item.IsCompleted = completed;
        item.CompletedAt = completed ? DateTimeOffset.UtcNow : null;
        item.CompletedByUserId = completed ? (_tenant.UserId == Guid.Empty ? null : _tenant.UserId) : null;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.LogAsync("WorkOrderChecklistItem", item.Id, "Toggle",
            newValueObj: new { item.IsCompleted, item.CompletedAt });
        await _db.SaveChangesAsync();
        return (true, completed ? "Đã đánh dấu hoàn thành." : "Đã bỏ đánh dấu.");
    }

    // ── PART USAGE (F5.2) ────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> AddPartUsageAsync(Guid workOrderId, Guid sparePartId, int qty)
    {
        if (qty <= 0) return (false, "Số lượng phải > 0.");
        var tid = _tenant.TenantId;
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId && w.TenantId == tid && !w.IsDeleted);
        if (wo == null) return (false, "Không tìm thấy Work Order.");
        if (WorkOrderStateMachine.IsTerminal(wo.Status))
            return (false, $"Work Order đã {wo.Status}, không thể thêm phụ tùng.");

        var part = await _db.SpareParts.FirstOrDefaultAsync(p => p.Id == sparePartId && p.TenantId == tid && !p.IsDeleted);
        if (part == null) return (false, "Không tìm thấy phụ tùng.");
        if (part.StockQuantity < qty)
            return (false, $"Tồn kho phụ tùng {part.Code} chỉ còn {part.StockQuantity}. Hãy tạo Spare Part Requisition.");

        var unitCost = part.UnitPrice;
        var line = new WorkOrderSparePartUsage
        {
            TenantId = tid,
            WorkOrderId = workOrderId,
            SparePartId = sparePartId,
            QuantityUsed = qty,
            UnitCost = unitCost,
            LineTotal = unitCost.HasValue ? unitCost.Value * qty : null,
            RecordedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        _db.WorkOrderSparePartUsages.Add(line);

        // Giảm tồn kho ngay
        var oldStock = part.StockQuantity;
        part.StockQuantity = Math.Max(0, part.StockQuantity - qty);
        part.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.LogAsync("WorkOrderSparePartUsage", line.Id, "AddPartUsage",
            newValueObj: new { line.WorkOrderId, line.SparePartId, line.QuantityUsed, line.UnitCost },
            extra: new { OldStock = oldStock, NewStock = part.StockQuantity, PartCode = part.Code });

        await _db.SaveChangesAsync();
        return (true, $"Đã ghi nhận sử dụng {qty} {part.Unit} {part.Code}.");
    }

    public async Task<(bool Success, string Message)> RemovePartUsageAsync(Guid usageId)
    {
        var tid = _tenant.TenantId;
        var line = await _db.WorkOrderSparePartUsages
            .Include(l => l.WorkOrder)
            .Include(l => l.SparePart)
            .FirstOrDefaultAsync(l => l.Id == usageId && l.TenantId == tid && !l.IsDeleted);
        if (line == null) return (false, "Không tìm thấy dòng.");
        if (line.WorkOrder != null && WorkOrderStateMachine.IsTerminal(line.WorkOrder.Status))
            return (false, "Work Order đã đóng, không thể hoàn lại phụ tùng.");

        if (line.SparePart != null)
        {
            line.SparePart.StockQuantity += line.QuantityUsed;
            line.SparePart.UpdatedAt = DateTimeOffset.UtcNow;
        }
        line.IsDeleted = true;
        line.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.LogAsync("WorkOrderSparePartUsage", line.Id, "RemovePartUsage",
            extra: new { ReturnedQty = line.QuantityUsed, SparePartId = line.SparePartId });
        await _db.SaveChangesAsync();
        return (true, "Đã hoàn phụ tùng về kho.");
    }

    // ── AUTO-GEN từ Incident/PM ──────────────────────────────────────────────

    public async Task<Guid?> CreateFromIncidentAsync(Guid incidentId)
    {
        var inc = await _db.MaintenanceIncidents
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == incidentId && i.TenantId == _tenant.TenantId && !i.IsDeleted);
        if (inc == null) return null;

        var code = await _numbering.NextAsync(NumberingSequenceKeys.WorkOrder, "WO-", 4, DateTime.UtcNow.Year);
        var priority = inc.Severity switch
        {
            IncidentSeverity.Critical => PriorityLevel.Critical,
            IncidentSeverity.High => PriorityLevel.High,
            IncidentSeverity.Medium => PriorityLevel.Normal,
            _ => PriorityLevel.Low
        };

        var wo = new WorkOrder
        {
            TenantId = inc.TenantId,
            Code = code,
            EquipmentId = inc.EquipmentId,
            Type = inc.Severity == IncidentSeverity.Critical ? WorkOrderType.Emergency : WorkOrderType.Corrective,
            Status = inc.AssignedTechnicianId.HasValue ? WorkOrderStatus.Assigned : WorkOrderStatus.Open,
            Priority = priority,
            Title = $"Sửa chữa: {inc.Title}",
            Description = inc.Description,
            RequestedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId,
            AssignedTechnicianId = inc.AssignedTechnicianId,
            ScheduledStart = DateTimeOffset.UtcNow,
            IncidentId = inc.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        _db.WorkOrders.Add(wo);
        await _audit.LogAsync("WorkOrder", wo.Id, "AutoCreateFromIncident",
            newValueObj: new { wo.Code, wo.IncidentId, wo.Type, wo.Priority });
        await _db.SaveChangesAsync();

        if (wo.AssignedTechnicianId.HasValue)
        {
            await _notifications.SendAsync(
                $"Work Order {wo.Code} (từ incident {inc.Title})", wo.Title, "WorkOrder", wo.Id, wo.AssignedTechnicianId.Value);
        }
        return wo.Id;
    }

    public async Task<Guid?> CreateFromPmScheduleAsync(Guid pmId)
    {
        var pm = await _db.PmSchedules
            .Include(p => p.Equipment)
            .FirstOrDefaultAsync(p => p.Id == pmId && p.TenantId == _tenant.TenantId && !p.IsDeleted);
        if (pm == null) return null;

        var code = await _numbering.NextAsync(NumberingSequenceKeys.WorkOrder, "WO-", 4, DateTime.UtcNow.Year);
        var wo = new WorkOrder
        {
            TenantId = pm.TenantId,
            Code = code,
            EquipmentId = pm.EquipmentId,
            Type = WorkOrderType.Preventive,
            Status = pm.AssignedTechnicianId.HasValue ? WorkOrderStatus.Assigned : WorkOrderStatus.Open,
            Priority = PriorityLevel.Normal,
            Title = $"PM: {pm.TaskName}",
            Description = pm.Instructions,
            RequestedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId,
            AssignedTechnicianId = pm.AssignedTechnicianId,
            ScheduledStart = pm.NextDueDate.HasValue ? new DateTimeOffset(pm.NextDueDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.UtcNow,
            EstimatedHours = pm.EstimatedDurationMinutes.HasValue ? (decimal)pm.EstimatedDurationMinutes.Value / 60m : null,
            PmScheduleId = pm.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        _db.WorkOrders.Add(wo);
        await _audit.LogAsync("WorkOrder", wo.Id, "AutoCreateFromPm",
            newValueObj: new { wo.Code, wo.PmScheduleId, wo.Type });
        await _db.SaveChangesAsync();
        return wo.Id;
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────

    private async Task<WorkOrder?> LoadAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        return await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tid && !w.IsDeleted);
    }

    private async Task<List<SelectOption>> GetEquipmentOptionsAsync()
    {
        var tid = _tenant.TenantId;
        return await _db.Equipments.Where(e => e.TenantId == tid && !e.IsDeleted)
            .OrderBy(e => e.Code)
            .Select(e => new SelectOption { Value = e.Id.ToString(), Text = $"{e.Code} — {e.Name}" })
            .ToListAsync();
    }

    private async Task<List<SelectOption>> GetTechnicianOptionsAsync()
    {
        var tid = _tenant.TenantId;
        return await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();
    }

    private async Task<List<WorkOrderSparePartOption>> GetSparePartOptionsAsync()
    {
        var tid = _tenant.TenantId;
        return await _db.SpareParts.Where(p => p.TenantId == tid && !p.IsDeleted)
            .OrderBy(p => p.Code)
            .Select(p => new WorkOrderSparePartOption
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Unit = p.Unit,
                StockQuantity = p.StockQuantity,
                UnitPrice = p.UnitPrice
            }).ToListAsync();
    }
}
