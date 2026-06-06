using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class CommandCenterService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<CommandCenterViewModel> GetDashboardAsync()
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var yesterdayStart = todayStart.AddDays(-1);

        // ── Open Requests ────────────────────────────────────────────────────
        var openStatuses = new List<OperationStatus> { OperationStatus.Submitted, OperationStatus.Approved, OperationStatus.InReview, OperationStatus.InProgress };
        var openRequests = await db.OperationRequests
            .Where(r => r.TenantId == tid && !r.IsDeleted && openStatuses.Contains(r.Status))
            .CountAsync();
        var openRequestsYesterday = await db.OperationRequests
            .Where(r => r.TenantId == tid && !r.IsDeleted && r.CreatedAt < todayStart && openStatuses.Contains(r.Status))
            .CountAsync();

        // ── Critical Incidents ───────────────────────────────────────────────
        var closedStatuses = new List<IncidentStatus> { IncidentStatus.Closed, IncidentStatus.Resolved };
        var criticalIncidents = await db.MaintenanceIncidents
            .Where(i => i.TenantId == tid && !i.IsDeleted && i.Severity == IncidentSeverity.Critical && !closedStatuses.Contains(i.Status))
            .CountAsync();
        var criticalIncidentsYesterday = await db.MaintenanceIncidents
            .Where(i => i.TenantId == tid && !i.IsDeleted && i.Severity == IncidentSeverity.Critical && !closedStatuses.Contains(i.Status) && i.CreatedAt < todayStart)
            .CountAsync();

        // ── Overdue PM ───────────────────────────────────────────────────────
        var overduePm = await db.PmSchedules
            .Where(s => s.TenantId == tid && !s.IsDeleted && s.IsActive && s.NextDueDate.HasValue && s.NextDueDate.Value < today)
            .CountAsync();

        // ── Equipment in Maintenance ─────────────────────────────────────────
        var equipInMaintenance = await db.Equipments
            .Where(e => e.TenantId == tid && !e.IsDeleted && e.Status == EquipmentStatus.Maintenance)
            .CountAsync();

        // ── SLA Breach Today ─────────────────────────────────────────────────
        var slaBreachToday = await db.OperationSlaBreaches
            .Where(b => b.TenantId == tid && !b.IsDeleted && b.CreatedAt >= todayStart)
            .CountAsync();
        var slaBreachYesterday = await db.OperationSlaBreaches
            .Where(b => b.TenantId == tid && !b.IsDeleted && b.CreatedAt >= yesterdayStart && b.CreatedAt < todayStart)
            .CountAsync();

        // ── Pending Approvals ────────────────────────────────────────────────
        var pendingApprovals = await db.ApprovalTasks
            .Where(a => a.TenantId == tid && !a.IsDeleted && a.Status == ApprovalStatus.Pending)
            .CountAsync();
        var pendingApprovalsYesterday = await db.ApprovalTasks
            .Where(a => a.TenantId == tid && !a.IsDeleted && a.Status == ApprovalStatus.Pending && a.CreatedAt < todayStart)
            .CountAsync();

        // ── Equipment Heatmap ────────────────────────────────────────────────
        var equipmentHeatmap = await db.Equipments
            .Where(e => e.TenantId == tid && !e.IsDeleted)
            .OrderBy(e => e.Code)
            .Select(e => new EquipmentHeatmapItem
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                Status = e.Status.ToString(),
                Location = e.Location
            })
            .ToListAsync();

        return new CommandCenterViewModel
        {
            OpenRequests = openRequests,
            OpenRequestsDelta = openRequests - openRequestsYesterday,
            CriticalIncidents = criticalIncidents,
            CriticalIncidentsDelta = criticalIncidents - criticalIncidentsYesterday,
            OverduePm = overduePm,
            EquipmentInMaintenance = equipInMaintenance,
            SlaBreachToday = slaBreachToday,
            SlaBreachYesterday = slaBreachYesterday,
            PendingApprovals = pendingApprovals,
            PendingApprovalsDelta = pendingApprovals - pendingApprovalsYesterday,
            EquipmentHeatmap = equipmentHeatmap
        };
    }
}
