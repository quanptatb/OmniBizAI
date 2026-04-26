using Microsoft.AspNetCore.Mvc;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers
{
    public class AccountController : Controller
    {
        // Tài khoản cứng để test
        private const string HardcodedEmail    = "Admin@gmail.com";
        private const string HardcodedPassword = "123456";

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            await Task.CompletedTask;

            // Kiểm tra tài khoản cứng (không phân biệt hoa/thường cho email)
            bool emailOk    = model.Email.Equals(HardcodedEmail, StringComparison.OrdinalIgnoreCase);
            bool passwordOk = model.Password == HardcodedPassword;

            if (emailOk && passwordOk)
            {
                // Đăng nhập thành công → vào Dashboard
                return LocalRedirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
            }

            // Sai tài khoản hoặc mật khẩu
            ModelState.AddModelError("LoginError", "Email hoặc mật khẩu không đúng. Vui lòng kiểm tra lại.");
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return RedirectToAction(nameof(Login));
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await Task.CompletedTask;
            return RedirectToAction(nameof(Login));
        }
    }
}