using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISeedDataService _seedDataService;

    public AdminController(ISeedDataService seedDataService)
    {
        _seedDataService = seedDataService;
    }

    [HttpPost("seed-data")]
    public async Task<ActionResult<ApiResponse<object>>> Seed(CancellationToken cancellationToken)
    {
        await _seedDataService.SeedAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { seeded = true }, "Seed data completed"));
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Health()
    {
        return Ok(ApiResponse<object>.Ok(new { status = "ok", database = "SQL Server" }));
    }
}
