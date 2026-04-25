using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/identity")]
public class IdentityApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public IdentityApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var data = await _db.Users.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Email)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.FullName,
                x.Phone,
                x.IsActive,
                x.IsLocked,
                x.LockedUntil,
                x.FailedLoginCount,
                x.LastLoginAt,
                x.EmailConfirmed,
                roleCount = x.UserRoles.Count,
                hasEmployeeProfile = x.Employee != null
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
