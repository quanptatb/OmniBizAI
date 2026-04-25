using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize]
public class PerformanceController : Controller
{
    public IActionResult Periods() => View();

    public IActionResult Objectives() => View();

    public IActionResult Kpis() => View();

    public IActionResult CheckIns() => View();

    public IActionResult Evaluations() => View();
}
