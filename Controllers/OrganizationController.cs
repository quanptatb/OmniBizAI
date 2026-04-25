using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "Admin,Director,Manager,HR")]
public class OrganizationController : Controller
{
    public IActionResult Employees() => View();

    public IActionResult Departments() => View();

    [Authorize(Roles = "Admin")]
    public IActionResult Roles() => View();

    public IActionResult Positions() => View();
}
