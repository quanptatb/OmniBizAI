using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Services;

/// <summary>
/// Background service quét PM schedules định kỳ. Notify managers (theo tenant) khi:
/// - Overdue: NextDueDate < today
/// - Due soon: NextDueDate trong 7 ngày tới
/// Chống spam bằng PmSchedule.LastOverdueNotificationAt (>= 24h mới gửi lại).
/// </summary>
public class PmOverdueNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PmOverdueNotificationService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DedupWindow = TimeSpan.FromHours(24);
    private const int DueSoonDays = 7;

    public PmOverdueNotificationService(IServiceScopeFactory scopeFactory, ILogger<PmOverdueNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PM overdue scan failed");
            }

            try { await Task.Delay(CheckInterval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var dueSoonCutoff = today.AddDays(DueSoonDays);
        var dedupCutoff = DateTimeOffset.UtcNow - DedupWindow;

        var candidates = await db.PmSchedules
            .Include(p => p.Equipment)
            .Where(p => !p.IsDeleted && p.IsActive
                && p.NextDueDate.HasValue
                && p.NextDueDate.Value <= dueSoonCutoff
                && (p.LastOverdueNotificationAt == null || p.LastOverdueNotificationAt < dedupCutoff))
            .ToListAsync(ct);

        if (candidates.Count == 0) return;

        var byTenant = candidates.GroupBy(p => p.TenantId);
        foreach (var group in byTenant)
        {
            if (ct.IsCancellationRequested) break;
            await NotifyTenantAsync(db, group.Key, group.ToList(), today, ct);
        }
    }

    private async Task NotifyTenantAsync(ApplicationDbContext db, Guid tenantId, List<PmSchedule> schedules, DateOnly today, CancellationToken ct)
    {
        var managerIds = await (
            from u in db.AppUsers
            join ur in db.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>() on u.Id equals ur.UserId
            join r in db.Set<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>() on ur.RoleId equals r.Id
            where u.TenantId == tenantId && u.Status == UserStatus.Active && !u.IsDeleted
                && (r.Name == "EXECUTIVE" || r.Name == "DEPARTMENT_MANAGER"
                    || r.Name == "TENANT_ADMIN" || r.Name == "SYSTEM_ADMIN")
            select u.Id
        ).Distinct().ToListAsync(ct);

        if (managerIds.Count == 0)
        {
            foreach (var pm in schedules) pm.LastOverdueNotificationAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }

        foreach (var pm in schedules)
        {
            var overdue = pm.NextDueDate.HasValue && pm.NextDueDate.Value < today;
            var icon = overdue ? "⛔" : "⏰";
            var label = overdue
                ? $"Quá hạn {(today.DayNumber - pm.NextDueDate!.Value.DayNumber)} ngày"
                : $"Đến hạn {pm.NextDueDate:dd/MM/yyyy}";
            var title = $"{icon} PM {label} — {pm.Equipment?.Name ?? "thiết bị"}";
            var body = $"Công việc bảo trì \"{pm.TaskName}\" trên {pm.Equipment?.Name ?? "thiết bị"} {(overdue ? "đã quá hạn" : "sắp đến hạn")} ({pm.NextDueDate:dd/MM/yyyy}). Tần suất: {pm.Frequency}.";

            var notif = new Notification
            {
                TenantId = tenantId,
                Title = title.Length > 200 ? title[..200] : title,
                Body = body.Length > 2000 ? body[..2000] : body,
                EntityName = "PmSchedule",
                EntityId = pm.Id,
                Status = NotificationStatus.Published,
                PublishedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Notifications.Add(notif);

            foreach (var uid in managerIds)
            {
                db.NotificationDeliveries.Add(new NotificationDelivery
                {
                    TenantId = tenantId,
                    NotificationId = notif.Id,
                    UserId = uid,
                    Status = NotificationDeliveryStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            pm.LastOverdueNotificationAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("PM overdue scan: tenant {Tenant} notified {Count} schedules to {Managers} managers",
            tenantId, schedules.Count, managerIds.Count);
    }
}
