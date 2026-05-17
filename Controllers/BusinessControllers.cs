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

    // ── Index (Tree) ──────────────────────────────────────────────────────────
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

        var unitMap = allUnits.ToDictionary(u => u.Id, u => u.Name);

        OrgUnitTreeItem BuildTree(OrganizationUnit unit)
        {
            managers.TryGetValue(unit.ManagerUserId ?? Guid.Empty, out var managerName);
            employeeCounts.TryGetValue(unit.Id, out var empCount);
            unitMap.TryGetValue(unit.ParentId ?? Guid.Empty, out var parentName);

            return new OrgUnitTreeItem
            {
                Id = unit.Id,
                Code = unit.Code,
                Name = unit.Name,
                Level = unit.Level,
                ManagerUserId = unit.ManagerUserId,
                ManagerName = managerName,
                ParentName = parentName,
                IsActive = unit.IsActive,
                EmployeeCount = empCount,
                Children = allUnits
                    .Where(c => c.ParentId == unit.Id)
                    .OrderBy(c => c.Name)
                    .Select(BuildTree)
                    .ToList()
            };
        }

        var roots = allUnits.Where(o => o.ParentId == null).OrderBy(o => o.Name).Select(BuildTree).ToList();
        var totalEmp = employeeCounts.Values.Sum();

        return View(new OrganizationUnitListViewModel
        {
            Tree = roots,
            TotalActive = allUnits.Count(u => u.IsActive),
            TotalInactive = allUnits.Count(u => !u.IsActive),
            TotalEmployees = totalEmp
        });
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        var vm = await BuildFormAsync();
        return View(new OrgUnitCreateViewModel { ParentOptions = vm.ParentOptions, UserOptions = vm.UserOptions });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrgUnitCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await BuildFormAsync();
            vm.ParentOptions = form.ParentOptions;
            vm.UserOptions = form.UserOptions;
            return View(vm);
        }

        var tid = _tenant.TenantId;
        if (await _db.OrganizationUnits.AnyAsync(o => o.TenantId == tid && o.Code == vm.Code && !o.IsDeleted))
        {
            ModelState.AddModelError("Code", "Mã phòng ban đã tồn tại.");
            var form = await BuildFormAsync();
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

        var unitId = Guid.NewGuid();
        _db.OrganizationUnits.Add(new OrganizationUnit
        {
            Id = unitId,
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
            TenantId = tid, UserId = _tenant.UserId, UserName = _tenant.UserFullName,
            EntityType = "OrganizationUnit", EntityId = unitId, Action = "Create",
            NewValues = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Tạo phòng ban {vm.Name} thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var tid = _tenant.TenantId;
        var unit = await _db.OrganizationUnits.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tid && !o.IsDeleted);
        if (unit == null) return NotFound();

        var form = await BuildFormAsync(excludeId: id);
        return View(new OrgUnitEditViewModel
        {
            Id = unit.Id,
            Code = unit.Code,
            Name = unit.Name,
            ParentId = unit.ParentId,
            ManagerUserId = unit.ManagerUserId,
            IsActive = unit.IsActive,
            ParentOptions = form.ParentOptions,
            UserOptions = form.UserOptions
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OrgUnitEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await BuildFormAsync(excludeId: vm.Id);
            vm.ParentOptions = form.ParentOptions;
            vm.UserOptions = form.UserOptions;
            return View(vm);
        }

        var tid = _tenant.TenantId;
        var unit = await _db.OrganizationUnits.FirstOrDefaultAsync(o => o.Id == vm.Id && o.TenantId == tid && !o.IsDeleted);
        if (unit == null) return NotFound();

        // Check duplicate code
        if (await _db.OrganizationUnits.AnyAsync(o => o.TenantId == tid && o.Code == vm.Code && o.Id != vm.Id && !o.IsDeleted))
        {
            ModelState.AddModelError("Code", "Mã phòng ban đã tồn tại.");
            var form = await BuildFormAsync(excludeId: vm.Id);
            vm.ParentOptions = form.ParentOptions;
            vm.UserOptions = form.UserOptions;
            return View(vm);
        }

        var oldValues = $"{{\"Code\":\"{unit.Code}\",\"Name\":\"{unit.Name}\",\"IsActive\":{unit.IsActive.ToString().ToLower()}}}";

        unit.Code = vm.Code;
        unit.Name = vm.Name;
        unit.ParentId = vm.ParentId;
        unit.ManagerUserId = vm.ManagerUserId;
        unit.IsActive = vm.IsActive;
        unit.UpdatedAt = DateTimeOffset.UtcNow;

        // Recalculate level
        if (vm.ParentId.HasValue)
        {
            var parent = await _db.OrganizationUnits.FindAsync(vm.ParentId.Value);
            unit.Level = (parent?.Level ?? 0) + 1;
        }
        else unit.Level = 0;

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = _tenant.UserId, UserName = _tenant.UserFullName,
            EntityType = "OrganizationUnit", EntityId = unit.Id, Action = "Update",
            OldValues = oldValues,
            NewValues = $"{{\"Code\":\"{vm.Code}\",\"Name\":\"{vm.Name}\",\"IsActive\":{vm.IsActive.ToString().ToLower()}}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Cập nhật phòng ban {vm.Name} thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── Details ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var tid = _tenant.TenantId;
        var unit = await _db.OrganizationUnits
            .Include(o => o.Parent)
            .Include(o => o.ManagerUser)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tid && !o.IsDeleted);
        if (unit == null) return NotFound();

        // Employees in this department
        var employees = await _db.AppUsers
            .Where(u => u.OrganizationUnitId == id && u.TenantId == tid && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .Select(u => new OrgDetailEmployee
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email,
                JobTitle = u.JobTitle, Status = u.Status.ToString()
            }).ToListAsync();

        // Positions in this department
        var positions = await _db.Positions
            .Where(p => p.OrganizationUnitId == id && p.TenantId == tid && !p.IsDeleted)
            .OrderBy(p => p.Level).ThenBy(p => p.Name)
            .Select(p => new OrgDetailPosition
            {
                Id = p.Id, Code = p.Code, Name = p.Name,
                Level = p.Level, IsManagerial = p.IsManagerial
            }).ToListAsync();

        // Child units with their employee counts and managers
        var childUnits = await _db.OrganizationUnits
            .Include(c => c.ManagerUser)
            .Where(c => c.ParentId == id && c.TenantId == tid && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var childIds = childUnits.Select(c => c.Id).ToList();
        var childEmpCounts = await _db.AppUsers
            .Where(u => u.TenantId == tid && u.OrganizationUnitId.HasValue && childIds.Contains(u.OrganizationUnitId!.Value) && !u.IsDeleted)
            .GroupBy(u => u.OrganizationUnitId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DeptId, x => x.Count);

        var children = childUnits.Select(c =>
        {
            childEmpCounts.TryGetValue(c.Id, out var cnt);
            return new OrgDetailChild
            {
                Id = c.Id, Code = c.Code, Name = c.Name,
                IsActive = c.IsActive, EmployeeCount = cnt,
                ManagerName = c.ManagerUser?.FullName
            };
        }).ToList();

        var createdByName = unit.CreatedByUserId.HasValue
            ? (await _db.AppUsers.Where(u => u.Id == unit.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync()) ?? "—"
            : "Hệ thống";

        // For move employee dropdown
        ViewBag.AllDepartments = await _db.OrganizationUnits
            .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted && o.Id != id)
            .OrderBy(o => o.Name)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        return View(new OrgUnitDetailViewModel
        {
            Id = unit.Id, Code = unit.Code, Name = unit.Name,
            Level = unit.Level, ParentName = unit.Parent?.Name,
            ParentId = unit.ParentId,
            ManagerUserId = unit.ManagerUserId,
            ManagerName = unit.ManagerUser?.FullName,
            IsActive = unit.IsActive,
            EmployeeCount = employees.Count,
            ChildCount = children.Count,
            PositionCount = positions.Count,
            CreatedAt = unit.CreatedAt, UpdatedAt = unit.UpdatedAt,
            CreatedByName = createdByName,
            Employees = employees, Positions = positions, Children = children
        });
    }

    // ── Move Employee ─────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveEmployee(Guid employeeId, Guid fromDeptId, Guid toDeptId)
    {
        var tid = _tenant.TenantId;
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == employeeId && u.TenantId == tid && !u.IsDeleted);
        if (user == null) return NotFound();

        var toDept = await _db.OrganizationUnits.FirstOrDefaultAsync(o => o.Id == toDeptId && o.TenantId == tid && !o.IsDeleted);
        if (toDept == null) return NotFound();

        var oldDeptName = user.OrganizationUnitId.HasValue
            ? (await _db.OrganizationUnits.Where(o => o.Id == user.OrganizationUnitId.Value).Select(o => o.Name).FirstOrDefaultAsync()) ?? "—"
            : "—";

        user.OrganizationUnitId = toDeptId;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = _tenant.UserId, UserName = _tenant.UserFullName,
            EntityType = "AppUser", EntityId = employeeId, Action = "MoveDepartment",
            OldValues = $"{{\"Department\":\"{oldDeptName}\"}}",
            NewValues = $"{{\"Department\":\"{toDept.Name}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã chuyển {user.FullName} sang {toDept.Name}.";
        return RedirectToAction(nameof(Details), new { id = fromDeptId });
    }

    // ── Toggle Active ─────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var unit = await _db.OrganizationUnits.FindAsync(id);
        if (unit == null || unit.TenantId != _tenant.TenantId) return NotFound();

        unit.IsActive = !unit.IsActive;
        unit.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = unit.IsActive ? $"Đã kích hoạt {unit.Name}." : $"Đã ngừng hoạt động {unit.Name}.";
        return RedirectToAction(nameof(Index));
    }

    // ── Delete (soft) ─────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tid = _tenant.TenantId;
        var unit = await _db.OrganizationUnits.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tid && !o.IsDeleted);
        if (unit == null) return NotFound();

        // Check for children or employees
        var hasChildren = await _db.OrganizationUnits.AnyAsync(c => c.ParentId == id && !c.IsDeleted);
        var hasEmployees = await _db.AppUsers.AnyAsync(u => u.OrganizationUnitId == id && !u.IsDeleted);
        if (hasChildren || hasEmployees)
        {
            TempData["ErrorMessage"] = "Không thể xóa phòng ban đang có đơn vị con hoặc nhân viên.";
            return RedirectToAction(nameof(Index));
        }

        unit.IsDeleted = true;
        unit.IsActive = false;
        unit.UpdatedAt = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = _tenant.UserId, UserName = _tenant.UserFullName,
            EntityType = "OrganizationUnit", EntityId = id, Action = "Delete",
            OldValues = $"{{\"Code\":\"{unit.Code}\",\"Name\":\"{unit.Name}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã xóa phòng ban {unit.Name}.";
        return RedirectToAction(nameof(Index));
    }

    // Kept for backward compat
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(Guid id) => ToggleActive(id);

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task<(List<SelectOption> ParentOptions, List<SelectOption> UserOptions)> BuildFormAsync(Guid? excludeId = null)
    {
        var tid = _tenant.TenantId;
        var parentQuery = _db.OrganizationUnits
            .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted);
        if (excludeId.HasValue)
            parentQuery = parentQuery.Where(o => o.Id != excludeId.Value);

        var parents = await parentQuery
            .OrderBy(o => o.Level).ThenBy(o => o.Name)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

        var users = await _db.AppUsers
            .Where(u => u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();

        return (parents, users);
    }
}

[Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly IEmailService _email;

    public UsersController(ApplicationDbContext db, ITenantContext tenant, UserManager<IdentityUser<Guid>> userManager, IEmailService email)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
        _email = email;
    }

    // ── List ──────────────────────────────────────────────────────────────────
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

    // ── Create ────────────────────────────────────────────────────────────────
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
            PhoneNumber = vm.PhoneNumber,
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
        var appUser = new AppUser
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
        };
        _db.AppUsers.Add(appUser);

        // Create UserProfile
        if (!string.IsNullOrWhiteSpace(vm.PhoneNumber))
        {
            _db.Set<UserProfile>().Add(new UserProfile
            {
                TenantId = tid,
                UserId = userId,
                PhoneNumber = vm.PhoneNumber,
                TimeZoneId = "Asia/Ho_Chi_Minh",
                Locale = "vi-VN",
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Audit log
        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            EntityType = "AppUser",
            EntityId = userId,
            Action = "Create",
            NewValues = $"{{\"Email\":\"{vm.Email}\",\"Role\":\"{vm.Role}\",\"FullName\":\"{vm.FullName}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();

        // Send welcome email
        if (vm.SendWelcomeEmail)
        {
            try
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Scheme)!;
                await _email.SendWelcomeEmailAsync(vm.Email, vm.FullName, loginUrl, vm.Password);
            }
            catch { /* Non-critical, don't fail user creation */ }
        }

        TempData["SuccessMessage"] = $"Tạo người dùng {vm.FullName} thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── Details ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var tid = _tenant.TenantId;
        var user = await _db.AppUsers
            .Include(u => u.OrganizationUnit)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tid && !u.IsDeleted);
        if (user == null) return NotFound();

        var identityUser = await _userManager.FindByIdAsync(id.ToString());
        var roles = identityUser != null ? (await _userManager.GetRolesAsync(identityUser)).ToList() : new List<string>();
        var isLocked = identityUser != null && await _userManager.IsLockedOutAsync(identityUser);

        var createdByName = user.CreatedByUserId.HasValue
            ? (await _db.AppUsers.Where(u => u.Id == user.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync()) ?? "—"
            : "Hệ thống";

        var notifCount = await _db.NotificationDeliveries.CountAsync(d => d.UserId == id && d.TenantId == tid && !d.IsDeleted);

        var vm = new UserDetailViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.Profile?.PhoneNumber,
            JobTitle = user.JobTitle,
            Department = user.OrganizationUnit?.Name ?? "Chưa phân bổ",
            Status = user.Status.ToString(),
            Roles = string.Join(", ", roles),
            AvatarUrl = user.Profile?.AvatarUrl,
            TimeZoneId = user.Profile?.TimeZoneId,
            Locale = user.Profile?.Locale,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            CreatedByName = createdByName,
            TotalNotifications = notifCount,
            IsLocked = isLocked
        };
        return View(vm);
    }

    // ── Edit ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var tid = _tenant.TenantId;
        var user = await _db.AppUsers.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tid && !u.IsDeleted);
        if (user == null) return NotFound();

        var identityUser = await _userManager.FindByIdAsync(id.ToString());
        var roles = identityUser != null ? (await _userManager.GetRolesAsync(identityUser)).ToList() : new List<string>();

        var vm = await BuildEditFormAsync();
        vm.Id = user.Id;
        vm.FullName = user.FullName;
        vm.Email = user.Email;
        vm.PhoneNumber = user.Profile?.PhoneNumber;
        vm.JobTitle = user.JobTitle;
        vm.OrganizationUnitId = user.OrganizationUnitId;
        vm.Role = roles.FirstOrDefault() ?? "STAFF";
        vm.Status = user.Status.ToString();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await BuildEditFormAsync();
            vm.DepartmentOptions = form.DepartmentOptions;
            vm.RoleOptions = form.RoleOptions;
            vm.StatusOptions = form.StatusOptions;
            return View(vm);
        }

        var tid = _tenant.TenantId;
        var user = await _db.AppUsers.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == vm.Id && u.TenantId == tid && !u.IsDeleted);
        if (user == null) return NotFound();

        // Check duplicate email
        if (await _db.AppUsers.AnyAsync(u => u.TenantId == tid && u.Email == vm.Email && u.Id != vm.Id && !u.IsDeleted))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại.");
            var form = await BuildEditFormAsync();
            vm.DepartmentOptions = form.DepartmentOptions;
            vm.RoleOptions = form.RoleOptions;
            vm.StatusOptions = form.StatusOptions;
            return View(vm);
        }

        var oldValues = $"{{\"FullName\":\"{user.FullName}\",\"Email\":\"{user.Email}\",\"Status\":\"{user.Status}\"}}";

        user.FullName = vm.FullName;
        user.Email = vm.Email;
        user.JobTitle = vm.JobTitle;
        user.OrganizationUnitId = vm.OrganizationUnitId;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (Enum.TryParse<UserStatus>(vm.Status, out var newStatus))
            user.Status = newStatus;

        // Update profile phone
        if (user.Profile != null)
        {
            user.Profile.PhoneNumber = vm.PhoneNumber;
            user.Profile.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (!string.IsNullOrWhiteSpace(vm.PhoneNumber))
        {
            _db.Set<UserProfile>().Add(new UserProfile
            {
                TenantId = tid, UserId = user.Id, PhoneNumber = vm.PhoneNumber,
                TimeZoneId = "Asia/Ho_Chi_Minh", Locale = "vi-VN",
                CreatedByUserId = _tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Update Identity user email
        var identityUser = await _userManager.FindByIdAsync(vm.Id.ToString());
        if (identityUser != null)
        {
            identityUser.Email = vm.Email;
            identityUser.UserName = vm.Email;
            identityUser.NormalizedEmail = vm.Email.ToUpperInvariant();
            identityUser.NormalizedUserName = vm.Email.ToUpperInvariant();
            identityUser.PhoneNumber = vm.PhoneNumber;
            await _userManager.UpdateAsync(identityUser);

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(identityUser);
            if (!currentRoles.Contains(vm.Role))
            {
                await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                await _userManager.AddToRoleAsync(identityUser, vm.Role);
            }
        }

        // Audit
        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = _tenant.UserId, UserName = _tenant.UserFullName,
            EntityType = "AppUser", EntityId = user.Id, Action = "Update",
            OldValues = oldValues,
            NewValues = $"{{\"FullName\":\"{vm.FullName}\",\"Email\":\"{vm.Email}\",\"Status\":\"{vm.Status}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Cập nhật người dùng {vm.FullName} thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ── Delete (soft) ─────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tid = _tenant.TenantId;
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tid && !u.IsDeleted);
        if (user == null) return NotFound();

        user.IsDeleted = true;
        user.Status = UserStatus.Locked;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Lock Identity account
        var identityUser = await _userManager.FindByIdAsync(id.ToString());
        if (identityUser != null)
        {
            await _userManager.SetLockoutEndDateAsync(identityUser, DateTimeOffset.MaxValue);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = _tenant.UserId, UserName = _tenant.UserFullName,
            EntityType = "AppUser", EntityId = id, Action = "Delete",
            OldValues = $"{{\"FullName\":\"{user.FullName}\",\"Email\":\"{user.Email}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã xóa người dùng {user.FullName}.";
        return RedirectToAction(nameof(Index));
    }

    // ── Toggle Lock ───────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        var user = await _db.AppUsers.FindAsync(id);
        if (user == null || user.TenantId != _tenant.TenantId) return NotFound();

        user.Status = user.Status == UserStatus.Active ? UserStatus.Locked : UserStatus.Active;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Sync with Identity lockout
        var identityUser = await _userManager.FindByIdAsync(id.ToString());
        if (identityUser != null)
        {
            if (user.Status == UserStatus.Locked)
                await _userManager.SetLockoutEndDateAsync(identityUser, DateTimeOffset.MaxValue);
            else
            {
                await _userManager.SetLockoutEndDateAsync(identityUser, null);
                await _userManager.ResetAccessFailedCountAsync(identityUser);
            }
        }

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = user.Status == UserStatus.Locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task<UserCreateViewModel> BuildCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        return new UserCreateViewModel
        {
            DepartmentOptions = await _db.OrganizationUnits
                .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .OrderBy(o => o.Name)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
                .ToListAsync(),
            RoleOptions = BuildRoleOptions()
        };
    }

    private async Task<UserEditViewModel> BuildEditFormAsync()
    {
        var tid = _tenant.TenantId;
        return new UserEditViewModel
        {
            DepartmentOptions = await _db.OrganizationUnits
                .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .OrderBy(o => o.Name)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
                .ToListAsync(),
            RoleOptions = BuildRoleOptions(),
            StatusOptions = new List<SelectOption>
            {
                new() { Value = "Active", Text = "Hoạt động" },
                new() { Value = "Locked", Text = "Đã khóa" },
                new() { Value = "Inactive", Text = "Ngừng hoạt động" },
            }
        };
    }

    private static List<SelectOption> BuildRoleOptions() => new()
    {
        new() { Value = "STAFF", Text = "Nhân viên" },
        new() { Value = "DEPARTMENT_MANAGER", Text = "Trưởng bộ phận" },
        new() { Value = "EXECUTIVE", Text = "Ban lãnh đạo" },
        new() { Value = "ACCOUNTANT", Text = "Kế toán" },
        new() { Value = "AUDITOR", Text = "Kiểm soát" },
        new() { Value = "TENANT_ADMIN", Text = "Quản trị doanh nghiệp" },
    };
}


[Authorize(Roles = "ACCOUNTANT,TENANT_ADMIN,SYSTEM_ADMIN,EXECUTIVE,DEPARTMENT_MANAGER")]
public class FinanceController : Controller
{
    private readonly ProcurementService _svc;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public FinanceController(ProcurementService svc, NotificationService notif, ITenantContext tenant)
    {
        _svc = svc;
        _notif = notif;
        _tenant = tenant;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var vm = await _svc.GetDashboardAsync();
        return View(vm);
    }

    // ── Budgets ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Budgets([FromServices] ApplicationDbContext db)
    {
        var budgets = await db.Budgets
            .Include(b => b.Expenses.Where(e => !e.IsDeleted))
            .Include(b => b.OrganizationUnit)
            .Where(b => b.TenantId == _tenant.TenantId && !b.IsDeleted)
            .OrderByDescending(b => b.FiscalYear).ThenBy(b => b.Name)
            .ToListAsync();

        var items = budgets.Select(b => new BudgetListItem
        {
            Id = b.Id, Name = b.Name, Department = b.OrganizationUnit?.Name ?? "",
            TotalAmount = b.PlannedAmount, UsedAmount = b.Expenses.Sum(e => e.Amount),
            Status = b.Status.ToString(),
            PeriodStart = new DateOnly(b.FiscalYear, 1, 1), PeriodEnd = new DateOnly(b.FiscalYear, 12, 31)
        }).ToList();

        return View(new BudgetListViewModel { Items = items });
    }

    public async Task<IActionResult> BudgetCreate() => View(await _svc.GetBudgetCreateFormAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BudgetCreate(BudgetCreateViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Departments = (await _svc.GetBudgetCreateFormAsync()).Departments; return View(vm); }
        var id = await _svc.CreateBudgetAsync(vm);
        await _notif.SendToManagersAsync($"💰 {_tenant.UserFullName} tạo ngân sách mới", $"{_tenant.UserFullName} đã tạo ngân sách \"{vm.Name}\" ({vm.PlannedAmount:N0} VNĐ)", "Budget", id);
        TempData["SuccessMessage"] = "Tạo ngân sách thành công.";
        return RedirectToAction(nameof(BudgetDetails), new { id });
    }

    public async Task<IActionResult> BudgetDetails(Guid id)
    {
        var vm = await _svc.GetBudgetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> BudgetEdit(Guid id)
    {
        var vm = await _svc.GetBudgetEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BudgetEdit(BudgetEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (!await _svc.UpdateBudgetAsync(vm)) { TempData["ErrorMessage"] = "Không thể cập nhật ngân sách."; return RedirectToAction(nameof(BudgetDetails), new { id = vm.Id }); }
        await _notif.SendToManagersAsync($"📝 {_tenant.UserFullName} cập nhật ngân sách", $"{_tenant.UserFullName} đã cập nhật ngân sách \"{vm.Name}\".", "Budget", vm.Id);
        TempData["SuccessMessage"] = "Cập nhật ngân sách thành công.";
        return RedirectToAction(nameof(BudgetDetails), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BudgetClose(Guid id)
    {
        if (!await _svc.CloseBudgetAsync(id)) { TempData["ErrorMessage"] = "Không thể đóng ngân sách."; }
        else { await _notif.SendToManagersAsync($"🔒 {_tenant.UserFullName} đóng ngân sách", $"{_tenant.UserFullName} đã đóng một ngân sách.", "Budget", id); TempData["SuccessMessage"] = "Đã đóng ngân sách."; }
        return RedirectToAction(nameof(BudgetDetails), new { id });
    }

    // ── Payment Requests ─────────────────────────────────────────────────────
    public async Task<IActionResult> PaymentRequests(string? status)
    {
        var vm = await _svc.GetPaymentListAsync(status);
        return View(vm);
    }

    public async Task<IActionResult> PaymentCreate() => View(await _svc.GetPaymentCreateFormAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PaymentCreate(PaymentRequestCreateViewModel vm)
    {
        if (!ModelState.IsValid) { var form = await _svc.GetPaymentCreateFormAsync(); vm.Vendors = form.Vendors; vm.PurchaseOrders = form.PurchaseOrders; return View(vm); }
        var id = await _svc.CreatePaymentAsync(vm);
        await _notif.SendToManagersAsync($"💳 {_tenant.UserFullName} tạo đề nghị thanh toán", $"{_tenant.UserFullName} đã tạo đề nghị thanh toán {vm.TotalAmount:N0} VNĐ.", "PaymentRequest", id);
        TempData["SuccessMessage"] = "Tạo đề nghị thanh toán thành công.";
        return RedirectToAction(nameof(PaymentDetails), new { id });
    }

    public async Task<IActionResult> PaymentDetails(Guid id)
    {
        var vm = await _svc.GetPaymentDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PaymentSubmit(Guid id)
    {
        if (!await _svc.SubmitPaymentAsync(id)) { TempData["ErrorMessage"] = "Không thể gửi duyệt."; }
        else { await _notif.SendToManagersAsync($"📤 {_tenant.UserFullName} gửi duyệt thanh toán", $"{_tenant.UserFullName} đã gửi đề nghị thanh toán chờ phê duyệt.", "PaymentRequest", id); TempData["SuccessMessage"] = "Đã gửi duyệt thành công."; }
        return RedirectToAction(nameof(PaymentDetails), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PaymentApprove(Guid id)
    {
        if (!await _svc.ApprovePaymentAsync(id)) { TempData["ErrorMessage"] = "Không thể phê duyệt."; }
        else { await _notif.BroadcastAsync($"✅ {_tenant.UserFullName} phê duyệt thanh toán", $"{_tenant.UserFullName} đã phê duyệt một đề nghị thanh toán.", "PaymentRequest", id); TempData["SuccessMessage"] = "Đã phê duyệt thành công."; }
        return RedirectToAction(nameof(PaymentDetails), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PaymentReject(Guid id, string? reason)
    {
        if (!await _svc.RejectPaymentAsync(id, reason)) { TempData["ErrorMessage"] = "Không thể từ chối."; }
        else { await _notif.BroadcastAsync($"❌ {_tenant.UserFullName} từ chối thanh toán", $"{_tenant.UserFullName} đã từ chối đề nghị thanh toán. Lý do: {reason ?? "N/A"}", "PaymentRequest", id); TempData["SuccessMessage"] = "Đã từ chối."; }
        return RedirectToAction(nameof(PaymentDetails), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PaymentMarkPaid(Guid id)
    {
        if (!await _svc.MarkPaymentPaidAsync(id)) { TempData["ErrorMessage"] = "Không thể xác nhận thanh toán."; }
        else { await _notif.BroadcastAsync($"💵 {_tenant.UserFullName} xác nhận thanh toán", $"{_tenant.UserFullName} đã xác nhận hoàn thành thanh toán.", "PaymentRequest", id); TempData["SuccessMessage"] = "Đã xác nhận thanh toán thành công."; }
        return RedirectToAction(nameof(PaymentDetails), new { id });
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

    public ReportsController(ApplicationDbContext db, ITenantContext tenant) { _db = db; _tenant = tenant; }

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
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync();

        var tid = _tenant.TenantId;
        var requests = await _db.OperationRequests
            .Where(r => r.TenantId == tid && !r.IsDeleted &&
                r.CreatedAt.Date >= filter.FromDate.ToDateTime(TimeOnly.MinValue) &&
                r.CreatedAt.Date <= filter.ToDate.ToDateTime(TimeOnly.MaxValue))
            .Include(r => r.OrganizationUnit).ToListAsync();

        if (dept.HasValue) requests = requests.Where(r => r.OrganizationUnitId == dept.Value).ToList();

        var vm = new ReportSummaryViewModel
        {
            Filter = filter,
            TotalRequests = requests.Count,
            CompletedRequests = requests.Count(r => r.Status == OperationStatus.Completed),
            RejectedRequests = requests.Count(r => r.Status == OperationStatus.Rejected),
            PendingRequests = requests.Count(r => r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview),
            ByStatus = requests.GroupBy(r => r.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
            ByDepartment = requests.Where(r => r.OrganizationUnit != null).GroupBy(r => r.OrganizationUnit!.Name).Select(g => new DeptWorkloadItem { Dept = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToList(),
            MonthlyTrend = requests.GroupBy(r => r.CreatedAt.ToString("yyyy-MM")).OrderBy(g => g.Key).Select(g => new MonthlyTrendItem { Month = g.Key, Created = g.Count(), Completed = g.Count(r => r.Status == OperationStatus.Completed) }).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> Finance()
    {
        var tid = _tenant.TenantId; var year = DateTime.Today.Year;
        var budgets = await _db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.FiscalYear == year).ToListAsync();
        var expenses = await _db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == year).Include(e => e.Budget).ThenInclude(b => b!.OrganizationUnit).ToListAsync();
        var payments = await _db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted).ToListAsync();

        var vm = new FinanceReportViewModel
        {
            TotalBudget = budgets.Sum(b => b.PlannedAmount),
            TotalExpense = expenses.Sum(e => e.Amount),
            TotalPayments = payments.Sum(p => p.TotalAmount),
            PendingPaymentCount = payments.Count(p => p.Status == PaymentStatus.Submitted),
            PendingPaymentAmount = payments.Where(p => p.Status == PaymentStatus.Submitted).Sum(p => p.TotalAmount),
            TotalBudgetCount = budgets.Count,
            ActiveBudgetCount = budgets.Count(b => b.Status == BudgetStatus.Active),
            PaymentByStatus = payments.GroupBy(p => p.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
            ExpenseByDept = expenses.Where(e => e.Budget?.OrganizationUnit != null).GroupBy(e => e.Budget!.OrganizationUnit!.Name).Select(g => new DeptWorkloadItem { Dept = g.Key, Count = (int)g.Sum(e => e.Amount / 1000000) }).OrderByDescending(x => x.Count).ToList(),
            ExpenseTrend = expenses.GroupBy(e => e.ExpenseDate.ToString("yyyy-MM")).OrderBy(g => g.Key).Select(g => new MonthlyTrendItem { Month = g.Key, Created = (int)(g.Sum(e => e.Amount) / 1000000) }).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> Hr()
    {
        var tid = _tenant.TenantId;
        var users = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted).Include(u => u.OrganizationUnit).ToListAsync();
        var vm = new HrReportViewModel
        {
            TotalEmployees = users.Count,
            ActiveEmployees = users.Count(u => u.Status == UserStatus.Active),
            InactiveEmployees = users.Count(u => u.Status != UserStatus.Active),
            TotalDepartments = await _db.OrganizationUnits.CountAsync(o => o.TenantId == tid && o.IsActive && !o.IsDeleted),
            TotalPositions = await _db.Positions.CountAsync(p => p.TenantId == tid && !p.IsDeleted),
            PendingLeaves = await _db.LeaveRequests.CountAsync(l => l.TenantId == tid && !l.IsDeleted && l.Status == LeaveStatus.Submitted),
            ByDepartment = users.Where(u => u.OrganizationUnit != null && u.Status == UserStatus.Active).GroupBy(u => u.OrganizationUnit!.Name).Select(g => new DeptWorkloadItem { Dept = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ToList(),
            ByStatus = users.GroupBy(u => u.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> KpiOkr()
    {
        var tid = _tenant.TenantId;
        var okrs = await _db.OkrObjectives.Where(o => o.TenantId == tid && !o.IsDeleted).Include(o => o.KeyResults.Where(kr => !kr.IsDeleted)).ToListAsync();
        var kpis = await _db.KpiDefinitions.Where(k => k.TenantId == tid && !k.IsDeleted).ToListAsync();
        var avgProgress = okrs.Any() ? Math.Round((decimal)okrs.Average(o =>
            o.KeyResults.Any() ? (double)o.KeyResults.Average(kr => kr.TargetValue == 0 ? 0 : (double)(kr.CurrentValue / kr.TargetValue * 100)) : 0), 1) : 0;

        var vm = new KpiOkrReportViewModel
        {
            TotalOkr = okrs.Count, ActiveOkr = okrs.Count(o => o.Status == OkrStatus.Active),
            CompletedOkr = okrs.Count(o => o.Status == OkrStatus.Completed), AvgOkrProgress = avgProgress,
            TotalKpi = kpis.Count, ActiveKpi = kpis.Count(k => k.Status == KpiStatus.Active),
            PendingCheckIns = await _db.KpiCheckIns.CountAsync(c => c.TenantId == tid && !c.IsDeleted && c.ReviewStatus == CheckInReviewStatus.Pending),
            TotalEvaluations = await _db.EvaluationResults.CountAsync(e => e.TenantId == tid && !e.IsDeleted),
            OkrByStatus = okrs.GroupBy(o => o.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
            KpiByStatus = kpis.GroupBy(k => k.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList()
        };
        return View(vm);
    }

    // ── Inventory / Warehouse Report ──────────────────────────────────────────
    public async Task<IActionResult> Inventory()
    {
        var tid = _tenant.TenantId;
        var receipts = await _db.GoodsReceipts.Where(r => r.TenantId == tid && !r.IsDeleted).ToListAsync();
        var issues = await _db.GoodsIssues.Where(i => i.TenantId == tid && !i.IsDeleted).ToListAsync();
        var alerts = await _db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active);

        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5).AddDays(-(DateTime.Today.Day - 1)));
        var receiptsByMonth = receipts.Where(r => r.ReceiptDate >= sixMonthsAgo).GroupBy(r => r.ReceiptDate.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Count());
        var issuesByMonth = issues.Where(i => i.IssueDate >= sixMonthsAgo).GroupBy(i => i.IssueDate.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Count());
        var months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i).ToString("yyyy-MM")).ToList();
        var trend = months.Select(m => new MonthlyTrendItem { Month = m, Created = receiptsByMonth.GetValueOrDefault(m), Completed = issuesByMonth.GetValueOrDefault(m) }).ToList();

        var vm = new InventoryReportViewModel
        {
            TotalProducts = await _db.ProductServices.CountAsync(p => p.TenantId == tid && p.IsActive && !p.IsDeleted && p.Type == "Product"),
            ActiveAlertCount = alerts,
            TotalReceipts = receipts.Count, ConfirmedReceipts = receipts.Count(r => r.Status == GoodsReceiptStatus.Confirmed),
            TotalIssues = issues.Count, ConfirmedIssues = issues.Count(i => i.Status == GoodsIssueStatus.Confirmed),
            ReceiptIssueTrend = trend
        };
        return View(vm);
    }

    // ── Cash Flow Report ──────────────────────────────────────────────────────
    public async Task<IActionResult> CashFlow()
    {
        var tid = _tenant.TenantId;
        var txns = await _db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted).ToListAsync();
        var valid = txns.Where(t => t.Status != CashTransactionStatus.Voided && t.Status != CashTransactionStatus.Rejected).ToList();

        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5).AddDays(-(DateTime.Today.Day - 1)));
        var monthly = valid.Where(t => t.TransactionDate >= sixMonthsAgo).GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.TransactionType }).ToList();
        var mList = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();
        var trend = mList.Select(d => new CashMonthSummary
        {
            Month = d.ToString("yyyy-MM"),
            Income = monthly.Where(m => m.Key.Year == d.Year && m.Key.Month == d.Month && m.Key.TransactionType == "Income").Sum(m => m.Sum(t => t.Amount)),
            Expense = monthly.Where(m => m.Key.Year == d.Year && m.Key.Month == d.Month && m.Key.TransactionType == "Expense").Sum(m => m.Sum(t => t.Amount))
        }).ToList();

        var vm = new CashFlowReportViewModel
        {
            TotalIncome = valid.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
            TotalExpense = valid.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
            TotalTransactions = txns.Count, PendingApproval = txns.Count(t => t.Status == CashTransactionStatus.Recorded),
            ByCategory = valid.GroupBy(t => t.Category).Select(g => new StatusCountItem { Status = g.Key, Count = (int)(g.Sum(t => t.TransactionType == "Income" ? t.Amount : -t.Amount) / 1000000) }).OrderByDescending(x => Math.Abs(x.Count)).Take(10).ToList(),
            ByPaymentMethod = valid.Where(t => t.PaymentMethod != null).GroupBy(t => t.PaymentMethod!).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
            MonthlyTrend = trend
        };
        return View(vm);
    }

    // ── CRM Report ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Crm()
    {
        var tid = _tenant.TenantId;
        var customers = await _db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted).ToListAsync();
        var opps = await _db.SalesOpportunities.Include(o => o.Customer).Where(o => o.TenantId == tid && !o.IsDeleted).ToListAsync();
        var interactions = await _db.CrmInteractions.Where(i => i.TenantId == tid && !i.IsDeleted).ToListAsync();

        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-5).AddDays(-(DateTime.Today.Day - 1)));
        var interactionTrend = interactions.Where(i => DateOnly.FromDateTime(i.CreatedAt.DateTime) >= sixMonthsAgo)
            .GroupBy(i => i.CreatedAt.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Count());
        var months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i).ToString("yyyy-MM")).ToList();
        var trend = months.Select(m => new MonthlyTrendItem { Month = m, Created = interactionTrend.GetValueOrDefault(m) }).ToList();

        var vm = new CrmReportViewModel
        {
            TotalCustomers = customers.Count, ActiveCustomers = customers.Count(c => c.IsActive),
            TotalVendors = await _db.Vendors.CountAsync(v => v.TenantId == tid && !v.IsDeleted),
            TotalOpportunities = opps.Count, WonOpportunities = opps.Count(o => o.Stage == "ClosedWon"),
            LostOpportunities = opps.Count(o => o.Stage == "ClosedLost"),
            PipelineValue = opps.Where(o => o.Stage != "ClosedWon" && o.Stage != "ClosedLost").Sum(o => o.EstimatedValue),
            WonValue = opps.Where(o => o.Stage == "ClosedWon").Sum(o => o.EstimatedValue),
            TotalInteractions = interactions.Count,
            OpportunityByStage = opps.GroupBy(o => o.Stage).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
            TopCustomersByRevenue = opps.Where(o => o.Stage == "ClosedWon" && o.Customer != null).GroupBy(o => o.Customer!.Name)
                .Select(g => new DeptWorkloadItem { Dept = g.Key, Count = (int)(g.Sum(o => o.EstimatedValue) / 1000000) }).OrderByDescending(x => x.Count).Take(10).ToList(),
            InteractionTrend = trend
        };
        return View(vm);
    }

    // ── Enhanced Executive ────────────────────────────────────────────────────
    public async Task<IActionResult> Executive()
    {
        var tid = _tenant.TenantId;
        var requests = await _db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted).ToListAsync();
        var budgets = await _db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.FiscalYear == DateTime.Today.Year).ToListAsync();
        var expenses = await _db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == DateTime.Today.Year).ToListAsync();
        var users = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted).ToListAsync();
        var okrs = await _db.OkrObjectives.Where(o => o.TenantId == tid && !o.IsDeleted).Include(o => o.KeyResults.Where(kr => !kr.IsDeleted)).ToListAsync();
        var kpis = await _db.KpiDefinitions.Where(k => k.TenantId == tid && !k.IsDeleted).ToListAsync();
        var cashTxns = await _db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted && t.Status != CashTransactionStatus.Voided && t.Status != CashTransactionStatus.Rejected).ToListAsync();
        var opps = await _db.SalesOpportunities.Where(o => o.TenantId == tid && !o.IsDeleted).ToListAsync();
        var alerts = await _db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active);

        var vm = new ExecutiveReportViewModel
        {
            Operations = new ReportSummaryViewModel
            {
                TotalRequests = requests.Count, CompletedRequests = requests.Count(r => r.Status == OperationStatus.Completed),
                RejectedRequests = requests.Count(r => r.Status == OperationStatus.Rejected),
                PendingRequests = requests.Count(r => r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview)
            },
            Finance = new FinanceReportViewModel
            {
                TotalBudget = budgets.Sum(b => b.PlannedAmount), TotalExpense = expenses.Sum(e => e.Amount),
                PendingPaymentCount = await _db.PaymentRequests.CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Status == PaymentStatus.Submitted),
                PendingPaymentAmount = await _db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted && p.Status == PaymentStatus.Submitted).SumAsync(p => p.TotalAmount)
            },
            Hr = new HrReportViewModel
            {
                TotalEmployees = users.Count, ActiveEmployees = users.Count(u => u.Status == UserStatus.Active),
                TotalDepartments = await _db.OrganizationUnits.CountAsync(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
            },
            KpiOkr = new KpiOkrReportViewModel
            {
                TotalOkr = okrs.Count, ActiveOkr = okrs.Count(o => o.Status == OkrStatus.Active),
                CompletedOkr = okrs.Count(o => o.Status == OkrStatus.Completed),
                TotalKpi = kpis.Count, ActiveKpi = kpis.Count(k => k.Status == KpiStatus.Active),
                AvgOkrProgress = okrs.Any() ? Math.Round((decimal)okrs.Average(o => o.KeyResults.Any() ? (double)o.KeyResults.Average(kr => kr.TargetValue == 0 ? 0 : (double)(kr.CurrentValue / kr.TargetValue * 100)) : 0), 1) : 0
            },
            Inventory = new InventoryReportViewModel
            {
                TotalProducts = await _db.ProductServices.CountAsync(p => p.TenantId == tid && p.IsActive && !p.IsDeleted && p.Type == "Product"),
                ActiveAlertCount = alerts,
                TotalReceipts = await _db.GoodsReceipts.CountAsync(r => r.TenantId == tid && !r.IsDeleted),
                TotalIssues = await _db.GoodsIssues.CountAsync(i => i.TenantId == tid && !i.IsDeleted)
            },
            CashFlow = new CashFlowReportViewModel
            {
                TotalIncome = cashTxns.Where(t => t.TransactionType == "Income").Sum(t => t.Amount),
                TotalExpense = cashTxns.Where(t => t.TransactionType == "Expense").Sum(t => t.Amount),
                TotalTransactions = cashTxns.Count
            },
            TotalCustomers = await _db.Customers.CountAsync(c => c.TenantId == tid && c.IsActive && !c.IsDeleted),
            TotalVendors = await _db.Vendors.CountAsync(v => v.TenantId == tid && v.IsActive && !v.IsDeleted),
            TotalProducts = await _db.ProductServices.CountAsync(p => p.TenantId == tid && p.IsActive && !p.IsDeleted),
            TotalOpportunities = opps.Count,
            OpportunityPipelineValue = opps.Where(o => o.Stage != "ClosedWon" && o.Stage != "ClosedLost").Sum(o => o.EstimatedValue)
        };
        return View(vm);
    }

    // ── Export Operations ─────────────────────────────────────────────────────
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
                x.r.RequestNo, x.r.Title, x.r.Type, Status = x.r.Status.ToString(), Priority = x.r.Priority.ToString(),
                Department = o.Name, x.CreatedBy, x.r.CreatedAt, x.r.DueDate, x.r.TotalAmount
            }).ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Số yêu cầu,Tiêu đề,Loại,Trạng thái,Ưu tiên,Phòng ban,Người tạo,Ngày tạo,Hạn xử lý,Giá trị");
        foreach (var r in requests)
            csv.AppendLine($"\"{r.RequestNo}\",\"{r.Title}\",\"{r.Type}\",\"{r.Status}\",\"{r.Priority}\",\"{r.Department}\",\"{r.CreatedBy}\",\"{r.CreatedAt:dd/MM/yyyy}\",\"{r.DueDate}\",\"{r.TotalAmount}\"");

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoVanHanh_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
    }

    // ── Export Cash Flow ──────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportCashFlow(DateOnly? from, DateOnly? to)
    {
        var tid = _tenant.TenantId;
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(-3));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var txns = await _db.CashTransactions.Include(t => t.Customer).Include(t => t.Vendor).Include(t => t.OrganizationUnit).Include(t => t.RecordedByUser)
            .Where(t => t.TenantId == tid && !t.IsDeleted && t.TransactionDate >= fromDate && t.TransactionDate <= toDate).OrderBy(t => t.TransactionDate).ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Mã GD,Loại,Danh mục,Mô tả,Số tiền,Ngày,Phương thức,KH/NCC,Phòng ban,Trạng thái,Người ghi");
        foreach (var t in txns)
        {
            var partner = t.TransactionType == "Income" ? t.Customer?.Name : t.Vendor?.Name;
            csv.AppendLine($"\"{t.TransactionNo}\",\"{(t.TransactionType == "Income" ? "Thu" : "Chi")}\",\"{t.Category}\",\"{t.Description}\",\"{t.Amount}\",\"{t.TransactionDate:dd/MM/yyyy}\",\"{t.PaymentMethod}\",\"{partner}\",\"{t.OrganizationUnit?.Name}\",\"{t.Status}\",\"{t.RecordedByUser?.FullName}\"");
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoThuChi_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
    }

    // ── Export Finance ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportFinance()
    {
        var tid = _tenant.TenantId; var year = DateTime.Today.Year;
        var expenses = await _db.Expenses.Include(e => e.Budget).ThenInclude(b => b!.OrganizationUnit)
            .Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == year).OrderBy(e => e.ExpenseDate).ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,Mô tả,Số tiền,Ngày chi,Ngân sách,Phòng ban,Trạng thái");
        foreach (var e in expenses)
            csv.AppendLine($"\"{e.Id}\",\"{e.Description}\",\"{e.Amount}\",\"{e.ExpenseDate:dd/MM/yyyy}\",\"{e.Budget?.Name}\",\"{e.Budget?.OrganizationUnit?.Name}\",\"{e.Status}\"");
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoTaiChinh_{year}.csv");
    }

    // ── Export HR ─────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportHr()
    {
        var tid = _tenant.TenantId;
        var users = await _db.AppUsers.Include(u => u.OrganizationUnit)
            .Where(u => u.TenantId == tid && !u.IsDeleted).OrderBy(u => u.FullName).ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,Họ tên,Email,Phòng ban,Chức danh,Trạng thái,Ngày tạo");
        foreach (var u in users)
            csv.AppendLine($"\"{u.Id}\",\"{u.FullName}\",\"{u.Email}\",\"{u.OrganizationUnit?.Name}\",\"{u.JobTitle}\",\"{u.Status}\",\"{u.CreatedAt:dd/MM/yyyy}\"");
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoNhanSu_{DateTime.Today:yyyyMMdd}.csv");
    }

    // ── Export KPI/OKR ───────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportKpiOkr()
    {
        var tid = _tenant.TenantId;
        var okrs = await _db.OkrObjectives.Include(o => o.KeyResults.Where(kr => !kr.IsDeleted))
            .Where(o => o.TenantId == tid && !o.IsDeleted).OrderBy(o => o.ObjectiveName).ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Mục tiêu,Cấp độ,Chu kỳ,Trạng thái,Số KR,Tiến độ TB (%)");
        foreach (var o in okrs)
        {
            var avg = o.KeyResults.Any() ? (double)o.KeyResults.Average(kr => kr.TargetValue == 0 ? 0 : (double)(kr.CurrentValue / kr.TargetValue * 100)) : 0;
            csv.AppendLine($"\"{o.ObjectiveName}\",\"{o.Level}\",\"{o.Cycle}\",\"{o.Status}\",\"{o.KeyResults.Count}\",\"{avg:F1}\"");
        }
        csv.AppendLine();
        var kpis = await _db.KpiDefinitions.Include(k => k.OrganizationUnit)
            .Where(k => k.TenantId == tid && !k.IsDeleted).OrderBy(k => k.Name).ToListAsync();
        csv.AppendLine("Mã KPI,Tên KPI,Đơn vị,Loại đo,Phòng ban,Trạng thái");
        foreach (var k in kpis)
            csv.AppendLine($"\"{k.Code}\",\"{k.Name}\",\"{k.Unit}\",\"{k.MeasureType}\",\"{k.OrganizationUnit?.Name}\",\"{k.Status}\"");
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoKPI_OKR_{DateTime.Today:yyyyMMdd}.csv");
    }

    // ── Export Inventory ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportInventory()
    {
        var tid = _tenant.TenantId;
        var products = await _db.ProductServices.Include(p => p.ProductCategory)
            .Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted && p.Type == "Product").OrderBy(p => p.Code).ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();
        var receivedMap = await _db.GoodsReceiptLines
            .Where(l => productIds.Contains(l.ProductServiceId!.Value) && !l.IsDeleted && l.GoodsReceipt!.TenantId == tid && l.GoodsReceipt.Status == GoodsReceiptStatus.Confirmed)
            .GroupBy(l => l.ProductServiceId).Select(g => new { PId = g.Key, T = g.Sum(l => l.ReceivedQuantity - (l.RejectedQuantity ?? 0)) }).ToListAsync();
        var issuedMap = await _db.GoodsIssueLines
            .Where(l => productIds.Contains(l.ProductServiceId!.Value) && !l.IsDeleted && l.GoodsIssue!.TenantId == tid && l.GoodsIssue.Status == GoodsIssueStatus.Confirmed)
            .GroupBy(l => l.ProductServiceId).Select(g => new { PId = g.Key, T = g.Sum(l => l.IssuedQuantity) }).ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Mã SP,Tên SP,Danh mục,Tồn kho,Điểm ĐH,Mức an toàn,Tồn tối đa,Đơn giá,Giá trị tồn");
        foreach (var p in products)
        {
            var recv = receivedMap.FirstOrDefault(r => r.PId == p.Id)?.T ?? 0;
            var iss = issuedMap.FirstOrDefault(i => i.PId == p.Id)?.T ?? 0;
            var stock = recv - iss;
            csv.AppendLine($"\"{p.Code}\",\"{p.Name}\",\"{p.ProductCategory?.Name}\",\"{stock}\",\"{p.ReorderPoint}\",\"{p.SafetyStock}\",\"{p.MaxStock}\",\"{p.StandardPrice}\",\"{stock * (p.StandardPrice ?? 0)}\"");
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoTonKho_{DateTime.Today:yyyyMMdd}.csv");
    }

    // ── Export CRM ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportCrm()
    {
        var tid = _tenant.TenantId;
        // Customers
        var csv = new System.Text.StringBuilder();
        var customers = await _db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted).OrderBy(c => c.Code).ToListAsync();
        csv.AppendLine("=== KHÁCH HÀNG ===");
        csv.AppendLine("Mã KH,Tên,Mã thuế,Ngành,Trạng thái");
        foreach (var c in customers)
            csv.AppendLine($"\"{c.Code}\",\"{c.Name}\",\"{c.TaxCode}\",\"{c.Industry}\",\"{(c.IsActive ? "Hoạt động" : "Ngừng")}\"");

        // Opportunities
        csv.AppendLine();
        var opps = await _db.SalesOpportunities.Include(o => o.Customer).Where(o => o.TenantId == tid && !o.IsDeleted).OrderByDescending(o => o.CreatedAt).ToListAsync();
        csv.AppendLine("=== CƠ HỘI BÁN HÀNG ===");
        csv.AppendLine("Mã,Tiêu đề,Khách hàng,Giai đoạn,Giá trị,Xác suất (%),Nguồn,Ngày tạo");
        foreach (var o in opps)
            csv.AppendLine($"\"{o.Code}\",\"{o.Title}\",\"{o.Customer?.Name}\",\"{o.Stage}\",\"{o.EstimatedValue}\",\"{o.Probability}\",\"{o.Source}\",\"{o.CreatedAt:dd/MM/yyyy}\"");

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"BaoCaoCRM_{DateTime.Today:yyyyMMdd}.csv");
    }

    // ── Export Center ────────────────────────────────────────────────────────
    public IActionResult ExportCenter() => View();
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

    public async Task<IActionResult> Index(string? contextType, string? riskLevel, string? search, int page = 1)
    {
        var vm = await _service.GetFilteredListAsync(contextType, riskLevel, search, page);
        var quickActions = await _service.GetQuickActionsAsync();
        ViewBag.QuickActions = quickActions;
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    public async Task<IActionResult> Recommendations()
    {
        var vm = await _service.GenerateRecommendationsAsync();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportInsight(Guid id)
    {
        var vm = await _service.GetDetailAsync(id);
        if (vm == null) return NotFound();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# AI Phân tích - {vm.ContextType}");
        sb.AppendLine($"Ngày: {vm.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Mức rủi ro: {vm.RiskLevel}");
        sb.AppendLine($"Người hỏi: {vm.AskedByName ?? "N/A"}");
        sb.AppendLine();
        sb.AppendLine($"## Câu hỏi");
        sb.AppendLine(vm.Question);
        sb.AppendLine();
        sb.AppendLine($"## Phân tích");
        sb.AppendLine(vm.Summary);
        if (!string.IsNullOrEmpty(vm.Recommendation))
        {
            sb.AppendLine();
            sb.AppendLine($"## Đề xuất hành động");
            sb.AppendLine(vm.Recommendation);
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/markdown", $"AiInsight_{vm.ContextType}_{vm.CreatedAt:yyyyMMdd_HHmm}.md");
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        TempData["Success"] = "Đã xóa phân tích.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "EXECUTIVE,DEPARTMENT_MANAGER,TENANT_ADMIN,SYSTEM_ADMIN,STAFF")]
public class AnomalyAlertsController : Controller
{
    private readonly AnomalyDetectionService _anomaly;
    private readonly InventoryService _inventory;
    private readonly NotificationService _notif;

    public AnomalyAlertsController(AnomalyDetectionService anomaly, InventoryService inventory, NotificationService notif)
    {
        _anomaly = anomaly; _inventory = inventory; _notif = notif;
    }

    public async Task<IActionResult> Index(string? module, string? severity)
    {
        // Auto-refresh stock alerts
        await _inventory.GenerateStockAlertsAsync();
        var vm = await _anomaly.ScanAsync(module, severity);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshAlerts()
    {
        var newAlerts = await _inventory.GenerateStockAlertsAsync();
        if (newAlerts > 0)
            await _notif.SendToManagersAsync($"⚠️ {newAlerts} cảnh báo tồn kho mới", $"Hệ thống phát hiện {newAlerts} sản phẩm cần chú ý.", "StockAlert", null);
        TempData["SuccessMessage"] = newAlerts > 0 ? $"Phát hiện {newAlerts} cảnh báo mới." : "Không có cảnh báo mới.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcknowledgeAlert(Guid id)
    {
        await _inventory.AcknowledgeAlertAsync(id);
        TempData["SuccessMessage"] = "Đã ghi nhận cảnh báo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveAlert(Guid id)
    {
        await _inventory.ResolveAlertAsync(id);
        TempData["SuccessMessage"] = "Đã xử lý cảnh báo.";
        return RedirectToAction(nameof(Index));
    }
}
