using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "EXECUTIVE,TENANT_ADMIN,SYSTEM_ADMIN,DEPARTMENT_MANAGER")]
public class CommandCenterController(CommandCenterService service) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Trung tâm điều hành";
        ViewData["Breadcrumb"] = "<i class='fa-solid fa-tower-broadcast'></i> <span>Trung tâm điều hành</span>";
        var vm = await service.GetDashboardAsync();
        return View(vm);
    }
}
