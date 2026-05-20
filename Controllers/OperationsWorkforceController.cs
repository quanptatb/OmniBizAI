using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OperationsWorkforceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public OperationsWorkforceController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index()
    {
        var tid = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var employees = await _db.EmployeeProfiles
            .Include(e => e.User)
            .Where(e => e.TenantId == tid && !e.IsDeleted)
            .ToListAsync();

        var deptNames = await _db.OrganizationUnits.Where(x => x.TenantId == tid && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var leaveToday = await _db.LeaveRequests.CountAsync(x => x.TenantId == tid && !x.IsDeleted && x.Status == LeaveStatus.Approved && x.StartDate <= today && x.EndDate >= today);

        var kpiAssignments = await _db.KpiEmployeeAssignments.CountAsync(x => x.TenantId == tid && !x.IsDeleted);
        var kpiDepartments = await _db.KpiDepartmentAssignments.CountAsync(x => x.TenantId == tid && !x.IsDeleted);

        var contracts = await _db.EmployeeContracts.Where(x => x.TenantId == tid && !x.IsDeleted).ToListAsync();
        var expiring = contracts.Count(x => x.EffectiveTo.HasValue && x.EffectiveTo.Value <= today.AddDays(30));

        var roleAssignments = await _db.UserRoleAssignments.Where(x => x.TenantId == tid && !x.IsDeleted).ToListAsync();
        var roleByDept = roleAssignments.GroupBy(x => x.ScopeEntityId).Select(g => new OpsRoleScopeItem { Scope = g.Key?.ToString()[..8] ?? "Global", AssignmentCount = g.Count() }).Take(12).ToList();

        var shiftByDept = employees.Where(e => e.User?.OrganizationUnitId != null)
            .GroupBy(e => e.User!.OrganizationUnitId!.Value)
            .Select(g => new ShiftCoverageItem
            {
                Department = deptNames.TryGetValue(g.Key, out var d) ? d : g.Key.ToString()[..8],
                Shift1 = g.Count() / 3,
                Shift2 = g.Count() / 3,
                Shift3 = g.Count() - (g.Count() / 3 * 2),
                OvertimeCount = g.Count(x => x.User != null && x.User.Status == UserStatus.Active) > 5 ? 1 : 0
            }).OrderByDescending(x => x.Total).Take(10).ToList();

        var vm = new OperationsWorkforceDashboardViewModel
        {
            TotalEmployees = employees.Count,
            OnShiftToday = employees.Count - leaveToday,
            OnLeaveToday = leaveToday,
            ShiftCoverage = shiftByDept,
            PersonalKpiAssignments = kpiAssignments,
            DepartmentKpiAssignments = kpiDepartments,
            TeamKpiProxy = kpiAssignments,
            ExpiringCertificates = expiring,
            ActiveCertificates = contracts.Count,
            RoleScope = roleByDept
        };

        return View(vm);
    }
}
