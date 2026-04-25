using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var query = _db.Notifications.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.Title,
                x.Message,
                x.Type,
                x.Priority,
                x.EntityType,
                x.EntityId,
                x.ActionUrl,
                x.IsRead,
                x.ReadAt,
                x.IsEmailSent,
                x.CreatedAt
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { notification.Id, notification.IsRead, notification.ReadAt });
    }
}
