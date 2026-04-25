using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

public class OrganizationController : Controller
{
    public IActionResult Employees() => View();

    public IActionResult Departments() => View();

    public IActionResult Roles() => View();

    public IActionResult Positions() => View();
}
