using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.ViewModels
{
    /// <summary>
    /// ViewModel for the Login page.
    /// Handles user credentials and remember-me preference.
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        [MaxLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
