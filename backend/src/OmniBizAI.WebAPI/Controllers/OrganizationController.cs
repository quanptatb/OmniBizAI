using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet("departments")]
    public async Task<ActionResult<ApiResponse<PagedResult<DepartmentDto>>>> GetDepartments([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<DepartmentDto>>.Ok(await _organizationService.GetDepartmentsAsync(request, cancellationToken)));
    }

    [HttpPost("departments")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _organizationService.CreateDepartmentAsync(request, cancellationToken);
        return Created($"/api/v1/departments/{created.Id}", ApiResponse<DepartmentDto>.Ok(created, "Department created"));
    }

    [HttpGet("employees")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> GetEmployees([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<EmployeeDto>>.Ok(await _organizationService.GetEmployeesAsync(request, cancellationToken)));
    }

    [HttpPost("employees")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var created = await _organizationService.CreateEmployeeAsync(request, cancellationToken);
        return Created($"/api/v1/employees/{created.Id}", ApiResponse<EmployeeDto>.Ok(created, "Employee created"));
    }
}
