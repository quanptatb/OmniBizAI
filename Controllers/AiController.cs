using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

public class AiController : Controller
{
    public IActionResult Copilot() => View();
}
