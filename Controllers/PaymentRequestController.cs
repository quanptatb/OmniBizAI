using Microsoft.AspNetCore.Mvc;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers
{
    public class PaymentRequestController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var model = new PaymentRequestViewModel
            {
                Title = "",
                Department = "",
                Category = "",
                Vendor = "",
                Budget = ""
            };

            model.LineItems.Add(new LineItemViewModel { Quantity = 1 });
            return View(model);
        }

        public IActionResult Detail(int id)
        {
            ViewData["Title"] = "Chi tiết Yêu cầu thanh toán";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PaymentRequestViewModel model, string action)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            if (action == "submit")
            {
                TempData["SuccessMessage"] = "Đề nghị thanh toán đã được gửi phê duyệt!";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã lưu bản nháp thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
