using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Services.Integrations;

namespace OmniBizAI.Controllers;

[Authorize(Roles = "Admin")]
public class IntegrationsController : Controller
{
    private readonly IConfigurationStatusService _configurationStatus;

    public IntegrationsController(IConfigurationStatusService configurationStatus)
    {
        _configurationStatus = configurationStatus;
    }

    public IActionResult Index()
    {
        return View(_configurationStatus.GetSnapshot());
    }
}
