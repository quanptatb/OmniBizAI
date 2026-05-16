using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class HrService(ApplicationDbContext db, ITenantContext tenant)
{
    // ── Employees ────────────────────────────────────────────────────────────
    public async Task<EmployeeListViewModel> GetEmployeesAsync(string? search, Guid? deptId)
    {
        var tid = tenant.TenantId;
        var q = from ep in db.EmployeeProfiles.Where(e => e.TenantId == tid && !e.IsDeleted)
                join u in db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted) on ep.UserId equals u.Id
                select new { ep, u };

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.u.FullName.Contains(search) || x.u.Email.Contains(search) || x.ep.EmployeeCode.Contains(search));
        if (deptId.HasValue)
            q = q.Where(x => x.u.OrganizationUnitId == deptId);

        var data = await q.OrderBy(x => x.u.FullName).ToListAsync();

        var deptIds = data.Where(x => x.u.OrganizationUnitId.HasValue).Select(x => x.u.OrganizationUnitId!.Value).Distinct().ToList();
        var depts = await db.OrganizationUnits.Where(o => deptIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);
        var contractsByProfile = await db.EmployeeContracts.Where(c => c.TenantId == tid && !c.IsDeleted)
            .GroupBy(c => c.EmployeeProfileId)
            .Select(g => new { ProfileId = g.Key, Latest = g.OrderByDescending(c => c.EffectiveFrom).FirstOrDefault() })
            .ToDictionaryAsync(x => x.ProfileId, x => x.Latest);

        var items = data.Select(x =>
        {
            depts.TryGetValue(x.u.OrganizationUnitId ?? Guid.Empty, out var deptName);
            contractsByProfile.TryGetValue(x.ep.Id, out var contract);
            return new EmployeeListItem
            {
                Id = x.ep.Id, UserId = x.u.Id, EmployeeCode = x.ep.EmployeeCode,
                FullName = x.u.FullName, Email = x.u.Email, JobTitle = x.u.JobTitle,
                Department = deptName, Status = x.u.Status.ToString(),
                StartDate = x.ep.StartDate,
                CurrentContract = contract != null ? $"{contract.ContractType} ({contract.ContractNo})" : null
            };
        }).ToList();

        return new EmployeeListViewModel
        {
            Items = items, TotalCount = items.Count, SearchTerm = search, DeptFilter = deptId,
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<EmployeeDetailViewModel?> GetEmployeeDetailAsync(Guid id)
    {
        var ep = await db.EmployeeProfiles
            .Include(e => e.User)
            .Include(e => e.Contracts.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenant.TenantId && !e.IsDeleted);
        if (ep?.User is null) return null;

        var dept = ep.User.OrganizationUnitId.HasValue
            ? await db.OrganizationUnits.FindAsync(ep.User.OrganizationUnitId.Value)
            : null;

        return new EmployeeDetailViewModel
        {
            Id = ep.Id, UserId = ep.UserId, EmployeeCode = ep.EmployeeCode,
            FullName = ep.User.FullName, Email = ep.User.Email, JobTitle = ep.User.JobTitle,
            Department = dept?.Name, Status = ep.User.Status.ToString(),
            DateOfBirth = ep.DateOfBirth, StartDate = ep.StartDate,
            Contracts = ep.Contracts.OrderByDescending(c => c.EffectiveFrom).Select(c => new EmployeeContractItem
            {
                Id = c.Id, ContractNo = c.ContractNo, ContractType = c.ContractType,
                EffectiveFrom = c.EffectiveFrom, EffectiveTo = c.EffectiveTo
            }).ToList()
        };
    }

    // ── Positions ────────────────────────────────────────────────────────────
    public async Task<PositionListViewModel> GetPositionsAsync()
    {
        var tid = tenant.TenantId;
        var items = await db.Positions.Where(p => p.TenantId == tid && !p.IsDeleted)
            .OrderBy(p => p.Level).ThenBy(p => p.Code)
            .Select(p => new PositionListItem
            {
                Id = p.Id, Code = p.Code, Name = p.Name, Level = p.Level,
                IsManagerial = p.IsManagerial,
                Department = p.OrganizationUnit != null ? p.OrganizationUnit.Name : null
            }).ToListAsync();

        return new PositionListViewModel { Items = items, TotalCount = items.Count };
    }

    public async Task<PositionCreateViewModel> GetPositionCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new PositionCreateViewModel
        {
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreatePositionAsync(PositionCreateViewModel vm)
    {
        var entity = new Position
        {
            TenantId = tenant.TenantId, Code = vm.Code, Name = vm.Name,
            Level = vm.Level, IsManagerial = vm.IsManagerial,
            OrganizationUnitId = vm.OrganizationUnitId,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Positions.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "Position", EntityId = entity.Id, NewValuesJson = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }
}
