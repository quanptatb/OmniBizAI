using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Domain.StateMachines;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class MaintenanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly GeminiService _gemini;
    private readonly INumberingService _numbering;
    private readonly IAuditService _audit;

    public MaintenanceService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini, INumberingService numbering, IAuditService audit)
    {
        _db = db; _tenant = tenant; _gemini = gemini; _numbering = numbering; _audit = audit;
    }

    private static PmFrequency ParsePmFrequency(string? frequency)
    {
        if (frequency?.Equals("Every_X_Hours", StringComparison.OrdinalIgnoreCase) == true)
            return PmFrequency.ByRunHours;

        return Enum.TryParse<PmFrequency>(frequency, true, out var parsedFrequency)
            ? parsedFrequency
            : PmFrequency.Monthly;
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

    // ─── DASHBOARD ──────────────────────────────────────────────────────────

    public async Task<MaintenanceDashboardViewModel> GetDashboardAsync()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var openIncidents = await _db.MaintenanceIncidents
            .CountAsync(i => i.TenantId == tid && !i.IsDeleted && i.Status != IncidentStatus.Closed && i.Status != IncidentStatus.Resolved);
        var criticalIncidents = await _db.MaintenanceIncidents
            .CountAsync(i => i.TenantId == tid && !i.IsDeleted && i.Status == IncidentStatus.Open && i.Severity == IncidentSeverity.Critical);
        var overduePm = await _db.PmSchedules
            .CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.IsActive
                && p.NextDueDate.HasValue && p.NextDueDate.Value < today);
        var dueSoonPm = await _db.PmSchedules
            .CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.IsActive
                && p.NextDueDate.HasValue && p.NextDueDate.Value >= today && p.NextDueDate.Value <= today.AddDays(7));
        var lowStockParts = await _db.SpareParts
            .CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.StockQuantity <= p.MinimumStock);

        var recentIncidents = await _db.MaintenanceIncidents
            .Include(i => i.Equipment).Include(i => i.AssignedTechnician)
            .Where(i => i.TenantId == tid && !i.IsDeleted)
            .OrderByDescending(i => i.OccurredAt ?? i.CreatedAt)
            .Take(5)
            .Select(i => new IncidentSummaryItem
            {
                Id = i.Id, Title = i.Title,
                EquipmentName = i.Equipment != null ? i.Equipment.Name : "",
                Severity = i.Severity.ToString(), Status = i.Status.ToString(),
                OccurredAt = i.OccurredAt ?? i.CreatedAt,
                TechnicianName = i.AssignedTechnician != null ? i.AssignedTechnician.FullName : null
            }).ToListAsync();

        var upcomingPm = await _db.PmSchedules
            .Include(p => p.Equipment)
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.IsActive
                && p.NextDueDate.HasValue && p.NextDueDate.Value <= today.AddDays(14))
            .OrderBy(p => p.NextDueDate)
            .Take(5)
            .Select(p => new PmScheduleSummaryItem
            {
                Id = p.Id, TaskName = p.TaskName,
                EquipmentName = p.Equipment != null ? p.Equipment.Name : "",
                NextDueDate = p.NextDueDate,
                Frequency = p.Frequency.ToString(),
                IsOverdue = p.NextDueDate.HasValue && p.NextDueDate.Value < today
            }).ToListAsync();

        // IoT status summary
        var sensorWarnings = await _db.EquipmentSensorReadings
            .Where(s => s.TenantId == tid && !s.IsDeleted && s.Status != SensorReadingStatus.Normal
                && s.ReadingTime >= DateTimeOffset.UtcNow.AddHours(-1))
            .CountAsync();

        return new MaintenanceDashboardViewModel
        {
            OpenIncidentCount = openIncidents,
            CriticalIncidentCount = criticalIncidents,
            OverduePmCount = overduePm,
            DueSoonPmCount = dueSoonPm,
            LowStockPartCount = lowStockParts,
            SensorWarningCount = sensorWarnings,
            RecentIncidents = recentIncidents,
            UpcomingPmTasks = upcomingPm
        };
    }

    // ─── INCIDENTS (CM - Corrective Maintenance) ─────────────────────────────

    public async Task<(List<IncidentSummaryItem> Items, int Total, int Open, int InProgress, int Resolved)>
        GetIncidentsAsync(string? search, string? severity, string? status)
    {
        var tid = _tenant.TenantId;
        var q = _db.MaintenanceIncidents.Include(i => i.Equipment).Include(i => i.AssignedTechnician)
            .Where(i => i.TenantId == tid && !i.IsDeleted);

        var total = await q.CountAsync();
        var open = await q.CountAsync(i => i.Status == IncidentStatus.Open);
        var inProg = await q.CountAsync(i => i.Status == IncidentStatus.InProgress);
        var resolved = await q.CountAsync(i => i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.Closed);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(i => i.Title.Contains(search) || (i.Equipment != null && i.Equipment.Name.Contains(search)));
        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<IncidentSeverity>(severity, true, out var severityFilter))
            q = q.Where(i => i.Severity == severityFilter);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<IncidentStatus>(status, true, out var statusFilter))
            q = q.Where(i => i.Status == statusFilter);

        var items = await q.OrderByDescending(i => i.OccurredAt ?? i.CreatedAt)
            .Select(i => new IncidentSummaryItem
            {
                Id = i.Id, Title = i.Title,
                EquipmentName = i.Equipment != null ? i.Equipment.Name : "",
                EquipmentId = i.EquipmentId,
                Severity = i.Severity.ToString(), Status = i.Status.ToString(),
                OccurredAt = i.OccurredAt ?? i.CreatedAt,
                TechnicianName = i.AssignedTechnician != null ? i.AssignedTechnician.FullName : null,
                DowntimeHours = i.DowntimeHours
            }).ToListAsync();

        return (items, total, open, inProg, resolved);
    }

    public async Task<MaintenanceIncidentDetailViewModel?> GetIncidentDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var inc = await _db.MaintenanceIncidents
            .Include(i => i.Equipment)
            .Include(i => i.ReportedByUser)
            .Include(i => i.AssignedTechnician)
            .Include(i => i.MaintenanceRecord)
            .Include(i => i.FailureMode)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tid && !i.IsDeleted);
        if (inc == null) return null;

        List<string> whys = new();
        if (!string.IsNullOrWhiteSpace(inc.FiveWhysJson))
        {
            try
            {
                whys = System.Text.Json.JsonSerializer.Deserialize<List<string>>(inc.FiveWhysJson) ?? new();
            }
            catch { }
        }

        var failureModeOptions = await _db.FailureModes
            .Where(f => f.TenantId == tid && !f.IsDeleted && f.IsActive)
            .OrderBy(f => f.Code)
            .Select(f => new SelectOption { Value = f.Id.ToString(), Text = $"{f.Code} — {f.Name}" })
            .ToListAsync();

        return new MaintenanceIncidentDetailViewModel
        {
            Id = inc.Id, Title = inc.Title, Description = inc.Description,
            Severity = inc.Severity.ToString(), Status = inc.Status.ToString(),
            EquipmentId = inc.EquipmentId,
            EquipmentName = inc.Equipment?.Name ?? "",
            ReportedByName = inc.ReportedByUser?.FullName,
            TechnicianName = inc.AssignedTechnician?.FullName,
            OccurredAt = inc.OccurredAt,
            ResolvedAt = inc.ResolvedAt,
            RootCause = inc.RootCause,
            Resolution = inc.Resolution,
            DowntimeHours = inc.DowntimeHours,
            MaintenanceRecordId = inc.MaintenanceRecordId,
            IsAnomalyDetected = inc.IsAnomalyDetected,
            FailureModeId = inc.FailureModeId,
            FailureModeName = inc.FailureMode?.Name,
            FiveWhys = whys,
            FailureModeOptions = failureModeOptions,
            NextStatuses = MaintenanceIncidentStateMachine.NextStates(inc.Status).Select(s => s.ToString()).ToList()
        };
    }

    public async Task<IncidentCreateFormViewModel> GetIncidentCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        return new IncidentCreateFormViewModel
        {
            Equipments = await _db.Equipments.Where(e => e.TenantId == tid && !e.IsDeleted)
                .OrderBy(e => e.Name).Select(e => new SelectOption { Value = e.Id.ToString(), Text = $"{e.Code} — {e.Name}" }).ToListAsync(),
            Technicians = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName).Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
        };
    }

    public async Task<Guid> CreateIncidentAsync(IncidentCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var severity = Enum.TryParse<IncidentSeverity>(vm.Severity, true, out var parsedSeverity)
            ? parsedSeverity
            : IncidentSeverity.Medium;

        var entity = new MaintenanceIncident
        {
            TenantId = tid,
            EquipmentId = vm.EquipmentId,
            Title = vm.Title,
            Description = vm.Description,
            Severity = severity,
            Status = IncidentStatus.Open,
            OccurredAt = vm.OccurredAt ?? DateTimeOffset.UtcNow,
            ReportedByUserId = _tenant.UserId,
            AssignedTechnicianId = vm.AssignedTechnicianId,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceIncidents.Add(entity);

        // Mark equipment as having issue
        var eq = await _db.Equipments.FindAsync(vm.EquipmentId);
        if (eq != null && severity is IncidentSeverity.High or IncidentSeverity.Critical)
        {
            var oldEquipmentStatus = eq.Status;
            eq.Status = EquipmentStatus.Maintenance;
            eq.UpdatedAt = DateTimeOffset.UtcNow;
            AddEquipmentStatusHistory(eq, oldEquipmentStatus, eq.Status, $"Tạo sự cố {severity}: {vm.Title}");
        }

        await _audit.LogAsync("MaintenanceIncident", entity.Id, "Create",
            newValueObj: new { entity.EquipmentId, entity.Title, entity.Severity, entity.Status, entity.AssignedTechnicianId });

        if (!await _db.SaveChangesWithConcurrencyAsync()) return Guid.Empty;
        return entity.Id;
    }

    public async Task<bool> ResolveIncidentAsync(ResolveIncidentViewModel vm)
    {
        var tid = _tenant.TenantId;
        var inc = await _db.MaintenanceIncidents.FindAsync(vm.IncidentId);
        if (inc == null || inc.TenantId != tid) return false;
        if (!MaintenanceIncidentStateMachine.CanTransition(inc.Status, IncidentStatus.Resolved)) return false;

        // F5.6: bắt buộc FailureMode khi Resolve
        if (!vm.FailureModeId.HasValue) return false;
        var fmExists = await _db.FailureModes.AnyAsync(f => f.Id == vm.FailureModeId.Value && f.TenantId == tid && !f.IsDeleted);
        if (!fmExists) return false;

        var oldStatus = inc.Status;
        inc.Status = IncidentStatus.Resolved;
        inc.RootCause = vm.RootCause;
        inc.Resolution = vm.Resolution;
        inc.DowntimeHours = vm.DowntimeHours;
        inc.ResolvedAt = DateTimeOffset.UtcNow;
        inc.FailureModeId = vm.FailureModeId;

        var whys = new[] { vm.Why1, vm.Why2, vm.Why3, vm.Why4, vm.Why5 }
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w!.Trim())
            .ToList();
        if (whys.Count > 0)
            inc.FiveWhysJson = System.Text.Json.JsonSerializer.Serialize(whys);

        inc.UpdatedAt = DateTimeOffset.UtcNow;

        // Create a CM record for history
        var record = new MaintenanceRecord
        {
            TenantId = tid,
            EquipmentId = inc.EquipmentId,
            MaintenanceType = MaintenanceType.Corrective,
            ScheduledDate = DateOnly.FromDateTime(inc.OccurredAt?.DateTime ?? DateTime.UtcNow),
            CompletedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = MaintenanceRecordStatus.Completed,
            Description = inc.Title,
            WorkDone = vm.Resolution,
            TechnicianUserId = inc.AssignedTechnicianId,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceRecords.Add(record);
        inc.MaintenanceRecordId = record.Id;

        // Restore equipment status — chỉ khi không còn incident High/Critical Open khác
        var eq = await _db.Equipments.FindAsync(inc.EquipmentId);
        if (eq != null && eq.Status == EquipmentStatus.Maintenance)
        {
            var hasOtherSevereOpen = await _db.MaintenanceIncidents.AnyAsync(i =>
                i.TenantId == tid && !i.IsDeleted
                && i.EquipmentId == inc.EquipmentId
                && i.Id != inc.Id
                && (i.Severity == IncidentSeverity.High || i.Severity == IncidentSeverity.Critical)
                && i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed);
            if (!hasOtherSevereOpen)
            {
                var oldEquipmentStatus = eq.Status;
                eq.Status = EquipmentStatus.Available;
                eq.UpdatedAt = DateTimeOffset.UtcNow;
                AddEquipmentStatusHistory(eq, oldEquipmentStatus, eq.Status, $"Resolve incident {inc.Title}.");
            }
        }

        await _audit.LogAsync("MaintenanceIncident", inc.Id, "Resolve",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { inc.Status, inc.RootCause, inc.Resolution, inc.DowntimeHours, inc.ResolvedAt, inc.MaintenanceRecordId },
            extra: new { MaintenanceRecordId = record.Id });

        return await _db.SaveChangesWithConcurrencyAsync();
    }

    // ─── PM SCHEDULES (Preventive Maintenance) ───────────────────────────────

    public async Task<List<PmScheduleSummaryItem>> GetPmSchedulesAsync(Guid? equipmentId, bool? overdueOnly)
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var q = _db.PmSchedules.Include(p => p.Equipment).Include(p => p.AssignedTechnician)
            .Where(p => p.TenantId == tid && !p.IsDeleted);
        if (equipmentId.HasValue) q = q.Where(p => p.EquipmentId == equipmentId.Value);
        if (overdueOnly == true) q = q.Where(p => p.NextDueDate.HasValue && p.NextDueDate.Value < today);

        return await q.OrderBy(p => p.NextDueDate).Select(p => new PmScheduleSummaryItem
        {
            Id = p.Id, TaskName = p.TaskName,
            EquipmentName = p.Equipment != null ? p.Equipment.Name : "",
            EquipmentId = p.EquipmentId,
            Frequency = p.Frequency.ToString(), FrequencyValue = p.FrequencyValue,
            NextDueDate = p.NextDueDate, LastPerformedDate = p.LastPerformedDate,
            IsActive = p.IsActive,
            TechnicianName = p.AssignedTechnician != null ? p.AssignedTechnician.FullName : null,
            EstimatedDurationMinutes = p.EstimatedDurationMinutes,
            IsOverdue = p.NextDueDate.HasValue && p.NextDueDate.Value < today
        }).ToListAsync();
    }

    public async Task<PmScheduleCreateFormViewModel> GetPmCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        return new PmScheduleCreateFormViewModel
        {
            Equipments = await _db.Equipments.Where(e => e.TenantId == tid && !e.IsDeleted)
                .OrderBy(e => e.Name).Select(e => new SelectOption { Value = e.Id.ToString(), Text = $"{e.Code} — {e.Name}" }).ToListAsync(),
            Technicians = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName).Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
        };
    }

    public async Task<Guid> CreatePmScheduleAsync(PmScheduleCreateViewModel vm)
    {
        var frequency = ParsePmFrequency(vm.Frequency);

        var entity = new PmSchedule
        {
            TenantId = _tenant.TenantId,
            EquipmentId = vm.EquipmentId,
            TaskName = vm.TaskName,
            Frequency = frequency,
            FrequencyValue = vm.FrequencyValue,
            TriggerType = vm.TriggerType,
            IntervalHours = vm.IntervalHours,
            IntervalCycles = vm.IntervalCycles,
            ConditionSensorType = vm.ConditionSensorType,
            ConditionThreshold = vm.ConditionThreshold,
            Instructions = vm.Instructions,
            EstimatedDurationMinutes = vm.EstimatedDurationMinutes,
            NextDueDate = vm.FirstDueDate,
            AssignedTechnicianId = vm.AssignedTechnicianId,
            IsActive = true,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.PmSchedules.Add(entity);
        await _audit.LogAsync("PmSchedule", entity.Id, "Create",
            newValueObj: new { entity.EquipmentId, entity.TaskName, entity.Frequency, entity.FrequencyValue, entity.NextDueDate, entity.AssignedTechnicianId });
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ExecutePmTaskAsync(ExecutePmViewModel vm)
    {
        var pm = await _db.PmSchedules.FindAsync(vm.PmScheduleId);
        if (pm == null || pm.TenantId != _tenant.TenantId) return false;
        var oldNextDueDate = pm.NextDueDate;

        // Create maintenance record
        var record = new MaintenanceRecord
        {
            TenantId = _tenant.TenantId,
            EquipmentId = pm.EquipmentId,
            MaintenanceType = MaintenanceType.Preventive,
            ScheduledDate = pm.NextDueDate ?? DateOnly.FromDateTime(DateTime.Today),
            CompletedDate = vm.CompletedDate,
            Status = MaintenanceRecordStatus.Completed,
            Description = pm.TaskName,
            WorkDone = vm.WorkDone,
            Cost = vm.Cost,
            TechnicianUserId = vm.TechnicianUserId ?? pm.AssignedTechnicianId,
            NextMaintenanceDate = vm.NextDueDate,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceRecords.Add(record);

        // Update PM schedule
        pm.LastPerformedDate = vm.CompletedDate;
        pm.NextDueDate = vm.NextDueDate;
        pm.UpdatedAt = DateTimeOffset.UtcNow;

        // Update equipment next maintenance date
        var eq = await _db.Equipments.FindAsync(pm.EquipmentId);
        if (eq != null)
        {
            eq.NextMaintenanceDate = vm.NextDueDate;
            eq.UpdatedAt = DateTimeOffset.UtcNow;
            AddEquipmentCostLedger(
                eq,
                EquipmentCostType.Maintenance,
                record.Cost,
                record.CompletedDate ?? DateOnly.FromDateTime(DateTime.Today),
                "MaintenanceRecord",
                record.Id,
                record.WorkDone ?? record.Description);

            // Snapshot RunHours/Cycles cho condition-based PM (F5.4)
            if (pm.TriggerType == PmTriggerType.RunHoursBased)
                pm.LastRunHoursAtPm = eq.RunHours;
            if (pm.TriggerType == PmTriggerType.CyclesBased)
                pm.LastCyclesAtPm = eq.CycleCount;
        }

        await _audit.LogAsync("PmSchedule", pm.Id, "Execute",
            oldValueObj: new { NextDueDate = oldNextDueDate },
            newValueObj: new { LastPerformedDate = pm.LastPerformedDate, pm.NextDueDate },
            extra: new { MaintenanceRecordId = record.Id, record.EquipmentId, record.Cost });

        return await _db.SaveChangesWithConcurrencyAsync();
    }

    // ─── SPARE PARTS ─────────────────────────────────────────────────────────

    public async Task<List<SparePartItem>> GetSparePartsAsync(string? search, string? category)
    {
        var tid = _tenant.TenantId;
        var q = _db.SpareParts.Where(p => p.TenantId == tid && !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category == category);

        return await q.OrderBy(p => p.Code).Select(p => new SparePartItem
        {
            Id = p.Id, Code = p.Code, Name = p.Name,
            Manufacturer = p.Manufacturer, PartNumber = p.PartNumber,
            Category = p.Category, StockQuantity = p.StockQuantity,
            MinimumStock = p.MinimumStock, UnitPrice = p.UnitPrice,
            Unit = p.Unit, Notes = p.Notes,
            IsLowStock = p.StockQuantity <= p.MinimumStock
        }).ToListAsync();
    }

    public async Task<Guid> CreateSparePartAsync(SparePartCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var code = await _numbering.NextAsync(NumberingSequenceKeys.SparePart, "SP-", 4);
        var entity = new SparePart
        {
            TenantId = tid,
            Code = code,
            Name = vm.Name, Manufacturer = vm.Manufacturer, PartNumber = vm.PartNumber,
            Category = vm.Category, StockQuantity = vm.InitialStock,
            MinimumStock = vm.MinimumStock, UnitPrice = vm.UnitPrice, Unit = vm.Unit,
            Notes = vm.Notes,
            CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        _db.SpareParts.Add(entity);
        await _audit.LogAsync("SparePart", entity.Id, "Create",
            newValueObj: new { entity.Code, entity.Name, entity.Category, entity.StockQuantity, entity.MinimumStock });
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> AdjustStockAsync(Guid partId, int delta, string reason)
    {
        var part = await _db.SpareParts.FindAsync(partId);
        if (part == null || part.TenantId != _tenant.TenantId) return false;
        var oldStockQuantity = part.StockQuantity;
        part.StockQuantity = Math.Max(0, part.StockQuantity + delta);
        part.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("SparePart", part.Id, "AdjustStock",
            oldValueObj: new { StockQuantity = oldStockQuantity },
            newValueObj: new { part.StockQuantity },
            extra: new { Delta = delta, Reason = reason });
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── IoT / SENSOR ────────────────────────────────────────────────────────

    public async Task<List<SensorReadingViewModel>> GetLatestSensorReadingsAsync(Guid equipmentId)
    {
        var tid = _tenant.TenantId;
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

        // Latest reading per sensor type
        var readings = await _db.EquipmentSensorReadings
            .Where(r => r.TenantId == tid && !r.IsDeleted && r.EquipmentId == equipmentId && r.ReadingTime >= cutoff)
            .GroupBy(r => r.SensorType)
            .Select(g => g.OrderByDescending(r => r.ReadingTime).First())
            .ToListAsync();

        return readings.Select(r => new SensorReadingViewModel
        {
            SensorType = r.SensorType, Value = r.Value, Unit = r.Unit,
            ReadingTime = r.ReadingTime, Status = r.Status.ToString(),
            ThresholdWarning = r.ThresholdWarning, ThresholdCritical = r.ThresholdCritical
        }).ToList();
    }

    /// <summary>Giả lập dữ liệu IoT (demo) - tạo readings ngẫu nhiên cho thiết bị</summary>
    public async Task SimulateSensorDataAsync(Guid equipmentId)
    {
        var tid = _tenant.TenantId;
        var rng = new Random();
        var sensors = new[]
        {
            new { Type = "Temperature", Min = 35.0, Max = 85.0, Unit = "°C", WarnAt = 70.0, CritAt = 80.0 },
            new { Type = "Vibration",   Min = 0.5,  Max = 8.0,  Unit = "mm/s", WarnAt = 5.0, CritAt = 7.0 },
            new { Type = "Pressure",    Min = 2.5,  Max = 8.0,  Unit = "bar",  WarnAt = 7.0, CritAt = 7.8 },
            new { Type = "RPM",         Min = 1400.0, Max = 1600.0, Unit = "rpm", WarnAt = 1560.0, CritAt = 1590.0 },
            new { Type = "Current",     Min = 8.0,  Max = 20.0, Unit = "A",    WarnAt = 17.0, CritAt = 19.0 }
        };

        foreach (var s in sensors)
        {
            var val = Math.Round(s.Min + rng.NextDouble() * (s.Max - s.Min), 2);
            var status = val >= s.CritAt
                ? SensorReadingStatus.Critical
                : val >= s.WarnAt ? SensorReadingStatus.Warning : SensorReadingStatus.Normal;
            _db.EquipmentSensorReadings.Add(new EquipmentSensorReading
            {
                TenantId = tid, EquipmentId = equipmentId,
                SensorType = s.Type, Value = val, Unit = s.Unit,
                Status = status, ReadingTime = DateTimeOffset.UtcNow,
                ThresholdWarning = s.WarnAt, ThresholdCritical = s.CritAt,
                CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<string> AnalyzeIncidentWithAiAsync(Guid incidentId)
    {
        var inc = await GetIncidentDetailAsync(incidentId);
        if (inc == null) return "Không tìm thấy sự cố.";

        var prompt = $"Phân tích sự cố bảo trì:\n" +
                     $"- Tên sự cố: {inc.Title}\n" +
                     $"- Thiết bị: {inc.EquipmentName}\n" +
                     $"- Mức độ: {inc.Severity}\n" +
                     $"- Mô tả: {inc.Description}\n" +
                     $"- Thời gian ngừng máy: {inc.DowntimeHours ?? 0} giờ\n\n" +
                     $"Hãy đề xuất: (1) Nguyên nhân gốc rễ có thể có, (2) Biện pháp khắc phục, (3) Biện pháp phòng ngừa tái phát.";

        var response = await _gemini.GenerateAsync("Bạn là chuyên gia bảo trì máy móc công nghiệp.", prompt);
        return response.Success ? response.Text : response.ErrorMessage ?? "Lỗi AI.";
    }
}
