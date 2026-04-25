using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetNotifications([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(await _notificationService.GetNotificationsAsync(request, cancellationToken)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<object>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<object>.Ok(new { count = await _notificationService.GetUnreadCountAsync(cancellationToken) }));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Notification marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { read = true }, "Notifications marked as read"));
    }
}
