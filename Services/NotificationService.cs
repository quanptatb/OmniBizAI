using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

/// <summary>
/// Service for creating and managing notifications. 
/// When user A makes a change, this creates a notification for users B, C... who need to know.
/// </summary>
public class NotificationService(ApplicationDbContext db, ITenantContext tenant)
{
    /// <summary>Send a notification to specific users.</summary>
    public async Task SendAsync(string title, string body, string? entityName, Guid? entityId, params Guid[] recipientUserIds)
    {
        var tid = tenant.TenantId;
        var notification = new Notification
        {
            TenantId = tid,
            Title = title.Length > 200 ? title[..200] : title,
            Body = body.Length > 2000 ? body[..2000] : body,
            EntityName = entityName,
            EntityId = entityId,
            Status = NotificationStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notifications.Add(notification);

        foreach (var uid in recipientUserIds.Where(u => u != tenant.UserId).Distinct())
        {
            db.NotificationDeliveries.Add(new NotificationDelivery
            {
                TenantId = tid,
                NotificationId = notification.Id,
                UserId = uid,
                Status = NotificationDeliveryStatus.Pending,
                CreatedByUserId = tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Send notification to all active users in the same tenant (except sender).</summary>
    public async Task BroadcastAsync(string title, string body, string? entityName = null, Guid? entityId = null)
    {
        var tid = tenant.TenantId;
        var userIds = await db.AppUsers
            .Where(u => u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted && u.Id != tenant.UserId)
            .Select(u => u.Id)
            .ToListAsync();

        if (!userIds.Any()) return;
        await SendAsync(title, body, entityName, entityId, userIds.ToArray());
    }

    /// <summary>Send notification to all users in a specific department.</summary>
    public async Task SendToDepartmentAsync(string title, string body, Guid departmentId, string? entityName = null, Guid? entityId = null)
    {
        var tid = tenant.TenantId;
        var userIds = await db.AppUsers
            .Where(u => u.TenantId == tid && u.OrganizationUnitId == departmentId && u.Status == UserStatus.Active && !u.IsDeleted && u.Id != tenant.UserId)
            .Select(u => u.Id)
            .ToListAsync();

        if (!userIds.Any()) return;
        await SendAsync(title, body, entityName, entityId, userIds.ToArray());
    }

    /// <summary>Send notification to managers/admins only.</summary>
    public async Task SendToManagersAsync(string title, string body, string? entityName = null, Guid? entityId = null)
    {
        var tid = tenant.TenantId;
        // Find users with manager/admin roles via Identity
        var adminUserNames = await db.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>()
            .Join(db.Set<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == "EXECUTIVE" || x.Name == "DEPARTMENT_MANAGER" || x.Name == "TENANT_ADMIN" || x.Name == "SYSTEM_ADMIN")
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        if (!adminUserNames.Any()) return;
        await SendAsync(title, body, entityName, entityId, adminUserNames.ToArray());
    }

    // ── Read operations ──────────────────────────────────────────────────────

    /// <summary>Get unread count for current user (for bell badge).</summary>
    public async Task<int> GetUnreadCountAsync()
    {
        return await db.NotificationDeliveries
            .Where(d => d.TenantId == tenant.TenantId && d.UserId == tenant.UserId && d.Status != NotificationDeliveryStatus.Read && !d.IsDeleted)
            .CountAsync();
    }

    /// <summary>Get recent notifications for current user.</summary>
    public async Task<List<NotificationItem>> GetMyNotificationsAsync(int take = 50)
    {
        var deliveries = await db.NotificationDeliveries
            .Where(d => d.TenantId == tenant.TenantId && d.UserId == tenant.UserId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .Select(d => new
            {
                d.Id,
                d.Notification!.Title,
                d.Notification.Body,
                d.Notification.EntityName,
                d.Notification.EntityId,
                IsRead = d.Status == NotificationDeliveryStatus.Read,
                CreatedAt = d.Notification.PublishedAt ?? d.CreatedAt,
                d.Notification.CreatedByUserId
            })
            .ToListAsync();

        // Resolve sender names
        var senderIds = deliveries.Where(d => d.CreatedByUserId.HasValue).Select(d => d.CreatedByUserId!.Value).Distinct().ToList();
        var senderNames = senderIds.Any()
            ? await db.AppUsers.Where(u => senderIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName)
            : new Dictionary<Guid, string>();

        return deliveries.Select(d => new NotificationItem
        {
            DeliveryId = d.Id,
            Title = d.Title,
            Body = d.Body,
            EntityName = d.EntityName,
            EntityId = d.EntityId,
            IsRead = d.IsRead,
            CreatedAt = d.CreatedAt,
            SenderName = d.CreatedByUserId.HasValue && senderNames.TryGetValue(d.CreatedByUserId.Value, out var name) ? name : "Hệ thống"
        }).ToList();
    }

    /// <summary>Mark one notification as read.</summary>
    public async Task MarkAsReadAsync(Guid deliveryId)
    {
        var d = await db.NotificationDeliveries.FirstOrDefaultAsync(x => x.Id == deliveryId && x.UserId == tenant.UserId && x.TenantId == tenant.TenantId);
        if (d != null && d.Status != NotificationDeliveryStatus.Read)
        {
            d.Status = NotificationDeliveryStatus.Read;
            d.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Mark all as read for current user.</summary>
    public async Task MarkAllAsReadAsync()
    {
        var unread = await db.NotificationDeliveries
            .Where(d => d.TenantId == tenant.TenantId && d.UserId == tenant.UserId && d.Status != NotificationDeliveryStatus.Read && !d.IsDeleted)
            .ToListAsync();

        foreach (var d in unread)
        {
            d.Status = NotificationDeliveryStatus.Read;
            d.ReadAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync();
    }
}
