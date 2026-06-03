using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class ResourceManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly GeminiService _gemini;
    private readonly INumberingService _numbering;
    private readonly IAuditService _audit;

    public ResourceManagementService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini, INumberingService numbering, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _gemini = gemini;
        _numbering = numbering;
        _audit = audit;
    }

    // ─── DASHBOARD ──────────────────────────────────────────────────────────

    public async Task<ResourceDashboardViewModel> GetDashboardAsync()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var equipmentCount = await _db.Equipments.CountAsync(e => e.TenantId == tid && !e.IsDeleted);
        var equipmentInMaintenance = await _db.Equipments.CountAsync(e => e.TenantId == tid && !e.IsDeleted && e.Status == EquipmentStatus.Maintenance);
        var overdueMaintenance = await _db.Equipments.CountAsync(e => e.TenantId == tid && !e.IsDeleted
            && e.NextMaintenanceDate.HasValue && e.NextMaintenanceDate.Value < today && e.Status != EquipmentStatus.Maintenance);

        var shiftCount = await _db.WorkShifts.CountAsync(s => s.TenantId == tid && !s.IsDeleted && s.IsActive);
        var todayAssignments = await _db.ShiftAssignments.CountAsync(s => s.TenantId == tid && !s.IsDeleted && s.WorkDate == today);

        var expiredCerts = await _db.EmployeeCertificates.CountAsync(c => c.TenantId == tid && !c.IsDeleted
            && c.ExpiryDate.HasValue && c.ExpiryDate.Value < today);
        var expiringCerts = await _db.EmployeeCertificates.CountAsync(c => c.TenantId == tid && !c.IsDeleted
            && c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(30));

        var workspaceCount = await _db.Workspaces.CountAsync(w => w.TenantId == tid && !w.IsDeleted && w.Status == WorkspaceStatus.Active);

        var upcomingMaintenance = await _db.MaintenanceRecords
            .Include(m => m.Equipment)
            .Where(m => m.TenantId == tid && !m.IsDeleted
                && m.Status == MaintenanceRecordStatus.Scheduled
                && m.ScheduledDate >= today && m.ScheduledDate <= today.AddDays(7))
            .OrderBy(m => m.ScheduledDate)
            .Take(5)
            .Select(m => new MaintenanceAlertItem
            {
                Id = m.Id,
                EquipmentName = m.Equipment != null ? m.Equipment.Name : "",
                EquipmentCode = m.Equipment != null ? m.Equipment.Code : "",
                MaintenanceType = m.MaintenanceType.ToString(),
                ScheduledDate = m.ScheduledDate,
                Status = m.Status.ToString()
            }).ToListAsync();

        var recentEquipments = await _db.Equipments
            .Where(e => e.TenantId == tid && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new EquipmentSummaryItem
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                Type = e.Type,
                Status = e.Status.ToString(),
                Location = e.Location,
                NextMaintenanceDate = e.NextMaintenanceDate
            }).ToListAsync();

        return new ResourceDashboardViewModel
        {
            EquipmentCount = equipmentCount,
            EquipmentInMaintenance = equipmentInMaintenance,
            OverdueMaintenanceCount = overdueMaintenance,
            ActiveShiftCount = shiftCount,
            TodayAssignmentCount = todayAssignments,
            ExpiredCertificateCount = expiredCerts,
            ExpiringCertificateCount = expiringCerts,
            WorkspaceCount = workspaceCount,
            UpcomingMaintenance = upcomingMaintenance,
            RecentEquipments = recentEquipments
        };
    }

    // ─── EQUIPMENT ──────────────────────────────────────────────────────────

    public async Task<List<EquipmentSummaryItem>> GetEquipmentsAsync(string? search, string? status, string? type)
    {
        var tid = _tenant.TenantId;
        var q = _db.Equipments.Where(e => e.TenantId == tid && !e.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Name.Contains(search) || e.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EquipmentStatus>(status, true, out var equipmentStatus))
            q = q.Where(e => e.Status == equipmentStatus);
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(e => e.Type == type);

        return await q.OrderBy(e => e.Code).Select(e => new EquipmentSummaryItem
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Type = e.Type,
            Status = e.Status.ToString(),
            Location = e.Location,
            Manufacturer = e.Manufacturer,
            Model = e.Model,
            PurchaseDate = e.PurchaseDate,
            NextMaintenanceDate = e.NextMaintenanceDate,
            LifespanYears = e.LifespanYears
        }).ToListAsync();
    }

    public async Task<EquipmentDetailViewModel?> GetEquipmentDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var eq = await _db.Equipments
            .Include(e => e.MaintenanceRecords.Where(m => !m.IsDeleted))
                .ThenInclude(m => m.TechnicianUser)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tid && !e.IsDeleted);
        if (eq == null) return null;

        var oeeTasks = await _db.PlanTasks
            .AsNoTracking()
            .Include(t => t.Plan)
            .Where(t => t.TenantId == tid
                && !t.IsDeleted
                && t.EquipmentId == id
                && t.Status == PlanTaskStatus.Done
                && t.OeePercent.HasValue
                && t.ActualEndTime.HasValue
                && t.ActualEndTime.Value >= DateTime.Today.AddDays(-89))
            .OrderByDescending(t => t.ActualEndTime)
            .ToListAsync();
        var costLedgers = await _db.EquipmentCostLedgers
            .AsNoTracking()
            .Where(l => l.TenantId == tid && l.EquipmentId == id && !l.IsDeleted)
            .OrderByDescending(l => l.OccurredDate)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync();
        var incidents = await _db.MaintenanceIncidents
            .AsNoTracking()
            .Where(i => i.TenantId == tid && i.EquipmentId == id && !i.IsDeleted)
            .ToListAsync();
        var statusHistories = await _db.EquipmentStatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.TenantId == tid && h.EquipmentId == id && !h.IsDeleted)
            .OrderByDescending(h => h.ChangedAt)
            .Take(20)
            .ToListAsync();

        return new EquipmentDetailViewModel
        {
            Id = eq.Id, Code = eq.Code, Name = eq.Name,
            Type = eq.Type, Status = eq.Status.ToString(), Location = eq.Location,
            Manufacturer = eq.Manufacturer, Model = eq.Model, SerialNumber = eq.SerialNumber,
            PurchaseDate = eq.PurchaseDate, PurchasePrice = eq.PurchasePrice,
            LifespanYears = eq.LifespanYears, NextMaintenanceDate = eq.NextMaintenanceDate,
            Notes = eq.Notes,
            Oee7Days = BuildOeeSummary(oeeTasks, 7),
            Oee30Days = BuildOeeSummary(oeeTasks, 30),
            Oee90Days = BuildOeeSummary(oeeTasks, 90),
            OeeTrend = BuildOeeTrend(oeeTasks, 30),
            CostPerformance = BuildCostPerformance(eq, costLedgers, incidents),
            CostLedgers = costLedgers.Take(20).Select(l => new EquipmentCostLedgerItemViewModel
            {
                Id = l.Id,
                CostType = l.CostType.ToString(),
                Amount = l.Amount,
                OccurredDate = l.OccurredDate,
                SourceType = l.SourceType,
                SourceId = l.SourceId,
                Notes = l.Notes
            }).ToList(),
            StatusHistories = statusHistories.Select(h => new EquipmentStatusHistoryItemViewModel
            {
                Id = h.Id,
                OldStatus = h.OldStatus?.ToString(),
                NewStatus = h.NewStatus.ToString(),
                ChangedAt = h.ChangedAt,
                ChangedByName = h.ChangedByUser?.FullName,
                Reason = h.Reason
            }).ToList(),
            RecentOeeTasks = oeeTasks.Take(8).Select(t => new EquipmentOeeTaskItemViewModel
            {
                Id = t.Id,
                TaskName = t.Name,
                PlanCode = t.Plan?.Code ?? "",
                ActualEndTime = t.ActualEndTime,
                PlannedDurationMinutes = t.PlannedDurationMinutes,
                ActualDurationMinutes = t.ActualDurationMinutes,
                UnitsProduced = t.UnitsProduced,
                UnitsGood = t.UnitsGood,
                OeePercent = t.OeePercent
            }).ToList(),
            MaintenanceRecords = eq.MaintenanceRecords.OrderByDescending(m => m.ScheduledDate).Select(m => new MaintenanceRecordItem
            {
                Id = m.Id,
                MaintenanceType = m.MaintenanceType.ToString(),
                ScheduledDate = m.ScheduledDate,
                CompletedDate = m.CompletedDate,
                TechnicianName = m.TechnicianUser?.FullName,
                Status = m.Status.ToString(),
                Description = m.Description,
                WorkDone = m.WorkDone,
                Cost = m.Cost,
                NextMaintenanceDate = m.NextMaintenanceDate
            }).ToList()
        };
    }

    private static EquipmentOeeSummaryViewModel BuildOeeSummary(List<PlanTask> tasks, int days)
    {
        var cutoff = DateTime.Today.AddDays(-(days - 1));
        var scoped = tasks
            .Where(t => t.ActualEndTime.HasValue && t.ActualEndTime.Value >= cutoff)
            .ToList();

        return new EquipmentOeeSummaryViewModel
        {
            Days = days,
            TaskCount = scoped.Count,
            OeePercent = AveragePercent(scoped.Select(t => t.OeePercent)),
            AvailabilityPercent = AveragePercent(scoped.Select(t => t.OeeAvailabilityPercent)),
            PerformancePercent = AveragePercent(scoped.Select(t => t.OeePerformancePercent)),
            QualityPercent = AveragePercent(scoped.Select(t => t.OeeQualityPercent)),
            UnitsProduced = scoped.Sum(t => t.UnitsProduced ?? 0),
            UnitsGood = scoped.Sum(t => t.UnitsGood ?? 0)
        };
    }

    private static List<EquipmentOeeTrendPointViewModel> BuildOeeTrend(List<PlanTask> tasks, int days)
    {
        var result = new List<EquipmentOeeTrendPointViewModel>();
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-(days - 1)));

        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            var scoped = tasks
                .Where(t => t.ActualEndTime.HasValue
                    && t.ActualEndTime.Value >= dayStart
                    && t.ActualEndTime.Value < dayEnd)
                .ToList();

            result.Add(new EquipmentOeeTrendPointViewModel
            {
                Date = date,
                TaskCount = scoped.Count,
                OeePercent = AveragePercent(scoped.Select(t => t.OeePercent))
            });
        }

        return result;
    }

    private static decimal? AveragePercent(IEnumerable<decimal?> values)
    {
        var materialized = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return materialized.Any() ? Math.Round(materialized.Average(), 2) : null;
    }

    private static EquipmentCostPerformanceViewModel BuildCostPerformance(
        Equipment equipment,
        List<EquipmentCostLedger> ledgers,
        List<MaintenanceIncident> incidents)
    {
        var purchaseCost = ledgers.Where(l => l.CostType == EquipmentCostType.Purchase).Sum(l => l.Amount);
        if (purchaseCost <= 0 && equipment.PurchasePrice.HasValue) purchaseCost = equipment.PurchasePrice.Value;

        var downtimeHours = incidents.Sum(i => i.DowntimeHours ?? 0);
        var failureCount = incidents.Count(i => i.DowntimeHours.HasValue && i.DowntimeHours.Value > 0);
        var operatingStart = equipment.PurchaseDate?.ToDateTime(TimeOnly.MinValue) ?? equipment.CreatedAt.DateTime;
        var totalOperatingHours = Math.Max(1m, (decimal)(DateTime.UtcNow - operatingStart).TotalHours - downtimeHours);
        var totalCost = purchaseCost
            + ledgers.Where(l => l.CostType != EquipmentCostType.Purchase).Sum(l => l.Amount);
        var costToPurchasePercent = purchaseCost > 0 ? Math.Round(totalCost / purchaseCost * 100m, 2) : (decimal?)null;

        return new EquipmentCostPerformanceViewModel
        {
            PurchaseCost = purchaseCost,
            MaintenanceCost = ledgers.Where(l => l.CostType == EquipmentCostType.Maintenance).Sum(l => l.Amount),
            RepairCost = ledgers.Where(l => l.CostType == EquipmentCostType.Repair).Sum(l => l.Amount),
            SparePartCost = ledgers.Where(l => l.CostType == EquipmentCostType.SparePart).Sum(l => l.Amount),
            OtherCost = ledgers.Where(l => l.CostType == EquipmentCostType.Other).Sum(l => l.Amount),
            DowntimeHours = downtimeHours,
            FailureCount = failureCount,
            MtbfHours = failureCount > 0 ? Math.Round(totalOperatingHours / failureCount, 2) : null,
            MttrHours = failureCount > 0 ? Math.Round(downtimeHours / failureCount, 2) : null,
            CostToPurchasePercent = costToPurchasePercent,
            ShouldRecommendReplace = costToPurchasePercent.HasValue && costToPurchasePercent.Value > 80m
        };
    }

    private void AddEquipmentStatusHistory(Equipment equipment, EquipmentStatus? oldStatus, EquipmentStatus newStatus, string reason)
    {
        if (oldStatus == newStatus) return;
        _db.EquipmentStatusHistories.Add(new EquipmentStatusHistory
        {
            TenantId = equipment.TenantId,
            EquipmentId = equipment.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        });
    }

    private void AddEquipmentCostLedger(
        Equipment equipment,
        EquipmentCostType costType,
        decimal? amount,
        DateOnly occurredDate,
        string sourceType,
        Guid? sourceId,
        string? notes)
    {
        if (!amount.HasValue || amount.Value <= 0) return;
        _db.EquipmentCostLedgers.Add(new EquipmentCostLedger
        {
            TenantId = equipment.TenantId,
            EquipmentId = equipment.Id,
            CostType = costType,
            Amount = amount.Value,
            OccurredDate = occurredDate,
            SourceType = sourceType,
            SourceId = sourceId,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        });
    }

    public async Task<Guid> CreateEquipmentAsync(EquipmentCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var code = await _numbering.NextAsync(NumberingSequenceKeys.Equipment, "EQ-", 4);
        var entity = new Equipment
        {
            TenantId = tid,
            Code = code,
            Name = vm.Name, Type = vm.Type, Location = vm.Location,
            Manufacturer = vm.Manufacturer, Model = vm.Model, SerialNumber = vm.SerialNumber,
            PurchaseDate = vm.PurchaseDate, PurchasePrice = vm.PurchasePrice,
            LifespanYears = vm.LifespanYears, NextMaintenanceDate = vm.NextMaintenanceDate,
            Notes = vm.Notes, Status = EquipmentStatus.Available,
            CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Equipments.Add(entity);
        AddEquipmentStatusHistory(entity, null, entity.Status, "Tạo thiết bị mới.");
        AddEquipmentCostLedger(
            entity,
            EquipmentCostType.Purchase,
            entity.PurchasePrice,
            entity.PurchaseDate ?? DateOnly.FromDateTime(DateTime.Today),
            "Equipment",
            entity.Id,
            "Chi phí mua mới thiết bị.");
        await _audit.LogAsync("Equipment", entity.Id, "Create",
            newValueObj: new { entity.Code, entity.Name, entity.Type, entity.Status, entity.Location });
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ScheduleMaintenanceAsync(ScheduleMaintenanceViewModel vm)
    {
        var eq = await _db.Equipments.FindAsync(vm.EquipmentId);
        if (eq == null || eq.TenantId != _tenant.TenantId) return false;
        var oldEquipmentStatus = eq.Status;
        var oldNextMaintenanceDate = eq.NextMaintenanceDate;

        var maintenanceType = Enum.TryParse<MaintenanceType>(vm.MaintenanceType, true, out var parsedType)
            ? parsedType
            : MaintenanceType.Preventive;

        var record = new MaintenanceRecord
        {
            TenantId = _tenant.TenantId,
            EquipmentId = vm.EquipmentId,
            MaintenanceType = maintenanceType,
            ScheduledDate = vm.ScheduledDate,
            Description = vm.Description,
            TechnicianUserId = vm.TechnicianUserId,
            Status = MaintenanceRecordStatus.Scheduled,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceRecords.Add(record);

        // Update equipment status and next maintenance date
        if (maintenanceType is MaintenanceType.Preventive or MaintenanceType.Emergency)
        {
            eq.Status = EquipmentStatus.Maintenance;
            AddEquipmentStatusHistory(eq, oldEquipmentStatus, eq.Status, $"Lên lịch bảo trì {maintenanceType}.");
        }
        eq.NextMaintenanceDate = vm.ScheduledDate;
        eq.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.LogAsync("Equipment", eq.Id, "ScheduleMaintenance",
            oldValueObj: new { Status = oldEquipmentStatus, NextMaintenanceDate = oldNextMaintenanceDate },
            newValueObj: new { eq.Status, eq.NextMaintenanceDate },
            extra: new { MaintenanceRecordId = record.Id, record.MaintenanceType, record.ScheduledDate });

        return await _db.SaveChangesWithConcurrencyAsync();
    }

    public async Task<bool> CompleteMaintenanceAsync(CompleteMaintenanceViewModel vm)
    {
        var record = await _db.MaintenanceRecords.Include(m => m.Equipment)
            .FirstOrDefaultAsync(m => m.Id == vm.RecordId && m.TenantId == _tenant.TenantId);
        if (record == null) return false;
        var oldRecordStatus = record.Status;
        var oldEquipmentStatus = record.Equipment?.Status;

        record.Status = MaintenanceRecordStatus.Completed;
        record.CompletedDate = vm.CompletedDate;
        record.WorkDone = vm.WorkDone;
        record.Cost = vm.Cost;
        record.NextMaintenanceDate = vm.NextMaintenanceDate;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        if (record.Equipment != null)
        {
            var oldStatus = record.Equipment.Status;
            record.Equipment.Status = EquipmentStatus.Available;
            record.Equipment.NextMaintenanceDate = vm.NextMaintenanceDate;
            record.Equipment.UpdatedAt = DateTimeOffset.UtcNow;
            AddEquipmentStatusHistory(record.Equipment, oldStatus, record.Equipment.Status, "Hoàn thành bảo trì.");
            AddEquipmentCostLedger(
                record.Equipment,
                record.MaintenanceType == MaintenanceType.Corrective || record.MaintenanceType == MaintenanceType.Emergency
                    ? EquipmentCostType.Repair
                    : EquipmentCostType.Maintenance,
                record.Cost,
                record.CompletedDate ?? DateOnly.FromDateTime(DateTime.Today),
                "MaintenanceRecord",
                record.Id,
                record.WorkDone ?? record.Description);
        }
        await _audit.LogAsync("Equipment", record.EquipmentId, "CompleteMaintenance",
            oldValueObj: new { MaintenanceStatus = oldRecordStatus, EquipmentStatus = oldEquipmentStatus },
            newValueObj: new { MaintenanceStatus = record.Status, EquipmentStatus = record.Equipment?.Status, record.NextMaintenanceDate },
            extra: new { MaintenanceRecordId = record.Id, record.Cost });
        return await _db.SaveChangesWithConcurrencyAsync();
    }

    // ─── WORK SHIFTS ────────────────────────────────────────────────────────

    public async Task<List<WorkShiftViewModel>> GetShiftsAsync()
    {
        var tid = _tenant.TenantId;
        var shifts = await _db.WorkShifts
            .Where(s => s.TenantId == tid && !s.IsDeleted)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var result = new List<WorkShiftViewModel>();
        foreach (var s in shifts)
        {
            var count = await _db.ShiftAssignments.CountAsync(a => a.ShiftId == s.Id && a.WorkDate == today && !a.IsDeleted);
            result.Add(new WorkShiftViewModel
            {
                Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime,
                WorkHours = s.WorkHours, ShiftType = s.ShiftType, IsActive = s.IsActive,
                Notes = s.Notes, TodayAssignmentCount = count
            });
        }
        return result;
    }

    public async Task<Guid> CreateShiftAsync(WorkShiftCreateViewModel vm)
    {
        var entity = new WorkShift
        {
            TenantId = _tenant.TenantId,
            Name = vm.Name,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            WorkHours = vm.WorkHours,
            ShiftType = vm.ShiftType,
            Notes = vm.Notes,
            IsActive = true,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.WorkShifts.Add(entity);
        await _audit.LogAsync("WorkShift", entity.Id, "Create",
            newValueObj: new { entity.Name, entity.StartTime, entity.EndTime, entity.ShiftType });
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<ShiftScheduleViewModel> GetShiftScheduleAsync(DateOnly? date = null)
    {
        var tid = _tenant.TenantId;
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var assignments = await _db.ShiftAssignments
            .Include(a => a.Shift)
            .Include(a => a.User)
            .Where(a => a.TenantId == tid && !a.IsDeleted && a.WorkDate == targetDate)
            .OrderBy(a => a.Shift!.StartTime)
            .Select(a => new ShiftAssignmentItem
            {
                Id = a.Id,
                ShiftName = a.Shift != null ? a.Shift.Name : "",
                ShiftStart = a.Shift != null ? a.Shift.StartTime : default,
                ShiftEnd = a.Shift != null ? a.Shift.EndTime : default,
                UserName = a.User != null ? a.User.FullName : "",
                UserId = a.UserId,
                Status = a.Status.ToString(),
                ActualCheckIn = a.ActualCheckIn,
                ActualCheckOut = a.ActualCheckOut
            }).ToListAsync();

        var shifts = await _db.WorkShifts.Where(s => s.TenantId == tid && !s.IsDeleted && s.IsActive)
            .Select(s => new SelectOption { Value = s.Id.ToString(), Text = $"{s.Name} ({s.StartTime:HH\\:mm} - {s.EndTime:HH\\:mm})" }).ToListAsync();
        var users = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync();

        return new ShiftScheduleViewModel
        {
            TargetDate = targetDate,
            Assignments = assignments,
            Shifts = shifts,
            Users = users
        };
    }

    public async Task<bool> AssignShiftAsync(AssignShiftViewModel vm)
    {
        var tid = _tenant.TenantId;
        var existing = await _db.ShiftAssignments.FirstOrDefaultAsync(a =>
            a.TenantId == tid && a.UserId == vm.UserId && a.WorkDate == vm.WorkDate && !a.IsDeleted);
        if (existing != null)
        {
            var oldShiftId = existing.ShiftId;
            existing.ShiftId = vm.ShiftId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _audit.LogAsync("ShiftAssignment", existing.Id, "Update",
                oldValueObj: new { ShiftId = oldShiftId },
                newValueObj: new { existing.ShiftId, existing.UserId, existing.WorkDate, existing.Status });
        }
        else
        {
            var assignment = new ShiftAssignment
            {
                TenantId = tid,
                ShiftId = vm.ShiftId,
                UserId = vm.UserId,
                WorkDate = vm.WorkDate,
                Status = ShiftAssignmentStatus.Scheduled,
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.ShiftAssignments.Add(assignment);
            await _audit.LogAsync("ShiftAssignment", assignment.Id, "Create",
                newValueObj: new { assignment.ShiftId, assignment.UserId, assignment.WorkDate, assignment.Status });
        }
        return await _db.SaveChangesWithConcurrencyAsync();
    }

    // ─── CERTIFICATES ────────────────────────────────────────────────────────

    public async Task<List<EmployeeCertificateItem>> GetCertificatesAsync(string? search, string? category, bool? expiredOnly)
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var q = _db.EmployeeCertificates.Include(c => c.User)
            .Where(c => c.TenantId == tid && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.CertificateName.Contains(search) || (c.User != null && c.User.FullName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(c => c.Category == category);
        if (expiredOnly == true)
            q = q.Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < today);

        return await q.OrderBy(c => c.ExpiryDate).Select(c => new EmployeeCertificateItem
        {
            Id = c.Id,
            UserName = c.User != null ? c.User.FullName : "",
            UserId = c.UserId,
            CertificateName = c.CertificateName,
            IssuingOrganization = c.IssuingOrganization,
            IssuedDate = c.IssuedDate,
            ExpiryDate = c.ExpiryDate,
            Category = c.Category,
            CertificateNumber = c.CertificateNumber,
            IsExpired = c.ExpiryDate.HasValue && c.ExpiryDate.Value < today,
            IsExpiringSoon = c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(30)
        }).ToListAsync();
    }

    public async Task<CertificateCreateFormViewModel> GetCertificateCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        return new CertificateCreateFormViewModel
        {
            Users = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
                .ToListAsync()
        };
    }

    public async Task<bool> AddCertificateAsync(CertificateCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var certificate = new EmployeeCertificate
        {
            TenantId = tid,
            UserId = vm.UserId,
            CertificateName = vm.CertificateName,
            IssuingOrganization = vm.IssuingOrganization,
            IssuedDate = vm.IssuedDate,
            ExpiryDate = vm.ExpiryDate,
            Category = vm.Category,
            CertificateNumber = vm.CertificateNumber,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.EmployeeCertificates.Add(certificate);
        await _audit.LogAsync("EmployeeCertificate", certificate.Id, "Create",
            newValueObj: new { vm.UserId, vm.CertificateName, vm.IssuedDate, vm.ExpiryDate, vm.Category });
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── WORKSPACES ──────────────────────────────────────────────────────────

    public async Task<List<WorkspaceItem>> GetWorkspacesAsync(string? search, string? type)
    {
        var tid = _tenant.TenantId;
        var q = _db.Workspaces.Where(w => w.TenantId == tid && !w.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(w => w.Name.Contains(search) || w.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(w => w.Type == type);

        return await q.OrderBy(w => w.Code).Select(w => new WorkspaceItem
        {
            Id = w.Id, Code = w.Code, Name = w.Name,
            Type = w.Type, Location = w.Location,
            AreaSqm = w.AreaSqm, Capacity = w.Capacity,
            Status = w.Status.ToString(), Notes = w.Notes
        }).ToListAsync();
    }

    public async Task<Guid> CreateWorkspaceAsync(WorkspaceCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var code = await _numbering.NextAsync(NumberingSequenceKeys.Workspace, "WS-", 3);
        var entity = new Workspace
        {
            TenantId = tid,
            Code = code,
            Name = vm.Name, Type = vm.Type,
            Location = vm.Location, AreaSqm = vm.AreaSqm,
            Capacity = vm.Capacity, Status = WorkspaceStatus.Active,
            ParentId = vm.ParentId, Notes = vm.Notes,
            CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Workspaces.Add(entity);
        await _audit.LogAsync("Workspace", entity.Id, "Create",
            newValueObj: new { entity.Code, entity.Name, entity.Type, entity.Status, entity.ParentId });
        await _db.SaveChangesAsync();
        return entity.Id;
    }
}
