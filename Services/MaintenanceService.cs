using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;
using System.Text.Json;

namespace OmniBizAI.Services;

public class MaintenanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly GeminiService _gemini;
    private readonly NotificationService _notif;
    private readonly NumberSequenceService _seq;

    public MaintenanceService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini, NotificationService notif, NumberSequenceService seq)
    {
        _db = db; _tenant = tenant; _gemini = gemini; _notif = notif; _seq = seq;
    }

    // ─── DASHBOARD ──────────────────────────────────────────────────────────

    public async Task<MaintenanceDashboardViewModel> GetDashboardAsync()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var openIncidents = await _db.MaintenanceIncidents
            .CountAsync(i => i.TenantId == tid && !i.IsDeleted && i.Status != "Closed" && i.Status != "Resolved");
        var criticalIncidents = await _db.MaintenanceIncidents
            .CountAsync(i => i.TenantId == tid && !i.IsDeleted && i.Status == "Open" && i.Severity == "Critical");
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
                Severity = i.Severity, Status = i.Status,
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
                Frequency = p.Frequency,
                IsOverdue = p.NextDueDate.HasValue && p.NextDueDate.Value < today
            }).ToListAsync();

        var sensorWarnings = await _db.EquipmentSensorReadings
            .Where(s => s.TenantId == tid && !s.IsDeleted && s.Status != "Normal"
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
        var open = await q.CountAsync(i => i.Status == "Open");
        var inProg = await q.CountAsync(i => i.Status == "InProgress");
        var resolved = await q.CountAsync(i => i.Status == "Resolved" || i.Status == "Closed");

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(i => i.Title.Contains(search) || (i.Equipment != null && i.Equipment.Name.Contains(search)));
        if (!string.IsNullOrWhiteSpace(severity)) q = q.Where(i => i.Severity == severity);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(i => i.Status == status);

        var items = await q.OrderByDescending(i => i.OccurredAt ?? i.CreatedAt)
            .Select(i => new IncidentSummaryItem
            {
                Id = i.Id, Title = i.Title,
                EquipmentName = i.Equipment != null ? i.Equipment.Name : "",
                EquipmentId = i.EquipmentId,
                Severity = i.Severity, Status = i.Status,
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
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tid && !i.IsDeleted);
        if (inc == null) return null;

        var partsUsed = new List<PartUsageDisplay>();
        if (inc.MaintenanceRecordId.HasValue)
        {
            partsUsed = await _db.MaintenancePartUsages
                .Include(u => u.SparePart)
                .Where(u => u.MaintenanceRecordId == inc.MaintenanceRecordId.Value && u.TenantId == tid && !u.IsDeleted)
                .Select(u => new PartUsageDisplay
                {
                    PartCode = u.SparePart!.Code,
                    PartName = u.SparePart.Name,
                    Quantity = u.QuantityUsed,
                    Unit = u.SparePart.Unit,
                    UnitCostAtTime = u.UnitCostAtTime
                }).ToListAsync();
        }

        var availableParts = (inc.Status is "Open" or "InProgress")
            ? await _db.SpareParts
                .Where(p => p.TenantId == tid && !p.IsDeleted && p.StockQuantity > 0)
                .OrderBy(p => p.Code)
                .Select(p => new SparePartOption
                {
                    Id = p.Id, Code = p.Code, Name = p.Name,
                    Unit = p.Unit, StockQuantity = p.StockQuantity, UnitPrice = p.UnitPrice
                }).ToListAsync()
            : new List<SparePartOption>();

        return new MaintenanceIncidentDetailViewModel
        {
            Id = inc.Id, Title = inc.Title, Description = inc.Description,
            Severity = inc.Severity, Status = inc.Status,
            EquipmentId = inc.EquipmentId,
            EquipmentName = inc.Equipment?.Name ?? "",
            ReportedByName = inc.ReportedByUser?.FullName,
            TechnicianName = inc.AssignedTechnician?.FullName,
            OccurredAt = inc.OccurredAt,
            ResolvedAt = inc.ResolvedAt,
            RootCause = inc.RootCause,
            Resolution = inc.Resolution,
            DowntimeHours = inc.DowntimeHours,
            PartsCost = inc.PartsCost,
            LaborCost = inc.LaborCost,
            TotalCost = inc.TotalCost,
            MaintenanceRecordId = inc.MaintenanceRecordId,
            PartsUsed = partsUsed,
            AvailableParts = availableParts
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
        var entity = new MaintenanceIncident
        {
            TenantId = tid,
            EquipmentId = vm.EquipmentId,
            Title = vm.Title,
            Description = vm.Description,
            Severity = vm.Severity,
            Status = "Open",
            OccurredAt = vm.OccurredAt ?? DateTimeOffset.UtcNow,
            ReportedByUserId = _tenant.UserId,
            AssignedTechnicianId = vm.AssignedTechnicianId,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceIncidents.Add(entity);

        var eq = await _db.Equipments.FirstOrDefaultAsync(e => e.Id == vm.EquipmentId && e.TenantId == tid);
        if (eq != null && vm.Severity is "High" or "Critical")
        {
            eq.Status = "Maintenance";
            eq.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        if (vm.Severity is "High" or "Critical")
        {
            await _notif.SendToManagersAsync(
                $"🚨 Sự cố {vm.Severity} — {eq?.Name ?? "thiết bị"}",
                $"{_tenant.UserFullName} báo cáo sự cố \"{vm.Title}\" trên {eq?.Name ?? "thiết bị"}. Thiết bị đã chuyển sang Maintenance.",
                "MaintenanceIncident", entity.Id);
        }

        return entity.Id;
    }

    public async Task<bool> StartIncidentAsync(Guid incidentId)
    {
        var tid = _tenant.TenantId;
        var inc = await _db.MaintenanceIncidents
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == incidentId && i.TenantId == tid && !i.IsDeleted);
        if (inc == null || inc.Status != "Open") return false;
        inc.Status = "InProgress";
        inc.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        await _notif.SendToManagersAsync(
            $"🔧 {_tenant.UserFullName} bắt đầu xử lý sự cố",
            $"Sự cố \"{inc.Title}\" trên {inc.Equipment?.Name ?? "thiết bị"} đã chuyển sang Đang xử lý.",
            "MaintenanceIncident", incidentId);
        return true;
    }

    public async Task<bool> CloseIncidentAsync(Guid incidentId)
    {
        var tid = _tenant.TenantId;
        var inc = await _db.MaintenanceIncidents
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.Id == incidentId && i.TenantId == tid && !i.IsDeleted);
        if (inc == null || inc.Status != "Resolved") return false;
        inc.Status = "Closed";
        inc.UpdatedAt = DateTimeOffset.UtcNow;

        if (inc.Equipment != null && inc.Equipment.Status == "Maintenance")
        {
            inc.Equipment.Status = "Available";
            inc.Equipment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _notif.SendToManagersAsync(
            $"✅ Đã đóng sự cố — {inc.Equipment?.Name ?? "thiết bị"}",
            $"{_tenant.UserFullName} đã đóng sự cố \"{inc.Title}\".",
            "MaintenanceIncident", incidentId);
        return true;
    }

    public async Task<(bool Success, string Message)> ResolveIncidentAsync(ResolveIncidentViewModel vm)
    {
        var tid = _tenant.TenantId;
        var inc = await _db.MaintenanceIncidents.FirstOrDefaultAsync(i => i.Id == vm.IncidentId && i.TenantId == tid && !i.IsDeleted);
        if (inc == null) return (false, "Không tìm thấy sự cố.");
        if (inc.Status is not ("Open" or "InProgress"))
            return (false, $"Chỉ có thể giải quyết sự cố đang Open hoặc InProgress (hiện tại: {inc.Status}).");

        var record = new MaintenanceRecord
        {
            TenantId = tid,
            EquipmentId = inc.EquipmentId,
            MaintenanceType = "Corrective",
            ScheduledDate = DateOnly.FromDateTime(inc.OccurredAt?.DateTime ?? DateTime.UtcNow),
            CompletedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Completed",
            Description = inc.Title,
            WorkDone = vm.Resolution,
            TechnicianUserId = inc.AssignedTechnicianId,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceRecords.Add(record);

        var (partsOk, partsCostFromUsage, partsErr) = await ConsumePartsAsync(tid, vm.PartsUsed, record.Id, "Sự cố: " + inc.Title);
        if (!partsOk)
        {
            _db.MaintenanceRecords.Remove(record);
            return (false, partsErr);
        }

        inc.Status = "Resolved";
        inc.RootCause = vm.RootCause;
        inc.Resolution = vm.Resolution;
        inc.DowntimeHours = vm.DowntimeHours;
        inc.PartsCost = (vm.PartsCost ?? 0) + partsCostFromUsage;
        inc.LaborCost = vm.LaborCost;
        inc.TotalCost = (inc.PartsCost.HasValue || inc.LaborCost.HasValue)
            ? (inc.PartsCost ?? 0) + (inc.LaborCost ?? 0)
            : null;
        inc.ResolvedAt = DateTimeOffset.UtcNow;
        inc.UpdatedAt = DateTimeOffset.UtcNow;

        record.Cost = inc.TotalCost;
        inc.MaintenanceRecordId = record.Id;

        var eq = await _db.Equipments.FirstOrDefaultAsync(e => e.Id == inc.EquipmentId && e.TenantId == tid);
        if (eq != null && eq.Status == "Maintenance")
        {
            eq.Status = "Available";
            eq.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, "Đã xác nhận giải quyết sự cố.");
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
            Frequency = p.Frequency, FrequencyValue = p.FrequencyValue,
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
        var entity = new PmSchedule
        {
            TenantId = _tenant.TenantId,
            EquipmentId = vm.EquipmentId,
            TaskName = vm.TaskName,
            Frequency = vm.Frequency,
            FrequencyValue = vm.FrequencyValue,
            Instructions = vm.Instructions,
            EstimatedDurationMinutes = vm.EstimatedDurationMinutes,
            NextDueDate = vm.FirstDueDate,
            AssignedTechnicianId = vm.AssignedTechnicianId,
            IsActive = true,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.PmSchedules.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    /// <summary>Compute next PM due date from base date + frequency.</summary>
    public static DateOnly ComputeNextDueDate(DateOnly baseDate, string frequency, int? frequencyValue)
    {
        return (frequency ?? "Monthly") switch
        {
            "Daily" => baseDate.AddDays(Math.Max(1, frequencyValue ?? 1)),
            "Weekly" => baseDate.AddDays(7 * Math.Max(1, frequencyValue ?? 1)),
            "Monthly" => baseDate.AddMonths(Math.Max(1, frequencyValue ?? 1)),
            "Quarterly" => baseDate.AddMonths(3 * Math.Max(1, frequencyValue ?? 1)),
            "Yearly" => baseDate.AddYears(Math.Max(1, frequencyValue ?? 1)),
            "Every_X_Hours" => baseDate.AddDays(Math.Max(1, (frequencyValue ?? 24) / 24)),
            _ => baseDate.AddMonths(1)
        };
    }

    public async Task<(bool Success, string Message)> ExecutePmTaskAsync(ExecutePmViewModel vm)
    {
        var tid = _tenant.TenantId;
        var pm = await _db.PmSchedules.FirstOrDefaultAsync(p => p.Id == vm.PmScheduleId && p.TenantId == tid && !p.IsDeleted);
        if (pm == null) return (false, "Không tìm thấy lịch bảo trì.");

        var nextDue = vm.NextDueDate ?? ComputeNextDueDate(vm.CompletedDate, pm.Frequency, pm.FrequencyValue);

        var record = new MaintenanceRecord
        {
            TenantId = tid,
            EquipmentId = pm.EquipmentId,
            MaintenanceType = "Preventive",
            ScheduledDate = pm.NextDueDate ?? DateOnly.FromDateTime(DateTime.Today),
            CompletedDate = vm.CompletedDate,
            Status = "Completed",
            Description = pm.TaskName,
            WorkDone = vm.WorkDone,
            Cost = vm.Cost,
            TechnicianUserId = vm.TechnicianUserId ?? pm.AssignedTechnicianId,
            NextMaintenanceDate = nextDue,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceRecords.Add(record);

        var (partsOk, partsCostFromUsage, partsErr) = await ConsumePartsAsync(tid, vm.PartsUsed, record.Id, "PM: " + pm.TaskName);
        if (!partsOk)
        {
            _db.MaintenanceRecords.Remove(record);
            return (false, partsErr);
        }

        record.Cost = (vm.Cost ?? 0) + partsCostFromUsage;
        if (record.Cost == 0) record.Cost = null;

        pm.LastPerformedDate = vm.CompletedDate;
        pm.NextDueDate = nextDue;
        pm.LastOverdueNotificationAt = null;
        pm.UpdatedAt = DateTimeOffset.UtcNow;

        var eq = await _db.Equipments.FirstOrDefaultAsync(e => e.Id == pm.EquipmentId && e.TenantId == tid);
        if (eq != null) { eq.NextMaintenanceDate = nextDue; eq.UpdatedAt = DateTimeOffset.UtcNow; }

        await _db.SaveChangesAsync();
        return (true, "Đã ghi nhận hoàn thành công việc bảo trì.");
    }

    /// <summary>
    /// Trừ phụ tùng khỏi kho, tạo MaintenancePartUsage link record, trả về tổng chi phí phụ tùng.
    /// </summary>
    private async Task<(bool Success, decimal TotalCost, string ErrorMessage)> ConsumePartsAsync(
        Guid tid, List<PartUsageInput>? items, Guid maintenanceRecordId, string reason)
    {
        if (items == null || items.Count == 0) return (true, 0m, "");

        var validItems = items.Where(i => i.PartId != Guid.Empty && i.Quantity > 0).ToList();
        if (validItems.Count == 0) return (true, 0m, "");

        var ids = validItems.Select(i => i.PartId).ToList();
        var parts = await _db.SpareParts
            .Where(p => ids.Contains(p.Id) && p.TenantId == tid && !p.IsDeleted)
            .ToListAsync();

        decimal totalCost = 0m;
        foreach (var input in validItems)
        {
            var part = parts.FirstOrDefault(p => p.Id == input.PartId);
            if (part == null) return (false, 0m, "Phụ tùng không tồn tại trong kho.");
            if (part.StockQuantity < input.Quantity)
                return (false, 0m, $"Phụ tùng {part.Code} - {part.Name} không đủ tồn (còn {part.StockQuantity} {part.Unit}, cần {input.Quantity}).");

            var before = part.StockQuantity;
            part.StockQuantity -= input.Quantity;
            part.UpdatedAt = DateTimeOffset.UtcNow;
            part.UpdatedByUserId = _tenant.UserId;

            _db.MaintenancePartUsages.Add(new MaintenancePartUsage
            {
                TenantId = tid,
                MaintenanceRecordId = maintenanceRecordId,
                SparePartId = part.Id,
                QuantityUsed = input.Quantity,
                UnitCostAtTime = part.UnitPrice,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = _tenant.UserId
            });

            _db.AuditLogs.Add(new AuditLog
            {
                TenantId = tid,
                UserId = _tenant.UserId,
                UserName = _tenant.UserFullName,
                Action = "StockOut",
                EntityName = "SparePart",
                EntityId = part.Id,
                OldValuesJson = System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = before }),
                NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    StockQuantity = part.StockQuantity,
                    Delta = -input.Quantity,
                    Reason = reason,
                    MaintenanceRecordId = maintenanceRecordId
                }),
                CreatedAt = DateTimeOffset.UtcNow
            });

            totalCost += (part.UnitPrice ?? 0) * input.Quantity;
        }

        return (true, totalCost, "");
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
        var code = await _seq.NextCodeAsync("SparePart", "SP-");
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
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<(bool Success, string Message)> AdjustStockAsync(Guid partId, int delta, string reason)
    {
        var tid = _tenant.TenantId;
        var part = await _db.SpareParts.FirstOrDefaultAsync(p => p.Id == partId && p.TenantId == tid && !p.IsDeleted);
        if (part == null) return (false, "Không tìm thấy phụ tùng.");

        if (delta == 0) return (false, "Số lượng điều chỉnh phải khác 0.");

        if (delta < 0 && part.StockQuantity + delta < 0)
            return (false, $"Không đủ tồn kho. Tồn hiện tại: {part.StockQuantity} {part.Unit}, yêu cầu xuất: {-delta} {part.Unit}.");

        var before = part.StockQuantity;
        part.StockQuantity = part.StockQuantity + delta;
        part.UpdatedAt = DateTimeOffset.UtcNow;
        part.UpdatedByUserId = _tenant.UserId;

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            Action = delta > 0 ? "StockIn" : "StockOut",
            EntityName = "SparePart",
            EntityId = partId,
            OldValuesJson = JsonSerializer.Serialize(new { StockQuantity = before }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                StockQuantity = part.StockQuantity,
                Delta = delta,
                Reason = reason ?? ""
            }),
            CreatedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Phụ tùng vừa được người khác cập nhật. Vui lòng làm mới và thử lại.");
        }

        if (delta < 0 && part.StockQuantity <= part.MinimumStock)
        {
            await _notif.SendToManagersAsync(
                $"📉 Tồn kho thấp — {part.Code} {part.Name}",
                $"Phụ tùng {part.Code} - {part.Name} còn {part.StockQuantity} {part.Unit} (ngưỡng cảnh báo: {part.MinimumStock}).",
                "SparePart", partId);
        }

        var msg = delta > 0
            ? $"Đã nhập kho +{delta} {part.Unit}. Tồn mới: {part.StockQuantity}."
            : $"Đã xuất kho {delta} {part.Unit}. Tồn mới: {part.StockQuantity}.";
        return (true, msg);
    }

    // ─── IoT / SENSOR ────────────────────────────────────────────────────────

    public async Task<List<SensorReadingViewModel>> GetLatestSensorReadingsAsync(Guid equipmentId)
    {
        var tid = _tenant.TenantId;
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

        var readings = await _db.EquipmentSensorReadings
            .Where(r => r.TenantId == tid && !r.IsDeleted && r.EquipmentId == equipmentId && r.ReadingTime >= cutoff)
            .GroupBy(r => r.SensorType)
            .Select(g => g.OrderByDescending(r => r.ReadingTime).First())
            .ToListAsync();

        return readings.Select(r => new SensorReadingViewModel
        {
            SensorType = r.SensorType, Value = r.Value, Unit = r.Unit,
            ReadingTime = r.ReadingTime, Status = r.Status,
            ThresholdWarning = r.ThresholdWarning, ThresholdCritical = r.ThresholdCritical
        }).ToList();
    }

    /// <summary>Giả lập dữ liệu IoT (demo) - tạo readings ngẫu nhiên cho thiết bị</summary>
    public async Task SimulateSensorDataAsync(Guid equipmentId)
    {
        var tid = _tenant.TenantId;
        var eq = await _db.Equipments.FirstOrDefaultAsync(e => e.Id == equipmentId && e.TenantId == tid);
        if (eq == null) return;

        var rng = new Random();
        var sensors = new[]
        {
            new { Type = "Temperature", Min = 35.0, Max = 85.0, Unit = "°C", WarnAt = 70.0, CritAt = 80.0 },
            new { Type = "Vibration",   Min = 0.5,  Max = 8.0,  Unit = "mm/s", WarnAt = 5.0, CritAt = 7.0 },
            new { Type = "Pressure",    Min = 2.5,  Max = 8.0,  Unit = "bar",  WarnAt = 7.0, CritAt = 7.8 },
            new { Type = "RPM",         Min = 1400.0, Max = 1600.0, Unit = "rpm", WarnAt = 1560.0, CritAt = 1590.0 },
            new { Type = "Current",     Min = 8.0,  Max = 20.0, Unit = "A",    WarnAt = 17.0, CritAt = 19.0 }
        };

        var abnormal = new List<(string Type, double Value, double WarnAt, double CritAt, string Unit, string Status)>();

        foreach (var s in sensors)
        {
            var val = Math.Round(s.Min + rng.NextDouble() * (s.Max - s.Min), 2);
            var status = val >= s.CritAt ? "Critical" : val >= s.WarnAt ? "Warning" : "Normal";
            _db.EquipmentSensorReadings.Add(new EquipmentSensorReading
            {
                TenantId = tid, EquipmentId = equipmentId,
                SensorType = s.Type, Value = val, Unit = s.Unit,
                Status = status, ReadingTime = DateTimeOffset.UtcNow,
                ThresholdWarning = s.WarnAt, ThresholdCritical = s.CritAt,
                CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
            if (status != "Normal")
                abnormal.Add((s.Type, val, s.WarnAt, s.CritAt, s.Unit, status));
        }
        await _db.SaveChangesAsync();

        if (abnormal.Any(a => a.Status == "Critical" || a.Status == "Warning"))
        {
            var lines = string.Join("; ", abnormal.Select(a =>
                $"{a.Type}={a.Value}{a.Unit} ({a.Status}, ngưỡng W:{a.WarnAt}/C:{a.CritAt})"));
            var icon = abnormal.Any(a => a.Status == "Critical") ? "🚨" : "⚠️";
            await _notif.SendToManagersAsync(
                $"{icon} Cảm biến cảnh báo — {eq.Name}",
                $"Thiết bị {eq.Code} - {eq.Name}: {lines}.",
                "Equipment", equipmentId);
        }
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
