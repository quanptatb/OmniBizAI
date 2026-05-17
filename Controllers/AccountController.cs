using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IEmailService _email;

    // Simple in-memory rate limit for forgot password (IP → last request time)
    private static readonly Dictionary<string, DateTimeOffset> _forgotRateLimit = new();
    private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(2);

    public AccountController(
        SignInManager<IdentityUser<Guid>> signInManager,
        UserManager<IdentityUser<Guid>> userManager,
        ILogger<AccountController> logger,
        IEmailService email)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _email = email;
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(vm.UserName, vm.Password, vm.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserName} logged in.", vm.UserName);

            // Update last login timestamp
            var user = await _userManager.FindByNameAsync(vm.UserName);
            if (user != null)
            {
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
            }

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {UserName} locked out.", vm.UserName);
            var lockedUser = await _userManager.FindByNameAsync(vm.UserName);
            var lockEnd = lockedUser?.LockoutEnd;
            var remaining = lockEnd.HasValue ? (int)Math.Ceiling((lockEnd.Value - DateTimeOffset.UtcNow).TotalMinutes) : 15;
            ModelState.AddModelError("", $"Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau {Math.Max(remaining, 1)} phút hoặc liên hệ quản trị viên.");
        }
        else if (result.IsNotAllowed)
        {
            ModelState.AddModelError("", "Tài khoản chưa được kích hoạt. Vui lòng liên hệ quản trị viên.");
        }
        else
        {
            var failUser = await _userManager.FindByNameAsync(vm.UserName);
            if (failUser != null)
            {
                var maxAttempts = _userManager.Options.Lockout.MaxFailedAccessAttempts;
                var failCount = await _userManager.GetAccessFailedCountAsync(failUser);
                var left = maxAttempts - failCount;
                if (left > 0 && left <= 3)
                    ModelState.AddModelError("", $"Sai mật khẩu. Còn {left} lần thử trước khi tài khoản bị khóa.");
                else
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
            else
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
        }

        return View(vm);
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {UserName} logged out.", userName);
        return RedirectToAction("Login");
    }

    // ── Forgot Password ───────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // Rate limiting by IP
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        lock (_forgotRateLimit)
        {
            if (_forgotRateLimit.TryGetValue(clientIp, out var lastReq) && DateTimeOffset.UtcNow - lastReq < _rateLimitWindow)
            {
                var waitSec = (int)(_rateLimitWindow - (DateTimeOffset.UtcNow - lastReq)).TotalSeconds;
                ModelState.AddModelError("", $"Vui lòng đợi {waitSec} giây trước khi gửi lại yêu cầu.");
                return View(vm);
            }
            _forgotRateLimit[clientIp] = DateTimeOffset.UtcNow;
        }

        // Lookup user by email or username
        var user = await _userManager.FindByEmailAsync(vm.Email) ?? await _userManager.FindByNameAsync(vm.Email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action("ResetPassword", "Account", new { email = user.Email, token }, Request.Scheme)!;

            _logger.LogInformation("Password reset token generated for {Email}.", vm.Email);

            // Send email (falls back to logging in demo mode)
            try
            {
                await _email.SendPasswordResetAsync(user.Email!, resetUrl, user.UserName ?? user.Email!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reset email to {Email}", user.Email);
            }

            // For demo: also expose via TempData
            TempData["ResetLink"] = resetUrl;
            TempData["ResetEmail"] = user.Email;
        }
        else
        {
            _logger.LogWarning("Password reset requested for non-existent account: {Email}", vm.Email);
        }

        // Always redirect to prevent email enumeration
        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // "Gửi lại" — resend reset email
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendResetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction("ForgotPassword");

        return await ForgotPassword(new ForgotPasswordViewModel { Email = email });
    }

    // ── Reset Password ────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.FindByEmailAsync(vm.Email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return RedirectToAction("ResetPasswordConfirmation");
        }

        var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                // Translate common Identity errors
                var msg = err.Code switch
                {
                    "InvalidToken" => "Liên kết đặt lại mật khẩu đã hết hạn hoặc không hợp lệ. Vui lòng yêu cầu lại.",
                    "PasswordTooShort" => $"Mật khẩu tối thiểu {_userManager.Options.Password.RequiredLength} ký tự.",
                    _ => err.Description
                };
                ModelState.AddModelError("", msg);
            }

            // If token expired, show a direct link to ForgotPassword
            if (result.Errors.Any(e => e.Code == "InvalidToken"))
            {
                ViewData["TokenExpired"] = true;
            }

            return View(vm);
        }

        // Unlock if locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        _logger.LogInformation("Password reset completed for {Email}.", vm.Email);
        return RedirectToAction("ResetPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    // ── Access Denied ─────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ── Admin: Reset User Password ────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AdminResetPassword(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        return View(new AdminResetPasswordViewModel
        {
            UserId = id,
            UserName = user.UserName ?? "",
            FullName = user.UserName ?? ""
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> AdminResetPassword(AdminResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.FindByIdAsync(vm.UserId.ToString());
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            return View(vm);
        }

        // Unlock if locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        _logger.LogInformation("Admin {Admin} reset password for user {User}.", User.Identity?.Name, user.UserName);
        TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu cho {user.UserName}.";
        return RedirectToAction("Index", "Users");
    }

    // ── Admin: Unlock User ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "TENANT_ADMIN,SYSTEM_ADMIN")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Admin {Admin} unlocked user {User}.", User.Identity?.Name, user.UserName);
        TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.UserName}.";
        return RedirectToAction("Index", "Users");
    }
}
