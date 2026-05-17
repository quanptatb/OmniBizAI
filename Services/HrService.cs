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

    public async Task<PositionEditViewModel?> GetPositionEditFormAsync(Guid id)
    {
        var p = await db.Positions.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenant.TenantId && !p.IsDeleted);
        if (p is null) return null;
        return new PositionEditViewModel
        {
            Id = p.Id, Code = p.Code, Name = p.Name, Level = p.Level,
            IsManagerial = p.IsManagerial, OrganizationUnitId = p.OrganizationUnitId,
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tenant.TenantId && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<bool> UpdatePositionAsync(PositionEditViewModel vm)
    {
        var p = await db.Positions.FindAsync(vm.Id);
        if (p is null || p.TenantId != tenant.TenantId) return false;
        p.Code = vm.Code; p.Name = vm.Name; p.Level = vm.Level;
        p.IsManagerial = vm.IsManagerial; p.OrganizationUnitId = vm.OrganizationUnitId;
        p.UpdatedAt = DateTimeOffset.UtcNow; p.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "Position", EntityId = p.Id, NewValuesJson = $"{{\"Name\":\"{vm.Name}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeletePositionAsync(Guid id)
    {
        var p = await db.Positions.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId) return false;
        p.IsDeleted = true; p.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    // ── Employee Create/Edit ────────────────────────────────────────────────
    public async Task<EmployeeCreateViewModel> GetEmployeeCreateFormAsync()
    {
        return new EmployeeCreateViewModel
        {
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tenant.TenantId && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<Guid> CreateEmployeeAsync(EmployeeCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        // Create AppUser
        var user = new AppUser
        {
            TenantId = tid, FullName = vm.FullName, Email = vm.Email,
            JobTitle = vm.JobTitle, OrganizationUnitId = vm.OrganizationUnitId,
            Status = UserStatus.Active, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.AppUsers.Add(user);
        // Create EmployeeProfile
        var profile = new EmployeeProfile
        {
            TenantId = tid, UserId = user.Id, EmployeeCode = vm.EmployeeCode,
            DateOfBirth = vm.DateOfBirth, StartDate = vm.StartDate,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.EmployeeProfiles.Add(profile);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "Employee", EntityId = profile.Id, NewValuesJson = $"{{\"Name\":\"{vm.FullName}\",\"Code\":\"{vm.EmployeeCode}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return profile.Id;
    }

    public async Task<EmployeeEditViewModel?> GetEmployeeEditFormAsync(Guid profileId)
    {
        var ep = await db.EmployeeProfiles.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == profileId && e.TenantId == tenant.TenantId && !e.IsDeleted);
        if (ep?.User is null) return null;
        return new EmployeeEditViewModel
        {
            ProfileId = ep.Id, UserId = ep.UserId, EmployeeCode = ep.EmployeeCode,
            FullName = ep.User.FullName, JobTitle = ep.User.JobTitle,
            DateOfBirth = ep.DateOfBirth, StartDate = ep.StartDate,
            OrganizationUnitId = ep.User.OrganizationUnitId,
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tenant.TenantId && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<bool> UpdateEmployeeAsync(EmployeeEditViewModel vm)
    {
        var ep = await db.EmployeeProfiles.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == vm.ProfileId && e.TenantId == tenant.TenantId);
        if (ep?.User is null) return false;
        ep.User.FullName = vm.FullName; ep.User.JobTitle = vm.JobTitle;
        ep.User.OrganizationUnitId = vm.OrganizationUnitId;
        ep.DateOfBirth = vm.DateOfBirth; ep.StartDate = vm.StartDate;
        ep.UpdatedAt = DateTimeOffset.UtcNow; ep.UpdatedByUserId = tenant.UserId;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "Employee", EntityId = ep.Id, NewValuesJson = $"{{\"Name\":\"{vm.FullName}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeactivateEmployeeAsync(Guid profileId)
    {
        var ep = await db.EmployeeProfiles.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == profileId && e.TenantId == tenant.TenantId);
        if (ep?.User is null) return false;
        ep.User.Status = UserStatus.Inactive; ep.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Deactivate", EntityName = "Employee", EntityId = ep.Id, NewValuesJson = $"{{\"Name\":\"{ep.User.FullName}\",\"Status\":\"Inactive\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<AddContractViewModel?> GetAddContractFormAsync(Guid profileId)
    {
        var ep = await db.EmployeeProfiles.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == profileId && e.TenantId == tenant.TenantId && !e.IsDeleted);
        if (ep?.User is null) return null;
        return new AddContractViewModel { EmployeeProfileId = ep.Id, EmployeeName = ep.User.FullName };
    }

    public async Task<bool> AddContractAsync(AddContractViewModel vm)
    {
        var ep = await db.EmployeeProfiles.FindAsync(vm.EmployeeProfileId);
        if (ep is null || ep.TenantId != tenant.TenantId) return false;
        var contract = new EmployeeContract
        {
            TenantId = tenant.TenantId, EmployeeProfileId = vm.EmployeeProfileId,
            ContractNo = vm.ContractNo, ContractType = vm.ContractType,
            EffectiveFrom = vm.EffectiveFrom, EffectiveTo = vm.EffectiveTo,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.EmployeeContracts.Add(contract);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "AddContract", EntityName = "Employee", EntityId = vm.EmployeeProfileId, NewValuesJson = $"{{\"ContractNo\":\"{vm.ContractNo}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── Leave Management ────────────────────────────────────────────────────
    public async Task<LeaveListViewModel> GetLeaveListAsync(string? status)
    {
        var tid = tenant.TenantId;
        var q = db.LeaveRequests.Where(l => l.TenantId == tid && !l.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, out var st))
            q = q.Where(l => l.Status == st);

        var data = await q.OrderByDescending(l => l.CreatedAt)
            .Include(l => l.EmployeeProfile).ThenInclude(ep => ep!.User)
            .ToListAsync();

        var deptIds = data.Where(l => l.EmployeeProfile?.User?.OrganizationUnitId != null)
            .Select(l => l.EmployeeProfile!.User!.OrganizationUnitId!.Value).Distinct().ToList();
        var depts = await db.OrganizationUnits.Where(o => deptIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        var items = data.Select(l =>
        {
            depts.TryGetValue(l.EmployeeProfile?.User?.OrganizationUnitId ?? Guid.Empty, out var deptName);
            return new LeaveListItem
            {
                Id = l.Id, EmployeeName = l.EmployeeProfile?.User?.FullName ?? "", EmployeeCode = l.EmployeeProfile?.EmployeeCode ?? "",
                Department = deptName, LeaveType = l.LeaveType.ToString(), Status = l.Status.ToString(),
                StartDate = l.StartDate, EndDate = l.EndDate, TotalDays = l.TotalDays,
                Reason = l.Reason, CreatedAt = l.CreatedAt
            };
        }).ToList();

        return new LeaveListViewModel { Items = items, TotalCount = items.Count, StatusFilter = status };
    }

    public async Task<Guid> CreateLeaveAsync(LeaveCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(e => e.TenantId == tid && e.UserId == tenant.UserId && !e.IsDeleted);
        if (profile is null) throw new InvalidOperationException("Employee profile not found");

        Enum.TryParse<LeaveType>(vm.LeaveType, out var lt);
        var entity = new LeaveRequest
        {
            TenantId = tid, EmployeeProfileId = profile.Id,
            LeaveType = lt, Status = LeaveStatus.Submitted,
            StartDate = vm.StartDate, EndDate = vm.EndDate, Reason = vm.Reason,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.LeaveRequests.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "LeaveRequest", EntityId = entity.Id, NewValuesJson = $"{{\"Type\":\"{vm.LeaveType}\",\"Days\":\"{entity.TotalDays}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ApproveLeaveAsync(Guid id)
    {
        var l = await db.LeaveRequests.FindAsync(id);
        if (l is null || l.TenantId != tenant.TenantId || l.Status != LeaveStatus.Submitted) return false;
        l.Status = LeaveStatus.Approved; l.ApprovedByUserId = tenant.UserId; l.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Approve", EntityName = "LeaveRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Approved\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> RejectLeaveAsync(Guid id, string? reason)
    {
        var l = await db.LeaveRequests.FindAsync(id);
        if (l is null || l.TenantId != tenant.TenantId || l.Status != LeaveStatus.Submitted) return false;
        l.Status = LeaveStatus.Rejected; l.RejectReason = reason; l.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Reject", EntityName = "LeaveRequest", EntityId = id, NewValuesJson = $"{{\"Status\":\"Rejected\",\"Reason\":\"{reason}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    // ── HR Dashboard ────────────────────────────────────────────────────────
    public async Task<HrDashboardViewModel> GetDashboardAsync()
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var som = new DateOnly(today.Year, today.Month, 1);
        var in30 = today.AddDays(30);

        var allProfiles = await db.EmployeeProfiles.Include(e => e.User).Where(e => e.TenantId == tid && !e.IsDeleted).ToListAsync();
        var active = allProfiles.Count(e => e.User?.Status == UserStatus.Active);
        var inactive = allProfiles.Count(e => e.User?.Status == UserStatus.Inactive);
        var newMonth = allProfiles.Count(e => e.StartDate.HasValue && e.StartDate >= som);

        // Expiring contracts
        var expiringContracts = await db.EmployeeContracts
            .Include(c => c.EmployeeProfile).ThenInclude(ep => ep!.User)
            .Where(c => c.TenantId == tid && !c.IsDeleted && c.EffectiveTo.HasValue && c.EffectiveTo.Value >= today && c.EffectiveTo.Value <= in30)
            .OrderBy(c => c.EffectiveTo)
            .ToListAsync();

        var expiringItems = expiringContracts.Select(c => new ExpiringContractItem
        {
            ProfileId = c.EmployeeProfileId, EmployeeName = c.EmployeeProfile?.User?.FullName ?? "",
            ContractNo = c.ContractNo, ExpiryDate = c.EffectiveTo!.Value,
            DaysRemaining = c.EffectiveTo!.Value.DayNumber - today.DayNumber
        }).ToList();

        // On leave today
        var onLeave = await db.LeaveRequests.CountAsync(l => l.TenantId == tid && !l.IsDeleted && l.Status == LeaveStatus.Approved && l.StartDate <= today && l.EndDate >= today);
        var pendingLeaves = await db.LeaveRequests.CountAsync(l => l.TenantId == tid && !l.IsDeleted && l.Status == LeaveStatus.Submitted);

        // Department distribution
        var deptDist = await db.AppUsers
            .Where(u => u.TenantId == tid && u.Status == UserStatus.Active && u.OrganizationUnitId.HasValue && !u.IsDeleted)
            .GroupBy(u => u.OrganizationUnitId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync();
        var deptNames = await db.OrganizationUnits.Where(o => o.TenantId == tid && !o.IsDeleted).ToDictionaryAsync(o => o.Id, o => o.Name);
        var totalForPct = deptDist.Sum(d => d.Count);
        var deptItems = deptDist.Select(d =>
        {
            deptNames.TryGetValue(d.DeptId, out var name);
            return new DeptDistributionItem { DepartmentName = name ?? "?", Count = d.Count, Percent = totalForPct > 0 ? Math.Round((decimal)d.Count / totalForPct * 100, 1) : 0 };
        }).OrderByDescending(d => d.Count).ToList();

        // Recent employees
        var recent = allProfiles.Where(e => e.User?.Status == UserStatus.Active)
            .OrderByDescending(e => e.StartDate ?? DateOnly.MinValue).Take(5)
            .Select(e => new EmployeeListItem
            {
                Id = e.Id, UserId = e.UserId, EmployeeCode = e.EmployeeCode,
                FullName = e.User!.FullName, Email = e.User.Email, JobTitle = e.User.JobTitle,
                Status = e.User.Status.ToString(), StartDate = e.StartDate
            }).ToList();

        return new HrDashboardViewModel
        {
            TotalEmployees = allProfiles.Count, ActiveEmployees = active, InactiveEmployees = inactive,
            NewThisMonth = newMonth, ExpiringContracts = expiringContracts.Count,
            OnLeaveToday = onLeave, PendingLeaves = pendingLeaves,
            DeptDistribution = deptItems, RecentEmployees = recent, ExpiringContractList = expiringItems
        };
    }
}

