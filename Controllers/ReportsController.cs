using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "Admin,Director,Manager,Accountant,HR")]
public class ReportsController : Controller
{
    public IActionResult Index() => View();
}
