using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Identity;
using OmniBizAI.Domain.Entities.Notification;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public NotificationService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public Task<PagedResult<NotificationDto>> GetNotificationsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUser();
        var query = _unitOfWork.Repository<Notification>().Query()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(x.Id, x.Title, x.Message, x.Type, x.Priority, x.EntityType, x.EntityId, x.ActionUrl, x.IsRead, x.CreatedAt));

        return Task.FromResult(PagedResult<NotificationDto>.Create(query, request));
    }

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUser();
        var count = _unitOfWork.Repository<Notification>().Query().Count(x => x.UserId == userId && !x.IsRead);
        return Task.FromResult(count);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUser();
        var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException("Notification is outside current user scope.");
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUser();
        var notifications = _unitOfWork.Repository<Notification>().Query()
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToList();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyUserAsync(Guid userId, CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Repository<Notification>().AddAsync(CreateNotification(userId, request), cancellationToken);
    }

    public async Task NotifyRolesAsync(IReadOnlyCollection<string> roleNames, CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var roleIds = _unitOfWork.Repository<Role>().Query()
            .Where(role => roleNames.Contains(role.Name))
            .Select(role => role.Id)
            .ToList();
        var userIds = _unitOfWork.Repository<UserRole>().Query()
            .Where(userRole => roleIds.Contains(userRole.RoleId))
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToList();

        foreach (var userId in userIds)
        {
            await NotifyUserAsync(userId, request, cancellationToken);
        }
    }

    private Guid RequireCurrentUser()
    {
        return _currentUserService.UserId ?? throw new UnauthorizedAccessException("Current user is required.");
    }

    private static Notification CreateNotification(Guid userId, CreateNotificationRequest request)
    {
        return new Notification
        {
            UserId = userId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Priority = request.Priority,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            ActionUrl = request.ActionUrl
        };
    }
}
