using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize]
public class WorkflowController : Controller
{
    [Authorize(Roles = "Admin,Director,Manager")]
    public IActionResult Approvals() => View();

    [Authorize(Roles = "Admin,Director")]
    public IActionResult Templates() => View();

    [Authorize(Roles = "Admin,Director")]
    public IActionResult Audit() => View();
}
