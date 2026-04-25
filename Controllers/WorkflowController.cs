using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

public class WorkflowController : Controller
{
    public IActionResult Approvals() => View();

    public IActionResult Templates() => View();

    public IActionResult Audit() => View();
}
