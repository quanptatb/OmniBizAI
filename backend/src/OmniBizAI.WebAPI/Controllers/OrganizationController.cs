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

    [HttpGet("departments/{id}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<DepartmentDto>.Ok(await _organizationService.GetDepartmentAsync(id, cancellationToken)));

    [HttpPut("departments/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<DepartmentDto>.Ok(await _organizationService.UpdateDepartmentAsync(id, request, cancellationToken)));

    [HttpDelete("departments/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteDepartmentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("departments/tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DepartmentDto>>>> GetDepartmentTree(CancellationToken cancellationToken) => Ok(ApiResponse<IReadOnlyCollection<DepartmentDto>>.Ok(await _organizationService.GetDepartmentTreeAsync(cancellationToken)));

    [HttpGet("departments/{id}/employees")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> GetDepartmentEmployees(Guid id, [FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<EmployeeDto>>.Ok(await _organizationService.GetDepartmentEmployeesAsync(id, request, cancellationToken)));

    [HttpGet("employees/{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDto>.Ok(await _organizationService.GetEmployeeAsync(id, cancellationToken)));

    [HttpPut("employees/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDto>.Ok(await _organizationService.UpdateEmployeeAsync(id, request, cancellationToken)));

    [HttpDelete("employees/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteEmployeeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("employees/{id}/status")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployeeStatus(Guid id, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<EmployeeDto>.Ok(await _organizationService.UpdateEmployeeStatusAsync(id, request, cancellationToken)));

    [HttpGet("positions")]
    public async Task<ActionResult<ApiResponse<PagedResult<PositionDto>>>> GetPositions([FromQuery] PagedRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PagedResult<PositionDto>>.Ok(await _organizationService.GetPositionsAsync(request, cancellationToken)));

    [HttpGet("positions/{id}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetPosition(Guid id, CancellationToken cancellationToken) => Ok(ApiResponse<PositionDto>.Ok(await _organizationService.GetPositionAsync(id, cancellationToken)));

    [HttpPost("positions")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> CreatePosition(CreatePositionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PositionDto>.Ok(await _organizationService.CreatePositionAsync(request, cancellationToken)));

    [HttpPut("positions/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> UpdatePosition(Guid id, UpdatePositionRequest request, CancellationToken cancellationToken) => Ok(ApiResponse<PositionDto>.Ok(await _organizationService.UpdatePositionAsync(id, request, cancellationToken)));

    [HttpDelete("positions/{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult> DeletePosition(Guid id, CancellationToken cancellationToken)
    {
        await _organizationService.DeletePositionAsync(id, cancellationToken);
        return NoContent();
    }

}
