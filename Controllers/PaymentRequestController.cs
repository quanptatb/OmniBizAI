using Microsoft.AspNetCore.Mvc;

namespace OmniBizAI.Controllers
{
    public class PaymentRequestController : Controller
    {
        public IActionResult Detail(int id)
        {
            ViewData["Title"] = "Chi tiết Yêu cầu thanh toán";
            return View();
        }
    }
}
