using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    public IActionResult Index() => View();
}
