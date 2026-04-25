using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }

    public List<DemoAccountViewModel> DemoAccounts { get; set; } = new();
}

public class DemoAccountViewModel
{
    public string RoleName { get; set; } = null!;

    public string RoleDisplayName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = null!;
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = null!;
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string? RoleName { get; set; }
    public string? DepartmentName { get; set; }

    public List<UserSessionViewModel> Sessions { get; set; } = new();
}

public class UserSessionViewModel
{
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }
}
