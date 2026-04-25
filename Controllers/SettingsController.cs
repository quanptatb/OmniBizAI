using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    public IActionResult Index() => View();
}
