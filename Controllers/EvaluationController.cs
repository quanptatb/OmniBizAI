using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;

namespace OmniBizAI.Controllers;

[Authorize]
public class EvaluationController(EvaluationService evalService) : Controller
{
    public async Task<IActionResult> Index(string? periodId)
    {
        var vm = await evalService.GetListAsync(periodId);
        return View(vm);
    }
}
