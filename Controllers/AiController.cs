using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize]
public class AiController : Controller
{
    public IActionResult Copilot() => View();

    public IActionResult History() => View();
}
