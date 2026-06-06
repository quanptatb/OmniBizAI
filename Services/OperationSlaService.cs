using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Services;

public interface IOperationSlaService
{
    Task ApplySubmittedAsync(OperationRequest request, DateTimeOffset submittedAt, CancellationToken cancellationToken = default);
    Task ApplyApprovedAsync(OperationRequest request, DateTimeOffset approvedAt, CancellationToken cancellationToken = default);
    Task<int> CheckBreachesAsync(CancellationToken cancellationToken = default);
}

public class OperationSlaService(ApplicationDbContext db) : IOperationSlaService
{
    private static readonly OperationStatus[] ApprovalStatuses = [OperationStatus.Submitted, OperationStatus.InReview];
    private static readonly OperationStatus[] ResolutionStatuses = [OperationStatus.Approved, OperationStatus.InProgress, OperationStatus.OnHold];
    private static readonly TimeSpan WarningWindow = TimeSpan.FromHours(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task ApplySubmittedAsync(OperationRequest request, DateTimeOffset submittedAt, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(request.TenantId, request.Priority, cancellationToken);
        request.SubmittedAt = submittedAt;
        request.ApprovalDueAt = submittedAt.AddHours(policy.MaxApprovalHours);
    }

    public async Task ApplyApprovedAsync(OperationRequest request, DateTimeOffset approvedAt, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(request.TenantId, request.Priority, cancellationToken);
        request.ApprovedAt = approvedAt;
        request.ResolutionDueAt = approvedAt.AddHours(policy.MaxResolutionHours);
    }

    public async Task<int> CheckBreachesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var trackedCount = 0;

        var warningCutoff = now.Add(WarningWindow);

        var approvalRequests = await db.OperationRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted
                && ApprovalStatuses.Contains(r.Status)
                && r.ApprovalDueAt.HasValue
                && r.ApprovalDueAt.Value <= warningCutoff)
            .ToListAsync(cancellationToken);

        foreach (var request in approvalRequests)
        {
            var dueAt = request.ApprovalDueAt!.Value;
            if (now > dueAt)
            {
                trackedCount += await TrackBreachAsync(request, OperationSlaBreachType.ApprovalOverdue, dueAt, now, true, cancellationToken);
            }
            else if (dueAt - now <= WarningWindow)
            {
                trackedCount += await TrackBreachAsync(request, OperationSlaBreachType.ApprovalWarning, dueAt, now, false, cancellationToken);
            }
        }

