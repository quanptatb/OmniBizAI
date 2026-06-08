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
    private readonly NumberSequenceService _seq;

    public ResourceManagementService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini, NumberSequenceService seq)
    {
        _db = db;
        _tenant = tenant;
        _gemini = gemini;
        _seq = seq;
    }

    // ─── DASHBOARD ──────────────────────────────────────────────────────────

    public async Task<ResourceDashboardViewModel> GetDashboardAsync()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var equipmentCount = await _db.Equipments.CountAsync(e => e.TenantId == tid && !e.IsDeleted);
        var equipmentInMaintenance = await _db.Equipments.CountAsync(e => e.TenantId == tid && !e.IsDeleted && e.Status == "Maintenance");
        var overdueMaintenance = await _db.Equipments.CountAsync(e => e.TenantId == tid && !e.IsDeleted
            && e.NextMaintenanceDate.HasValue && e.NextMaintenanceDate.Value < today && e.Status != "Maintenance");

        var shiftCount = await _db.WorkShifts.CountAsync(s => s.TenantId == tid && !s.IsDeleted && s.IsActive);
        var todayAssignments = await _db.ShiftAssignments.CountAsync(s => s.TenantId == tid && !s.IsDeleted && s.WorkDate == today);

        var expiredCerts = await _db.EmployeeCertificates.CountAsync(c => c.TenantId == tid && !c.IsDeleted
            && c.ExpiryDate.HasValue && c.ExpiryDate.Value < today);
        var expiringCerts = await _db.EmployeeCertificates.CountAsync(c => c.TenantId == tid && !c.IsDeleted
            && c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(30));

        var workspaceCount = await _db.Workspaces.CountAsync(w => w.TenantId == tid && !w.IsDeleted && w.Status == "Active");

        var upcomingMaintenance = await _db.MaintenanceRecords
            .Include(m => m.Equipment)
            .Where(m => m.TenantId == tid && !m.IsDeleted
                && m.Status == "Scheduled"
                && m.ScheduledDate >= today && m.ScheduledDate <= today.AddDays(7))
            .OrderBy(m => m.ScheduledDate)
            .Take(5)
            .Select(m => new MaintenanceAlertItem
            {
                Id = m.Id,
                EquipmentName = m.Equipment != null ? m.Equipment.Name : "",
                EquipmentCode = m.Equipment != null ? m.Equipment.Code : "",
                MaintenanceType = m.MaintenanceType,
                ScheduledDate = m.ScheduledDate,
                Status = m.Status
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
                Status = e.Status,
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
            q = q.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(e => e.Type == type);

        return await q.OrderBy(e => e.Code).Select(e => new EquipmentSummaryItem
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Type = e.Type,
            Status = e.Status,
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

        return new EquipmentDetailViewModel
        {
            Id = eq.Id, Code = eq.Code, Name = eq.Name,
            Type = eq.Type, Status = eq.Status, Location = eq.Location,
            Manufacturer = eq.Manufacturer, Model = eq.Model, SerialNumber = eq.SerialNumber,
            PurchaseDate = eq.PurchaseDate, PurchasePrice = eq.PurchasePrice,
            LifespanYears = eq.LifespanYears, NextMaintenanceDate = eq.NextMaintenanceDate,
            Notes = eq.Notes,
            MaintenanceRecords = eq.MaintenanceRecords.OrderByDescending(m => m.ScheduledDate).Select(m => new MaintenanceRecordItem
            {
                Id = m.Id,
                MaintenanceType = m.MaintenanceType,
                ScheduledDate = m.ScheduledDate,
                CompletedDate = m.CompletedDate,
                TechnicianName = m.TechnicianUser?.FullName,
                Status = m.Status,
                Description = m.Description,
                WorkDone = m.WorkDone,
                Cost = m.Cost,
                NextMaintenanceDate = m.NextMaintenanceDate
            }).ToList()
        };
    }

    public async Task<Guid> CreateEquipmentAsync(EquipmentCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var code = await _seq.NextCodeAsync("Equipment", "EQ-");
        var entity = new Equipment
        {
            TenantId = tid,
            Code = code,
            Name = vm.Name, Type = vm.Type, Location = vm.Location,
            Manufacturer = vm.Manufacturer, Model = vm.Model, SerialNumber = vm.SerialNumber,
            PurchaseDate = vm.PurchaseDate, PurchasePrice = vm.PurchasePrice,
            LifespanYears = vm.LifespanYears, NextMaintenanceDate = vm.NextMaintenanceDate,
            Notes = vm.Notes, Status = "Available",
            CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Equipments.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ScheduleMaintenanceAsync(ScheduleMaintenanceViewModel vm)
    {
        var eq = await _db.Equipments.FindAsync(vm.EquipmentId);
        if (eq == null || eq.TenantId != _tenant.TenantId) return false;

        var record = new MaintenanceRecord
        {
            TenantId = _tenant.TenantId,
            EquipmentId = vm.EquipmentId,
            MaintenanceType = vm.MaintenanceType,
            ScheduledDate = vm.ScheduledDate,
            Description = vm.Description,
            TechnicianUserId = vm.TechnicianUserId,
            Status = "Scheduled",
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceRecords.Add(record);

        // Update equipment status and next maintenance date
        if (vm.MaintenanceType == "Preventive" || vm.MaintenanceType == "Emergency")
        {
            eq.Status = "Maintenance";
        }
        eq.NextMaintenanceDate = vm.ScheduledDate;
        eq.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteMaintenanceAsync(CompleteMaintenanceViewModel vm)
    {
        var record = await _db.MaintenanceRecords.Include(m => m.Equipment)
            .FirstOrDefaultAsync(m => m.Id == vm.RecordId && m.TenantId == _tenant.TenantId);
        if (record == null) return false;

        record.Status = "Completed";
        record.CompletedDate = vm.CompletedDate;
        record.WorkDone = vm.WorkDone;
        record.Cost = vm.Cost;
        record.NextMaintenanceDate = vm.NextMaintenanceDate;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        if (record.Equipment != null)
        {
            record.Equipment.Status = "Available";
            record.Equipment.NextMaintenanceDate = vm.NextMaintenanceDate;
            record.Equipment.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await _db.SaveChangesAsync();
        return true;
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
                Status = a.Status,
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
            existing.ShiftId = vm.ShiftId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.ShiftAssignments.Add(new ShiftAssignment
            {
                TenantId = tid,
                ShiftId = vm.ShiftId,
                UserId = vm.UserId,
                WorkDate = vm.WorkDate,
                Status = "Scheduled",
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        return true;
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
        _db.EmployeeCertificates.Add(new EmployeeCertificate
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
        });
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
            Status = w.Status, Notes = w.Notes
        }).ToListAsync();
    }

    public async Task<Guid> CreateWorkspaceAsync(WorkspaceCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var seq = await _db.Workspaces.CountAsync(w => w.TenantId == tid) + 1;
        var entity = new Workspace
        {
            TenantId = tid,
            Code = $"WS-{seq:D3}",
            Name = vm.Name, Type = vm.Type,
            Location = vm.Location, AreaSqm = vm.AreaSqm,
            Capacity = vm.Capacity, Status = "Active",
            ParentId = vm.ParentId, Notes = vm.Notes,
            CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Workspaces.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }
}
