using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly NotificationService _notif;

    public ProfileController(ApplicationDbContext db, ITenantContext tenant, UserManager<IdentityUser<Guid>> userManager, NotificationService notif)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
        _notif = notif;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _db.AppUsers
            .Include(u => u.OrganizationUnit)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == _tenant.UserId && u.TenantId == _tenant.TenantId);

        if (user == null) return NotFound();

        var identityUser = await _userManager.FindByNameAsync(User.Identity?.Name ?? "");
        var roles = identityUser != null ? (await _userManager.GetRolesAsync(identityUser)).ToList() : new List<string>();

        var totalNotif = await _db.NotificationDeliveries.CountAsync(d => d.UserId == _tenant.UserId && d.TenantId == _tenant.TenantId && !d.IsDeleted);
        var unread = await _notif.GetUnreadCountAsync();

        var vm = new ProfileViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            JobTitle = user.JobTitle,
            Department = user.OrganizationUnit?.Name,
            PhoneNumber = user.Profile?.PhoneNumber,
            AvatarUrl = user.Profile?.AvatarUrl,
            TimeZoneId = user.Profile?.TimeZoneId,
            Locale = user.Profile?.Locale,
            Roles = roles,
            LastLogin = identityUser?.LockoutEnd, // approximate
            TotalNotifications = totalNotif,
            UnreadNotifications = unread
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _db.AppUsers.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == _tenant.UserId && u.TenantId == _tenant.TenantId);
        if (user == null) return NotFound();

        var vm = new ProfileEditViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.Profile?.PhoneNumber,
            JobTitle = user.JobTitle,
            TimeZoneId = user.Profile?.TimeZoneId ?? "Asia/Ho_Chi_Minh",
            Locale = user.Profile?.Locale ?? "vi-VN"
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.AppUsers.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == _tenant.UserId && u.TenantId == _tenant.TenantId);
        if (user == null) return NotFound();

        var oldName = user.FullName;
        user.FullName = vm.FullName;
        user.JobTitle = vm.JobTitle;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (user.Profile == null)
        {
            user.Profile = new Models.Entities.UserProfile
            {
                TenantId = _tenant.TenantId,
                UserId = user.Id,
                PhoneNumber = vm.PhoneNumber,
                TimeZoneId = vm.TimeZoneId,
                Locale = vm.Locale,
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        else
        {
            user.Profile.PhoneNumber = vm.PhoneNumber;
            user.Profile.TimeZoneId = vm.TimeZoneId;
            user.Profile.Locale = vm.Locale;
            user.Profile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        _db.AuditLogs.Add(new Models.Entities.AuditLog
        {
            TenantId = _tenant.TenantId,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            Action = "UpdateProfile",
            EntityName = "AppUser",
            EntityId = user.Id,
            NewValuesJson = $"{{\"FullName\":\"{vm.FullName}\",\"Phone\":\"{vm.PhoneNumber}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        // Notify managers if name changed
        if (oldName != vm.FullName)
        {
            await _notif.SendToManagersAsync(
                $"📝 {_tenant.UserFullName} đã cập nhật hồ sơ",
                $"{_tenant.UserFullName} đã đổi tên từ \"{oldName}\" thành \"{vm.FullName}\".",
                "AppUser", user.Id);
        }

        TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var identityUser = await _userManager.FindByNameAsync(User.Identity?.Name ?? "");
        if (identityUser == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(identityUser, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            return View(vm);
        }

        _db.AuditLogs.Add(new Models.Entities.AuditLog
        {
            TenantId = _tenant.TenantId,
            UserId = _tenant.UserId,
            UserName = _tenant.UserFullName,
            Action = "ChangePassword",
            EntityName = "AppUser",
            EntityId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class NotificationsController : Controller
{
    private readonly NotificationService _notif;

    public NotificationsController(NotificationService notif)
    {
        _notif = notif;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _notif.GetMyNotificationsAsync(100);
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notif.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notif.MarkAllAsReadAsync();
        return Ok();
    }

    /// <summary>API endpoint for notification bell dropdown (AJAX).</summary>
    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var items = await _notif.GetMyNotificationsAsync(10);
        var unread = await _notif.GetUnreadCountAsync();
        return Ok(new { items, unreadCount = unread });
    }

    /// <summary>Unread count only (for polling badge).</summary>
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _notif.GetUnreadCountAsync();
        return Ok(new { count });
    }
}
