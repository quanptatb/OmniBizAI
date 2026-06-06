using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public enum ResourceAvailabilitySeverity
{
    Warning = 1,
    Block = 2
}

public sealed record ResourceAvailabilityFinding(
    ResourceAvailabilitySeverity Severity,
    string Code,
    string Message);

public sealed class ResourceAvailabilityCheckResult
{
    public List<ResourceAvailabilityFinding> Findings { get; } = new();
    public bool CanBook => Findings.All(f => f.Severity != ResourceAvailabilitySeverity.Block);
    public IEnumerable<ResourceAvailabilityFinding> Blocks => Findings.Where(f => f.Severity == ResourceAvailabilitySeverity.Block);
    public IEnumerable<ResourceAvailabilityFinding> Warnings => Findings.Where(f => f.Severity == ResourceAvailabilitySeverity.Warning);

    public string BlockMessage() => string.Join(" ", Blocks.Select(f => f.Message));

    public string WarningSuffix()
    {
        var warnings = Warnings.Select(f => f.Message).ToList();
        return warnings.Any() ? $" Cảnh báo: {string.Join(" ", warnings)}" : string.Empty;
    }
}

public class ResourceAvailabilityService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public ResourceAvailabilityService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ResourceAvailabilityCheckResult> CheckPlanTaskBookingAsync(
        Guid? assignedUserId,
        Guid? equipmentId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludedTaskId = null)
    {
        var result = new ResourceAvailabilityCheckResult();

        if (endTime <= startTime)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "InvalidTimeRange",
                "Thời gian kết thúc phải lớn hơn thời gian bắt đầu."));
            return result;
        }

        if (assignedUserId.HasValue)
        {
            await CheckWorkerAsync(result, assignedUserId.Value, startTime, endTime, excludedTaskId);
        }

        if (equipmentId.HasValue)
        {
            await CheckEquipmentAsync(result, equipmentId.Value, startTime, endTime, excludedTaskId);
        }

        return result;
    }

    public async Task<ResourceAvailabilityMatrixViewModel> GetWorkerAvailabilityMatrixAsync(
        DateOnly date,
        int durationHours)
    {
        var tid = _tenant.TenantId;
        var normalizedDuration = Math.Clamp(durationHours, 1, 8);
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var horizonEnd = dayStart.AddDays(1).AddHours(normalizedDuration);
        var nextDate = DateOnly.FromDateTime(horizonEnd.AddTicks(-1));

        var users = await _db.AppUsers
            .Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, u.JobTitle })
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var assignments = await _db.ShiftAssignments
            .Include(a => a.Shift)
            .Where(a => a.TenantId == tid
                && !a.IsDeleted
                && userIds.Contains(a.UserId)
                && a.WorkDate >= date.AddDays(-1)
                && a.WorkDate <= nextDate
                && a.Status != ShiftAssignmentStatus.Cancelled
                && a.Status != ShiftAssignmentStatus.Absent
                && a.Shift != null
                && a.Shift.IsActive)
            .ToListAsync();

        var leaves = await _db.LeaveRequests
            .Include(l => l.EmployeeProfile)
            .Where(l => l.TenantId == tid
                && !l.IsDeleted
                && l.Status == LeaveStatus.Approved
                && l.EmployeeProfile != null
                && userIds.Contains(l.EmployeeProfile.UserId)
                && l.StartDate <= nextDate
                && l.EndDate >= date)
            .ToListAsync();

        var tasks = await _db.PlanTasks
            .Include(t => t.Plan)
            .Where(t => t.TenantId == tid
                && !t.IsDeleted
                && t.AssignedUserId.HasValue
                && userIds.Contains(t.AssignedUserId.Value)
                && t.Status != PlanTaskStatus.Cancelled
                && t.StartTime < horizonEnd
                && t.EndTime > dayStart)
            .ToListAsync();

        var rows = users.Select(user =>
        {
            var userAssignments = assignments.Where(a => a.UserId == user.Id).ToList();
            var userLeaves = leaves.Where(l => l.EmployeeProfile?.UserId == user.Id).ToList();
            var userTasks = tasks.Where(t => t.AssignedUserId == user.Id).ToList();
            var slots = new List<ResourceAvailabilitySlotViewModel>();

            for (var hour = 0; hour < 24; hour++)
            {
                var slotStart = dayStart.AddHours(hour);
                var slotEnd = slotStart.AddHours(normalizedDuration);
                slots.Add(BuildSlot(userAssignments, userLeaves, userTasks, hour, slotStart, slotEnd));
            }

            return new ResourceAvailabilityWorkerRowViewModel
            {
                UserId = user.Id,
                UserName = user.FullName,
                JobTitle = user.JobTitle,
                Slots = slots
            };
        }).ToList();

        return new ResourceAvailabilityMatrixViewModel
        {
            Date = date,
            DurationHours = normalizedDuration,
            Rows = rows
        };
    }

    private async Task CheckWorkerAsync(
        ResourceAvailabilityCheckResult result,
        Guid userId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludedTaskId)
    {
        var tid = _tenant.TenantId;
        var user = await _db.AppUsers
            .Where(u => u.Id == userId && u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
            .Select(u => new { u.FullName })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "WorkerNotFound",
                "Nhân sự được phân công không tồn tại hoặc không còn hoạt động."));
            return;
        }

        var dateRange = GetDateRange(startTime, endTime);
        var shiftIntervals = await GetWorkerShiftIntervalsAsync(userId, dateRange.Start.AddDays(-1), dateRange.End);
        if (!CoversInterval(shiftIntervals, startTime, endTime))
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Warning,
                "WorkerNoShift",
                $"Worker {user.FullName} không có ca làm bao phủ {startTime:dd/MM HH:mm}-{endTime:dd/MM HH:mm}."));
        }

        var leave = await _db.LeaveRequests
            .Include(l => l.EmployeeProfile)
            .Where(l => l.TenantId == tid
                && !l.IsDeleted
                && l.Status == LeaveStatus.Approved
                && l.EmployeeProfile != null
                && l.EmployeeProfile.UserId == userId
                && l.StartDate <= dateRange.End
                && l.EndDate >= dateRange.Start)
            .OrderBy(l => l.StartDate)
            .FirstOrDefaultAsync();
        if (leave != null)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "WorkerOnLeave",
                $"Worker {user.FullName} đã có nghỉ phép được duyệt từ {leave.StartDate:dd/MM/yyyy} đến {leave.EndDate:dd/MM/yyyy}."));
        }

        var conflict = await _db.PlanTasks
            .Include(t => t.Plan)
            .Where(t => t.TenantId == tid
                && !t.IsDeleted
                && t.AssignedUserId == userId
                && t.Status != PlanTaskStatus.Cancelled
                && (!excludedTaskId.HasValue || t.Id != excludedTaskId.Value)
                && t.StartTime < endTime
                && t.EndTime > startTime)
            .OrderBy(t => t.StartTime)
            .FirstOrDefaultAsync();
        if (conflict != null)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "WorkerTaskConflict",
                $"Xung đột lịch trình: {user.FullName} đã được phân công task \"{conflict.Name}\" ({conflict.StartTime:dd/MM HH:mm}-{conflict.EndTime:dd/MM HH:mm})."));
        }
    }

    private async Task CheckEquipmentAsync(
        ResourceAvailabilityCheckResult result,
        Guid equipmentId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludedTaskId)
    {
        var tid = _tenant.TenantId;
        var equipment = await _db.Equipments
            .Where(e => e.Id == equipmentId && e.TenantId == tid && !e.IsDeleted)
            .Select(e => new { e.Name, e.Status })
            .FirstOrDefaultAsync();

        if (equipment == null)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "EquipmentNotFound",
                "Thiết bị được phân công không tồn tại hoặc không thuộc tenant hiện tại."));
            return;
        }

        if (equipment.Status is EquipmentStatus.Maintenance or EquipmentStatus.OutOfOrder or EquipmentStatus.Retired)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "EquipmentUnavailable",
                $"Thiết bị {equipment.Name} đang ở trạng thái {equipment.Status}, không thể phân công."));
        }

        var dateRange = GetDateRange(startTime, endTime);
        var maintenance = await _db.MaintenanceRecords
            .Where(m => m.TenantId == tid
                && !m.IsDeleted
                && m.EquipmentId == equipmentId
                && (m.Status == MaintenanceRecordStatus.Scheduled || m.Status == MaintenanceRecordStatus.InProgress)
                && m.ScheduledDate >= dateRange.Start
                && m.ScheduledDate <= dateRange.End)
            .OrderBy(m => m.ScheduledDate)
            .FirstOrDefaultAsync();
        if (maintenance != null)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "EquipmentMaintenance",
                $"Thiết bị {equipment.Name} có lịch bảo trì {maintenance.MaintenanceType} ngày {maintenance.ScheduledDate:dd/MM/yyyy}."));
        }

        var conflict = await _db.PlanTasks
            .Where(t => t.TenantId == tid
                && !t.IsDeleted
                && t.EquipmentId == equipmentId
                && t.Status != PlanTaskStatus.Cancelled
                && (!excludedTaskId.HasValue || t.Id != excludedTaskId.Value)
                && t.StartTime < endTime
                && t.EndTime > startTime)
            .OrderBy(t => t.StartTime)
            .FirstOrDefaultAsync();
        if (conflict != null)
        {
            result.Findings.Add(new ResourceAvailabilityFinding(
                ResourceAvailabilitySeverity.Block,
                "EquipmentTaskConflict",
                $"Xung đột tài nguyên: thiết bị {equipment.Name} đang dùng cho task \"{conflict.Name}\" ({conflict.StartTime:dd/MM HH:mm}-{conflict.EndTime:dd/MM HH:mm})."));
        }
    }

    private async Task<List<(DateTime Start, DateTime End)>> GetWorkerShiftIntervalsAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var assignments = await _db.ShiftAssignments
            .Include(a => a.Shift)
            .Where(a => a.TenantId == _tenant.TenantId
                && !a.IsDeleted
                && a.UserId == userId
                && a.WorkDate >= fromDate
                && a.WorkDate <= toDate
                && a.Status != ShiftAssignmentStatus.Cancelled
                && a.Status != ShiftAssignmentStatus.Absent
                && a.Shift != null
                && a.Shift.IsActive)
            .ToListAsync();

        return assignments
            .Select(a => ToShiftInterval(a.WorkDate, a.Shift!.StartTime, a.Shift.EndTime))
            .ToList();
    }

    private static ResourceAvailabilitySlotViewModel BuildSlot(
        List<ShiftAssignment> assignments,
        List<LeaveRequest> leaves,
        List<PlanTask> tasks,
        int hour,
        DateTime slotStart,
        DateTime slotEnd)
    {
        var slotStartDate = DateOnly.FromDateTime(slotStart);
        var slotEndDate = DateOnly.FromDateTime(slotEnd.AddTicks(-1));
        var leave = leaves.FirstOrDefault(l => l.StartDate <= slotEndDate && l.EndDate >= slotStartDate);
        if (leave != null)
        {
            return Slot(hour, slotStart, slotEnd, "Leave", "Nghỉ phép", "availability-leave",
                $"{leave.LeaveType}: {leave.StartDate:dd/MM}-{leave.EndDate:dd/MM}");
        }

        var overlappingTasks = tasks
            .Where(t => t.StartTime < slotEnd && t.EndTime > slotStart)
            .OrderBy(t => t.StartTime)
            .ToList();
        if (overlappingTasks.Any())
        {
            return Slot(hour, slotStart, slotEnd, "Busy", "Bận", "availability-busy",
                string.Join("; ", overlappingTasks.Select(t => $"{t.Name} ({t.StartTime:HH:mm}-{t.EndTime:HH:mm})")));
        }

        var shiftIntervals = assignments
            .Where(a => a.Shift != null)
            .Select(a => ToShiftInterval(a.WorkDate, a.Shift!.StartTime, a.Shift.EndTime))
            .ToList();
        if (!CoversInterval(shiftIntervals, slotStart, slotEnd))
        {
            return Slot(hour, slotStart, slotEnd, "NoShift", "Không ca", "availability-no-shift",
                "Không có ca làm bao phủ khung giờ này.");
        }

        return Slot(hour, slotStart, slotEnd, "Available", "Rảnh", "availability-free", "Có ca và chưa có task.");
    }

    private static ResourceAvailabilitySlotViewModel Slot(
        int hour,
        DateTime start,
        DateTime end,
        string status,
        string label,
        string cssClass,
        string description)
    {
        return new ResourceAvailabilitySlotViewModel
        {
            Hour = hour,
            StartTime = start,
            EndTime = end,
            Status = status,
            Label = label,
            CssClass = cssClass,
            Description = description
        };
    }

    private static (DateOnly Start, DateOnly End) GetDateRange(DateTime startTime, DateTime endTime)
    {
        return (
            DateOnly.FromDateTime(startTime),
            DateOnly.FromDateTime(endTime.AddTicks(-1)));
    }

    private static (DateTime Start, DateTime End) ToShiftInterval(DateOnly workDate, TimeOnly start, TimeOnly end)
    {
        var shiftStart = workDate.ToDateTime(start);
        var shiftEnd = workDate.ToDateTime(end);
        if (shiftEnd <= shiftStart)
        {
            shiftEnd = shiftEnd.AddDays(1);
        }

        return (shiftStart, shiftEnd);
    }

    private static bool CoversInterval(
        IEnumerable<(DateTime Start, DateTime End)> intervals,
        DateTime start,
        DateTime end)
    {
        var current = start;
        foreach (var interval in intervals.OrderBy(i => i.Start))
        {
            if (interval.End <= current) continue;
            if (interval.Start > current) return false;

            current = interval.End > current ? interval.End : current;
            if (current >= end) return true;
        }

        return current >= end;
    }
}
