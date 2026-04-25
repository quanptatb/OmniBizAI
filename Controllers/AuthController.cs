using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

public class AuthController : Controller
{
    public IActionResult Login() => View();

    public IActionResult Register() => View();

    public IActionResult MyProfile() => View();

    public IActionResult AccessDenied() => View();
}
