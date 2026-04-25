using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Services.Integrations;

namespace OmniBizAI.Controllers.Api;

[ApiController]
[Route("api/v1/system")]
public class SystemApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfigurationStatusService _configurationStatus;

    public SystemApiController(ApplicationDbContext db, IConfigurationStatusService configurationStatus)
    {
        _db = db;
        _configurationStatus = configurationStatus;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var canConnect = false;
        try
        {
            canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            // Keep health endpoint useful even when DB is booting.
        }

        return Ok(new
        {
            service = "OmniBizAI",
            status = "ok",
            database = canConnect ? "connected" : "not_connected",
            utc = DateTime.UtcNow
        });
    }

    [HttpGet("configuration")]
    public IActionResult Configuration()
    {
        return Ok(_configurationStatus.GetSnapshot());
    }
}
