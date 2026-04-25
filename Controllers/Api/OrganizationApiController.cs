using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using OmniBizAI.Data;

namespace OmniBizAI.Controllers.Api;

[Authorize(Roles = "Admin,Director,Manager,HR")]
[ApiController]
[Route("api/v1/organization")]
public class OrganizationApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public OrganizationApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> Employees(CancellationToken cancellationToken)
    {
        var data = await _db.Employees.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.EmployeeCode)
            .Select(x => new
            {
                x.Id,
                x.EmployeeCode,
                x.FullName,
                x.Email,
                x.Phone,
                x.DepartmentId,
                x.PositionId,
                x.ManagerId,
                x.EmploymentType,
                x.Status,
                x.JoinDate
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [HttpGet("departments")]
    public async Task<IActionResult> Departments(CancellationToken cancellationToken)
    {
        var data = await _db.Departments.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.ParentDepartmentId,
                x.ManagerId,
                x.BudgetLimit,
                x.Level,
                x.IsActive,
                employeeCount = x.Employees.Count
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(data);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
    {
        var data = await _db.Roles.AsNoTracking()
            .OrderBy(x => x.Level)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DisplayName,
                x.Description,
                x.Level,
                x.IsSystem,
                userCount = x.UserRoles.Count,
                permissionCount = x.Permissions.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(data);
    }
}
