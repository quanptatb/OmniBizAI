using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.ViewModels;
using OmniBizAI.Services;

namespace OmniBizAI.Controllers;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AuthController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl);
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel
        {
            DemoAccounts = await GetDemoAccountsAsync()
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        model.DemoAccounts = await GetDemoAccountsAsync();
        if (!ModelState.IsValid) return View(model);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (!user.IsActive || user.IsDeleted)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa hoặc vô hiệu hóa.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Load roles from DB
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        foreach (var roleName in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        // Create user session
        var userSession = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserSessions.Add(userSession);
        
        user.LastLoginAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);

        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            return View(model);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            FullName = model.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Tạo tài khoản thành công.";
        return RedirectToAction(nameof(Register));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user != null)
        {
            // Simulate a token generation.
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.Id.ToString() + "|" + DateTime.UtcNow.Ticks));
            var resetLink = Url.Action("ResetPassword", "Auth", new { email = model.Email, token = token }, Request.Scheme);
            
            var body = $"<h1>Khôi phục mật khẩu</h1><p>Vui lòng click vào link sau để đặt lại mật khẩu của bạn: <br/><a href='{resetLink}'>Đặt lại mật khẩu</a></p>";
            await _emailService.SendEmailAsync(model.Email, "OmniBiz AI - Đặt lại mật khẩu", body);
        }

        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return BadRequest("Token không hợp lệ.");
        
        var model = new ResetPasswordViewModel { Email = email, Token = token };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Challenge();

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Employee!).ThenInclude(e => e.Department)
            .Include(u => u.UserSessions)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null) return Challenge();

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            RoleName = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "No Role",
            DepartmentName = user.Employee?.Department?.Name ?? "No Department",
            Sessions = user.UserSessions
                .OrderByDescending(s => s.StartedAt)
                .Take(5)
                .Select(s => new UserSessionViewModel
                {
                    UserAgent = s.UserAgent,
                    IpAddress = s.IpAddress,
                    StartedAt = s.StartedAt.ToLocalTime(),
                    EndedAt = s.EndedAt?.ToLocalTime(),
                    IsActive = s.IsActive,
                    IsCurrent = s.IpAddress == HttpContext.Connection.RemoteIpAddress?.ToString() && s.IsActive
                }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyProfile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Challenge();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Challenge();

        user.FullName = model.FullName;
        user.Phone = model.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
        return RedirectToAction(nameof(MyProfile));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        else
            return RedirectToAction("Index", "Home");
    }

    private async Task<List<DemoAccountViewModel>> GetDemoAccountsAsync()
    {
        var roleOrder = DemoIdentitySeeder.Accounts
            .Select((account, index) => new { account.RoleName, index })
            .ToDictionary(account => account.RoleName, account => account.index);

        var demoEmails = DemoIdentitySeeder.Accounts.Select(account => account.Email).ToList();

        var accounts = await _context.Users
            .AsNoTracking()
            .Where(user => demoEmails.Contains(user.Email) && user.IsActive && !user.IsDeleted)
            .SelectMany(
                user => user.UserRoles.Select(userRole => new DemoAccountViewModel
                {
                    RoleName = userRole.Role.Name,
                    RoleDisplayName = userRole.Role.DisplayName,
                    Email = user.Email,
                    FullName = user.FullName
                }))
            .ToListAsync();

        return accounts
            .Where(account => roleOrder.ContainsKey(account.RoleName))
            .OrderBy(account => roleOrder[account.RoleName])
            .ToList();
    }
}
