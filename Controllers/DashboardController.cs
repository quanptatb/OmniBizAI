using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}