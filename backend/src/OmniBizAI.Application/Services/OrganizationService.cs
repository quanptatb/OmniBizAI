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

    public async Task<DepartmentDto> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department not found");
        return MapDepartment(e);
    }
    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department not found");
        e.Name = request.Name; e.Code = request.Code; e.ParentDepartmentId = request.ParentDepartmentId; e.ManagerId = request.ManagerId; e.BudgetLimit = request.BudgetLimit;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return MapDepartment(e);
    }
    public async Task DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department not found");
        var hasEmps = _unitOfWork.Repository<Employee>().Query().Any(x => x.DepartmentId == id && !x.IsDeleted);
        if(hasEmps) throw new BusinessRuleException("Cannot delete department with active employees.");
        e.IsDeleted = true; e.IsActive = false; await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    public Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = _unitOfWork.Repository<Department>().Query().Where(x => !x.IsDeleted).ToList();
        return Task.FromResult<IReadOnlyCollection<DepartmentDto>>(all.Select(MapDepartment).ToList());
    }
    public Task<PagedResult<EmployeeDto>> GetDepartmentEmployeesAsync(Guid departmentId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Employee>().Query().Where(x => !x.IsDeleted && x.DepartmentId == departmentId);
        return Task.FromResult(PagedResult<EmployeeDto>.Create(q.Select(MapEmployee), request));
    }
    public async Task<EmployeeDto> GetEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return MapEmployee(e);
    }
    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.FullName = request.FullName; e.Phone = request.Phone; e.DepartmentId = request.DepartmentId; e.PositionId = request.PositionId; e.ManagerId = request.ManagerId;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return MapEmployee(e);
    }
    public async Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.IsDeleted = true; e.Status = "Terminated"; await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    public async Task<EmployeeDto> UpdateEmployeeStatusAsync(Guid id, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        e.Status = request.Status; await _unitOfWork.SaveChangesAsync(cancellationToken); return MapEmployee(e);
    }
    public Task<PagedResult<PositionDto>> GetPositionsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Position>().Query();
        return Task.FromResult(PagedResult<PositionDto>.Create(q.Select(x => new PositionDto(x.Id, x.Name, x.Level, x.DepartmentId, x.Description, x.IsActive)), request));
    }
    public async Task<PositionDto> GetPositionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Position>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new PositionDto(e.Id, e.Name, e.Level, e.DepartmentId, e.Description, e.IsActive);
    }
    public async Task<PositionDto> CreatePositionAsync(CreatePositionRequest request, CancellationToken cancellationToken = default)
    {
        var p = new Position { CompanyId = GetCompanyId(), Name = request.Name, Level = request.Level, DepartmentId = request.DepartmentId, Description = request.Description };
        await _unitOfWork.Repository<Position>().AddAsync(p, cancellationToken); await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PositionDto(p.Id, p.Name, p.Level, p.DepartmentId, p.Description, p.IsActive);
    }
    public async Task<PositionDto> UpdatePositionAsync(Guid id, UpdatePositionRequest request, CancellationToken cancellationToken = default)
    {
        var p = await _unitOfWork.Repository<Position>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        p.Name = request.Name; p.Level = request.Level; p.DepartmentId = request.DepartmentId; p.Description = request.Description; p.IsActive = request.IsActive;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PositionDto(p.Id, p.Name, p.Level, p.DepartmentId, p.Description, p.IsActive);
    }
    public async Task DeletePositionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _unitOfWork.Repository<Position>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Position>().Remove(p); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

}