        var resolutionRequests = await db.OperationRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted
                && ResolutionStatuses.Contains(r.Status)
                && r.ResolutionDueAt.HasValue
                && r.ResolutionDueAt.Value <= warningCutoff)
            .ToListAsync(cancellationToken);

        foreach (var request in resolutionRequests)
        {
            var dueAt = request.ResolutionDueAt!.Value;
            if (now > dueAt)
            {
                trackedCount += await TrackBreachAsync(request, OperationSlaBreachType.ResolutionOverdue, dueAt, now, true, cancellationToken);
            }
            else if (dueAt - now <= WarningWindow)
            {
                trackedCount += await TrackBreachAsync(request, OperationSlaBreachType.ResolutionWarning, dueAt, now, false, cancellationToken);
            }
        }

        if (trackedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return trackedCount;
    }

    public static DateTimeOffset? GetActiveDueAt(OperationStatus status, DateTimeOffset? approvalDueAt, DateTimeOffset? resolutionDueAt) =>
        status switch
        {
            OperationStatus.Submitted or OperationStatus.InReview => approvalDueAt,
            OperationStatus.Approved or OperationStatus.InProgress or OperationStatus.OnHold => resolutionDueAt,
            _ => null
        };

    public static string GetActiveStage(OperationStatus status) =>
        status switch
        {
            OperationStatus.Submitted or OperationStatus.InReview => "Approval",
            OperationStatus.Approved or OperationStatus.InProgress or OperationStatus.OnHold => "Resolution",
            _ => ""
        };

    private async Task<OperationSlaPolicy> GetPolicyAsync(Guid tenantId, PriorityLevel priority, CancellationToken cancellationToken)
    {
        var policy = await db.OperationSlaPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Priority == priority && p.IsActive, cancellationToken);

        return policy ?? DefaultPolicy(tenantId, priority);
    }

    private static OperationSlaPolicy DefaultPolicy(Guid tenantId, PriorityLevel priority)
    {
        var (approvalHours, resolutionHours) = priority switch
        {
            PriorityLevel.Critical => (2, 24),
            PriorityLevel.High => (8, 48),
            PriorityLevel.Normal => (24, 96),
            PriorityLevel.Low => (48, 168),
            _ => (24, 96)
        };

        return new OperationSlaPolicy
        {
            TenantId = tenantId,
            Priority = priority,
            MaxApprovalHours = approvalHours,
            MaxResolutionHours = resolutionHours,
            IsActive = true
        };
    }

    private async Task<int> TrackBreachAsync(
        OperationRequest request,
        OperationSlaBreachType breachType,
        DateTimeOffset dueAt,
        DateTimeOffset now,
        bool escalated,
        CancellationToken cancellationToken)
    {
        var exists = await db.OperationSlaBreaches.AnyAsync(b =>
            b.TenantId == request.TenantId
            && b.OperationRequestId == request.Id
            && b.BreachType == breachType
            && !b.IsDeleted,
            cancellationToken);

        if (exists) return 0;

        var hoursOverdue = now > dueAt ? (decimal)Math.Round((now - dueAt).TotalHours, 2) : 0m;
        var breach = new OperationSlaBreach
        {
            TenantId = request.TenantId,
            OperationRequestId = request.Id,
            BreachType = breachType,
            DueAt = dueAt,
            DetectedAt = now,
            HoursOverdue = hoursOverdue,
            IsEscalated = escalated,
            NotificationSentAt = now,
            Notes = BuildBreachTitle(request, breachType, hoursOverdue)
        };

        db.OperationSlaBreaches.Add(breach);
        AddAuditLog(request, breach, now);

        await CreateNotificationAsync(request, breachType, dueAt, hoursOverdue, escalated, cancellationToken);
        return 1;
    }

    private void AddAuditLog(OperationRequest request, OperationSlaBreach breach, DateTimeOffset now)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = request.TenantId,
            UserName = "System - SLA Watcher",
            Action = breach.IsEscalated ? "SlaEscalate" : "SlaWarning",
            EntityName = "OperationRequest",
            EntityId = request.Id,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                request.RequestNo,
                request.Status,
                breach.BreachType,
                breach.DueAt,
                breach.DetectedAt,
                breach.HoursOverdue,
                breach.IsEscalated
            }, JsonOptions),
            ExtraJson = JsonSerializer.Serialize(new
            {
                OperationSlaBreachId = breach.Id,
                Stage = breach.BreachType is OperationSlaBreachType.ApprovalWarning or OperationSlaBreachType.ApprovalOverdue
                    ? "Approval"
                    : "Resolution"
            }, JsonOptions),
            CreatedAt = now
        });
    }

    private async Task CreateNotificationAsync(
        OperationRequest request,
        OperationSlaBreachType breachType,
        DateTimeOffset dueAt,
        decimal hoursOverdue,
        bool escalated,
        CancellationToken cancellationToken)
    {
        var recipientIds = await GetManagerUserIdsAsync(
            request.TenantId,
            escalated
                ? ["EXECUTIVE", "TENANT_ADMIN", "SYSTEM_ADMIN"]
                : ["DEPARTMENT_MANAGER", "EXECUTIVE", "TENANT_ADMIN", "SYSTEM_ADMIN"],
            cancellationToken);

        if (breachType is OperationSlaBreachType.ResolutionWarning or OperationSlaBreachType.ResolutionOverdue)
            recipientIds.Add(request.RequestedByUserId);

        recipientIds = recipientIds.Distinct().ToList();
        if (!recipientIds.Any()) return;

        var title = BuildBreachTitle(request, breachType, hoursOverdue);
        var body = breachType is OperationSlaBreachType.ApprovalWarning or OperationSlaBreachType.ResolutionWarning
            ? $"Yêu cầu {request.RequestNo} còn dưới 2 giờ trước hạn SLA ({dueAt:HH:mm dd/MM/yyyy})."
            : $"Yêu cầu {request.RequestNo} đã quá hạn SLA {hoursOverdue:N1} giờ. Cần xử lý/escalate ngay.";

        var notification = new Notification
        {
            TenantId = request.TenantId,
            Title = title.Length > 200 ? title[..200] : title,
            Body = body,
            EntityName = "OperationRequest",
            EntityId = request.Id,
            Status = NotificationStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Notifications.Add(notification);
        foreach (var userId in recipientIds)
        {
            db.NotificationDeliveries.Add(new NotificationDelivery
            {
                TenantId = request.TenantId,
                NotificationId = notification.Id,
                UserId = userId,
                Status = NotificationDeliveryStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private async Task<List<Guid>> GetManagerUserIdsAsync(Guid tenantId, string[] roles, CancellationToken cancellationToken)
    {
        var roleUserIds = await db.Set<IdentityUserRole<Guid>>()
            .Join(db.Set<IdentityRole<Guid>>(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name != null && roles.Contains(x.Name))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!roleUserIds.Any()) return [];

        return await db.AppUsers
            .Where(u => u.TenantId == tenantId
                && roleUserIds.Contains(u.Id)
                && u.Status == UserStatus.Active
                && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    private static string BuildBreachTitle(OperationRequest request, OperationSlaBreachType breachType, decimal hoursOverdue) =>
        breachType switch
        {
            OperationSlaBreachType.ApprovalWarning => $"SLA duyệt sắp quá hạn: {request.RequestNo}",
            OperationSlaBreachType.ApprovalOverdue => $"SLA duyệt quá hạn {hoursOverdue:N1}h: {request.RequestNo}",
            OperationSlaBreachType.ResolutionWarning => $"SLA xử lý sắp quá hạn: {request.RequestNo}",
            OperationSlaBreachType.ResolutionOverdue => $"SLA xử lý quá hạn {hoursOverdue:N1}h: {request.RequestNo}",
            _ => $"Cảnh báo SLA: {request.RequestNo}"
        };
}
