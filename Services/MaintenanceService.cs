using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class MaintenanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly GeminiService _gemini;

    public MaintenanceService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini)
    {
        _db = db; _tenant = tenant; _gemini = gemini;
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

        // IoT status summary
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
            MaintenanceRecordId = inc.MaintenanceRecordId
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

        // Mark equipment as having issue
        var eq = await _db.Equipments.FindAsync(vm.EquipmentId);
        if (eq != null && vm.Severity is "High" or "Critical")
        {
            eq.Status = "Maintenance";
            eq.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ResolveIncidentAsync(ResolveIncidentViewModel vm)
    {
        var tid = _tenant.TenantId;
        var inc = await _db.MaintenanceIncidents.FindAsync(vm.IncidentId);
        if (inc == null || inc.TenantId != tid) return false;

        inc.Status = "Resolved";
        inc.RootCause = vm.RootCause;
        inc.Resolution = vm.Resolution;
        inc.DowntimeHours = vm.DowntimeHours;
        inc.ResolvedAt = DateTimeOffset.UtcNow;
        inc.UpdatedAt = DateTimeOffset.UtcNow;

        // Create a CM record for history
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
        inc.MaintenanceRecordId = record.Id;

        // Restore equipment status
        var eq = await _db.Equipments.FindAsync(inc.EquipmentId);
        if (eq != null && eq.Status == "Maintenance")
        {
            eq.Status = "Available";
            eq.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        return true;
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

    public async Task<bool> ExecutePmTaskAsync(ExecutePmViewModel vm)
    {
        var pm = await _db.PmSchedules.FindAsync(vm.PmScheduleId);
        if (pm == null || pm.TenantId != _tenant.TenantId) return false;

        // Create maintenance record
        var record = new MaintenanceRecord
        {
            TenantId = _tenant.TenantId,
            EquipmentId = pm.EquipmentId,
            MaintenanceType = "Preventive",
            ScheduledDate = pm.NextDueDate ?? DateOnly.FromDateTime(DateTime.Today),
            CompletedDate = vm.CompletedDate,
            Status = "Completed",
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
        if (eq != null) { eq.NextMaintenanceDate = vm.NextDueDate; eq.UpdatedAt = DateTimeOffset.UtcNow; }

        await _db.SaveChangesAsync();
        return true;
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
        var seq = await _db.SpareParts.CountAsync(p => p.TenantId == tid) + 1;
        var entity = new SparePart
        {
            TenantId = tid,
            Code = $"SP-{seq:D4}",
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

    public async Task<bool> AdjustStockAsync(Guid partId, int delta, string reason)
    {
        var part = await _db.SpareParts.FindAsync(partId);
        if (part == null || part.TenantId != _tenant.TenantId) return false;
        part.StockQuantity = Math.Max(0, part.StockQuantity + delta);
        part.UpdatedAt = DateTimeOffset.UtcNow;
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
            ReadingTime = r.ReadingTime, Status = r.Status,
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
            var status = val >= s.CritAt ? "Critical" : val >= s.WarnAt ? "Warning" : "Normal";
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
