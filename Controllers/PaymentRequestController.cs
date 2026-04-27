using Microsoft.AspNetCore.Mvc;
using OmniBizAI.ViewModels; // Thêm dòng này để Controller thấy được ViewModel

namespace OmniBizAI.Controllers
{
    public class PaymentRequestController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Khởi tạo model với các giá trị mặc định cho các thuộc tính required
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PaymentRequestViewModel model, string action)
        {
            // 1. Nếu Invalid (lỗi nhập liệu), trả về view kèm lỗi ModelState
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // 2. Nếu Valid, xử lý theo nút bấm
            if (action == "submit")
            {
                // Quy trình: Chạy AI Risk (đã có kết quả ở UI) -> Resolve Workflow -> Gửi duyệt
                TempData["SuccessMessage"] = "Đề nghị thanh toán đã được gửi phê duyệt!";
            }
            else
            {
                // Quy trình: Lưu vào bảng PaymentRequests với trạng thái Draft
                TempData["SuccessMessage"] = "Đã lưu bản nháp thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}