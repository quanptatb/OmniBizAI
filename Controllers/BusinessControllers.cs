using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
public class OrganizationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public OrganizationController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index()
    {
        var tid = _tenant.TenantId;
        var allUnits = await _db.OrganizationUnits
            .Where(o => o.TenantId == tid && !o.IsDeleted)
            .ToListAsync();

        var managerIds = allUnits.Where(o => o.ManagerUserId.HasValue).Select(o => o.ManagerUserId!.Value).ToList();
        var managers = await _db.AppUsers
            .Where(u => managerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var employeeCounts = await _db.AppUsers
            .Where(u => u.TenantId == tid && u.Status == UserStatus.Active && u.OrganizationUnitId.HasValue && !u.IsDeleted)
            .GroupBy(u => u.OrganizationUnitId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DeptId, x => x.Count);

        OrgUnitTreeItem BuildTree(OrganizationUnit unit)
        {
            managers.TryGetValue(unit.ManagerUserId ?? Guid.Empty, out var managerName);
            employeeCounts.TryGetValue(unit.Id, out var empCount);

            return new OrgUnitTreeItem
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                Level = unit.Level,
                ManagerName = managerName,
                IsActive = unit.IsActive,
                EmployeeCount = empCount,
                Children = allUnits
                    .Where(c => c.ParentId == unit.Id)
                    .Select(BuildTree)
                    .ToList()
            };
        }

        var roots = allUnits.Where(o => o.ParentId == null).Select(BuildTree).ToList();
        return View(new OrganizationUnitListViewModel { Tree = roots });
    }

    public async Task<IActionResult> Create()
    {
        var vm = await BuildCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrgUnitCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await BuildCreateFormAsync();
            vm.ParentOptions = form.ParentOptions;
            vm.UserOptions = form.UserOptions;
            return View(vm);
        }

        var tid = _tenant.TenantId;
        if (await _db.OrganizationUnits.AnyAsync(o => o.TenantId == tid && o.Code == vm.Code && !o.IsDeleted))
        {
            ModelState.AddModelError("Code", "Mã phòng ban đã tồn tại trong doanh nghiệp.");
            var form = await BuildCreateFormAsync();
            vm.ParentOptions = form.ParentOptions;
            vm.UserOptions = form.UserOptions;
            return View(vm);
        }

        int level = 0;
        if (vm.ParentId.HasValue)
        {
            var parent = await _db.OrganizationUnits.FindAsync(vm.ParentId.Value);
            level = (parent?.Level ?? 0) + 1;
        }

        _db.OrganizationUnits.Add(new OrganizationUnit
        {
            TenantId = tid,
            Code = vm.Code,
            Name = vm.Name,
            ParentId = vm.ParentId,
            ManagerUserId = vm.ManagerUserId,
            Level = level,
            IsActive = true,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            EntityType = "OrganizationUnit",
            EntityId = Guid.NewGuid(),
            Action = "Create",
            NewValues = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Tạo phòng ban thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var unit = await _db.OrganizationUnits.FindAsync(id);
        if (unit == null || unit.TenantId != _tenant.TenantId) return NotFound();

        unit.IsActive = false;
        unit.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã ngừng hoạt động phòng ban.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<OrgUnitCreateViewModel> BuildCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        var parents = await _db.OrganizationUnits
            .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = $"{'│'} {o.Name}" })
            .ToListAsync();

        var users = await _db.AppUsers
            .Where(u => u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();

        return new OrgUnitCreateViewModel { ParentOptions = parents, UserOptions = users };
    }
}

[Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public UsersController(ApplicationDbContext db, ITenantContext tenant, UserManager<IdentityUser<Guid>> userManager)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var tid = _tenant.TenantId;
        var query = _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var s))
            query = query.Where(u => u.Status == s);

        var users = await query
            .OrderBy(u => u.FullName)
            .Join(_db.OrganizationUnits, u => u.OrganizationUnitId, o => o.Id, (u, o) => new { u, DeptName = o.Name })
            .ToListAsync();

        // Get roles for each user from Identity
        var identityUsers = await _userManager.Users.ToListAsync();
        var rolesByUser = new Dictionary<Guid, string>();
        foreach (var iu in identityUsers)
        {
            var roles = await _userManager.GetRolesAsync(iu);
            rolesByUser[iu.Id] = string.Join(", ", roles);
        }

        var items = users.Select(x => new UserListItem
        {
            Id = x.u.Id,
            FullName = x.u.FullName,
            Email = x.u.Email,
            JobTitle = x.u.JobTitle,
            Department = x.DeptName,
            Status = x.u.Status.ToString(),
            Roles = rolesByUser.TryGetValue(x.u.Id, out var r) ? r : "",
            CreatedAt = x.u.CreatedAt
        }).ToList();

        return View(new UserListViewModel
        {
            Items = items,
            TotalCount = items.Count,
            SearchTerm = search,
            StatusFilter = status
        });
    }

    public async Task<IActionResult> Create()
    {
        var vm = await BuildCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await BuildCreateFormAsync();
            vm.DepartmentOptions = form.DepartmentOptions;
            vm.RoleOptions = form.RoleOptions;
            return View(vm);
        }

        var tid = _tenant.TenantId;
        if (await _db.AppUsers.AnyAsync(u => u.TenantId == tid && u.Email == vm.Email && !u.IsDeleted))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại trong doanh nghiệp.");
            var form = await BuildCreateFormAsync();
            vm.DepartmentOptions = form.DepartmentOptions;
            vm.RoleOptions = form.RoleOptions;
            return View(vm);
        }

        var userId = Guid.NewGuid();

        // Create Identity user
        var identityUser = new IdentityUser<Guid>
        {
            Id = userId,
            UserName = vm.Email,
            Email = vm.Email,
            EmailConfirmed = true,
            NormalizedEmail = vm.Email.ToUpperInvariant(),
            NormalizedUserName = vm.Email.ToUpperInvariant()
        };
        var result = await _userManager.CreateAsync(identityUser, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            var form = await BuildCreateFormAsync();
            vm.DepartmentOptions = form.DepartmentOptions;
            vm.RoleOptions = form.RoleOptions;
            return View(vm);
        }

        await _userManager.AddToRoleAsync(identityUser, vm.Role);

        // Create AppUser
        _db.AppUsers.Add(new AppUser
        {
            Id = userId,
            TenantId = tid,
            FullName = vm.FullName,
            Email = vm.Email,
            JobTitle = vm.JobTitle,
            OrganizationUnitId = vm.OrganizationUnitId,
            Status = UserStatus.Active,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            EntityType = "AppUser",
            EntityId = userId,
            Action = "Create",
            NewValues = $"{{\"Email\":\"{vm.Email}\",\"Role\":\"{vm.Role}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Tạo người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null || user.TenantId != _tenant.TenantId) return NotFound();

        user.Status = user.Status == UserStatus.Active ? UserStatus.Locked : UserStatus.Active;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = user.Status == UserStatus.Locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserCreateViewModel> BuildCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        return new UserCreateViewModel
        {
            DepartmentOptions = await _db.OrganizationUnits
                .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
                .ToListAsync(),
            RoleOptions = new List<SelectOption>
            {
                new() { Value = "STAFF", Text = "Nhân viên" },
                new() { Value = "DEPARTMENT_MANAGER", Text = "Trưởng bộ phận" },
                new() { Value = "EXECUTIVE", Text = "Ban lãnh đạo" },
                new() { Value = "ACCOUNTANT", Text = "Kế toán" },
                new() { Value = "AUDITOR", Text = "Kiểm soát" },
                new() { Value = "TENANT_ADMIN", Text = "Quản trị doanh nghiệp" },
            }
        };
    }
}

