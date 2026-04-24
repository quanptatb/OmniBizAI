using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrganizationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Department>().Query().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.Name.Contains(request.Search) || x.Code.Contains(request.Search));
        }

        var result = PagedResult<DepartmentDto>.Create(
            query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(MapDepartment),
            request);

        return Task.FromResult(result);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new BusinessRuleException("Department name and code are required.");
        }

        var companyId = GetCompanyId();
        var exists = _unitOfWork.Repository<Department>().Query().Any(x => x.Code == request.Code && !x.IsDeleted);
        if (exists)
        {
            throw new BusinessRuleException("Department code already exists.");
        }

        var department = new Department
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            ParentDepartmentId = request.ParentDepartmentId,
            ManagerId = request.ManagerId,
            BudgetLimit = request.BudgetLimit
        };

        await _unitOfWork.Repository<Department>().AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDepartment(department);
    }

    public Task<PagedResult<EmployeeDto>> GetEmployeesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Employee>().Query().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => x.FullName.Contains(request.Search) || x.Email.Contains(request.Search) || x.EmployeeCode.Contains(request.Search));
        }

        var result = PagedResult<EmployeeDto>.Create(query.OrderBy(x => x.EmployeeCode).Select(MapEmployee), request);
        return Task.FromResult(result);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new BusinessRuleException("Employee full name and email are required.");
        }

        var exists = _unitOfWork.Repository<Employee>().Query().Any(x => x.Email == request.Email && !x.IsDeleted);
        if (exists)
        {
            throw new BusinessRuleException("Employee email already exists.");
        }

        var nextNumber = _unitOfWork.Repository<Employee>().Query().Count() + 1;
        var employee = new Employee
        {
            CompanyId = GetCompanyId(),
            EmployeeCode = $"EMP-{nextNumber:000}",
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId,
            JoinDate = request.JoinDate
        };

        await _unitOfWork.Repository<Employee>().AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapEmployee(employee);
    }

    private Guid GetCompanyId()
    {
        return _unitOfWork.Repository<Company>().Query().Select(x => x.Id).FirstOrDefault() is { } id && id != Guid.Empty
            ? id
            : throw new BusinessRuleException("Company seed data is missing.");
    }

    private static DepartmentDto MapDepartment(Department department)
    {
        return new DepartmentDto(department.Id, department.Name, department.Code, department.ParentDepartmentId, department.ManagerId, department.BudgetLimit, department.IsActive);
    }

    private static EmployeeDto MapEmployee(Employee employee)
    {
        return new EmployeeDto(employee.Id, employee.UserId, employee.EmployeeCode, employee.FullName, employee.Email, employee.DepartmentId, employee.PositionId, employee.ManagerId, employee.Status);
    }
}