[Authorize(Roles = "ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN,EXECUTIVE")]
public class FinanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public FinanceController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Budgets()
    {
        var budgets = await _db.Budgets
            .Include(b => b.Expenses)
            .Where(b => b.TenantId == _tenant.TenantId && !b.IsDeleted)
            .Join(_db.OrganizationUnits, b => b.OrganizationUnitId, o => o.Id, (b, o) => new BudgetListItem
            {
                Id = b.Id,
                Name = b.Name,
                Department = o.Name,
                TotalAmount = b.PlannedAmount,
                UsedAmount = b.Expenses.Sum(e => e.Amount),
                Status = b.Status.ToString(),
                PeriodStart = new DateOnly(b.FiscalYear, 1, 1),
                PeriodEnd = new DateOnly(b.FiscalYear, 12, 31)
            })
            .ToListAsync();

        return View(new BudgetListViewModel { Items = budgets });
    }

    public async Task<IActionResult> PaymentRequests()
    {
        var items = await _db.PaymentRequests
            .Where(p => p.TenantId == _tenant.TenantId && !p.IsDeleted)
            .Select(p => new PaymentRequestListItem
            {
                Id = p.Id,
                RequestNo = p.RequestNo,
                Title = p.RequestNo,
                TotalAmount = p.TotalAmount,
                Status = p.Status.ToString(),
                Department = "",
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return View(new PaymentRequestListViewModel { Items = items });
    }

    public async Task<IActionResult> BudgetCreate([FromServices] ProcurementService svc)
    {
        var vm = await svc.GetBudgetCreateFormAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BudgetCreate([FromServices] ProcurementService svc, BudgetCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await svc.GetBudgetCreateFormAsync();
            vm.Departments = form.Departments;
            return View(vm);
        }
        var id = await svc.CreateBudgetAsync(vm);
        TempData["SuccessMessage"] = "Tạo ngân sách thành công.";
        return RedirectToAction(nameof(BudgetDetails), new { id });
    }

    public async Task<IActionResult> BudgetDetails([FromServices] ProcurementService svc, Guid id)
    {
        var vm = await svc.GetBudgetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }
}

[Authorize(Roles = "EXECUTIVE,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
public class KpiController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public KpiController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? ownerType)
    {
        var query = _db.KpiDefinitions
            .Where(k => k.TenantId == _tenant.TenantId && !k.IsDeleted);

        if (!string.IsNullOrWhiteSpace(ownerType) && Enum.TryParse<KpiOwnerType>(ownerType, out var t))
            query = query.Where(k => k.OwnerType == t);

        var items = await query
            .OrderBy(k => k.OwnerType)
            .ThenBy(k => k.Code)
            .Select(k => new KpiListItem
            {
                Id = k.Id,
                Code = k.Code,
                Name = k.Name,
                Unit = k.Unit ?? "",
                OwnerType = k.OwnerType.ToString(),
                PeriodType = k.PeriodType.ToString(),
                TargetValue = 0
            })
            .ToListAsync();

        return View(new KpiListViewModel { Items = items, OwnerTypeFilter = ownerType });
    }
}

[Authorize(Roles = "EXECUTIVE,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public ReportsController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IActionResult> Dashboard(DateOnly? from, DateOnly? to, Guid? dept)
    {
        var filter = new ReportFilterViewModel
        {
            FromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            ToDate = to ?? DateOnly.FromDateTime(DateTime.Today),
            OrganizationUnitId = dept
        };

        filter.Departments = await _db.OrganizationUnits
            .Where(o => o.TenantId == _tenant.TenantId && o.IsActive && !o.IsDeleted)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        var tid = _tenant.TenantId;
        var requests = await _db.OperationRequests
            .Where(r => r.TenantId == tid && !r.IsDeleted &&
                r.CreatedAt.Date >= filter.FromDate.ToDateTime(TimeOnly.MinValue) &&
                r.CreatedAt.Date <= filter.ToDate.ToDateTime(TimeOnly.MaxValue))
            .ToListAsync();

        if (dept.HasValue)
            requests = requests.Where(r => r.OrganizationUnitId == dept.Value).ToList();

        var vm = new ReportSummaryViewModel
        {
            Filter = filter,
            TotalRequests = requests.Count,
            CompletedRequests = requests.Count(r => r.Status == OperationStatus.Completed),
            RejectedRequests = requests.Count(r => r.Status == OperationStatus.Rejected),
            PendingRequests = requests.Count(r => r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview),
            ByStatus = requests.GroupBy(r => r.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export(DateOnly? from, DateOnly? to)
    {
        var tid = _tenant.TenantId;
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);

        var requests = await _db.OperationRequests
            .Where(r => r.TenantId == tid && !r.IsDeleted &&
                r.CreatedAt.Date >= fromDate.ToDateTime(TimeOnly.MinValue) &&
                r.CreatedAt.Date <= toDate.ToDateTime(TimeOnly.MaxValue))
            .Join(_db.AppUsers, r => r.RequestedByUserId, u => u.Id, (r, u) => new { r, CreatedBy = u.FullName })
            .Join(_db.OrganizationUnits, x => x.r.OrganizationUnitId, o => o.Id, (x, o) => new
            {
                x.r.RequestNo,
                x.r.Title,
                x.r.Type,
                Status = x.r.Status.ToString(),
                Priority = x.r.Priority.ToString(),
                Department = o.Name,
                x.CreatedBy,
                x.r.CreatedAt,
                x.r.DueDate,
                x.r.TotalAmount
            })
            .ToListAsync();

        // Generate CSV
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Số yêu cầu,Tiêu đề,Loại,Trạng thái,Ưu tiên,Phòng ban,Người tạo,Ngày tạo,Hạn xử lý,Giá trị");
        foreach (var r in requests)
            csv.AppendLine($"\"{r.RequestNo}\",\"{r.Title}\",\"{r.Type}\",\"{r.Status}\",\"{r.Priority}\",\"{r.Department}\",\"{r.CreatedBy}\",\"{r.CreatedAt:dd/MM/yyyy}\",\"{r.DueDate}\",\"{r.TotalAmount}\"");

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoVanHanh_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
    }
}

[Authorize]
public class AuditController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public AuditController(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [Authorize(Roles = "AUDITOR,SYSTEM_ADMIN,TENANT_ADMIN")]
    public async Task<IActionResult> Index(string? entityType, string? action, string? user, int page = 1)
    {
        var tid = _tenant.TenantId;
        var query = _db.AuditLogs.Where(a => a.TenantId == tid);

        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(user)) query = query.Where(a => a.UserName.Contains(user));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * 50)
            .Take(50)
            .Select(a => new AuditLogListItem
            {
                Id = a.Id,
                UserName = a.UserName,
                EntityType = a.EntityType,
                Action = a.Action,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return View(new AuditLogListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            EntityTypeFilter = entityType,
            ActionFilter = action,
            UserFilter = user
        });
    }
}

[Authorize(Roles = "EXECUTIVE,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN,STAFF")]
public class AiInsightsController : Controller
{
    private readonly AiInsightService _service;

    public AiInsightsController(AiInsightService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var insights = await _service.GetListAsync();
        var quickActions = await _service.GetQuickActionsAsync();
        ViewBag.QuickActions = quickActions;
        return View(insights);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] AiInsightCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Câu hỏi không hợp lệ." });

        try
        {
            var result = await _service.AnalyzeAsync(vm);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Hệ thống AI tạm thời gặp sự cố. Vui lòng thử lại sau." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> QuickActions()
    {
        var actions = await _service.GetQuickActionsAsync();
        return Ok(actions);
    }
}

