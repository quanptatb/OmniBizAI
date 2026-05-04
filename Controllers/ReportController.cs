using Microsoft.AspNetCore.Mvc;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            var model = new ReportIndexViewModel
            {
                CurrentRole = "Admin", // hoặc lấy từ session/login
                CanExport = true,      // hoặc check quyền

                ReportTypes =
                [
                    new("sales", "Báo cáo doanh thu"),
                    new("inventory", "Báo cáo tồn kho"),
                    new("finance", "Báo cáo tài chính")
                ],

                Periods =
                [
                    new("today", "Hôm nay"),
                    new("week", "Tuần này"),
                    new("month", "Tháng này"),
                    new("year", "Năm nay")
                ],

                Departments =
                [
                    new("all", "Tất cả"),
                    new("sales", "Kinh doanh"),
                    new("hr", "Nhân sự"),
                    new("it", "CNTT")
                ]
            };

            return View(model);
        }

        // ── API Preview (để JS gọi) ─────────────────────────────
        [HttpPost]
        public IActionResult Preview([FromBody] ReportFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.First().ErrorMessage
                    );
                return Json(new { success = false, message = "Vui lòng kiểm tra lại thông tin.", errors });
            }

            // Fake data demo
            var titleType = string.IsNullOrEmpty(filter.ReportType) ? "Báo cáo mẫu" : $"Báo cáo {filter.ReportType}";

            var data = new ReportPreviewData
            {
                Title = titleType,

                Summary =
                [
                    new("Doanh thu", "120M", "+10%", "up", "green"),
                    new("Chi phí", "80M", "-5%", "down", "red"),
                    new("Lợi nhuận", "40M", "+15%", "up", "blue")
                ],

                Rows =
                [
                    new() { Stt = 1, MaSo = "R001", TenMuc = "Sản phẩm A", GiaTri = "20M", TrangThai = "Hoàn thành", NgayTao = "2026-04-01" },
                    new() { Stt = 2, MaSo = "R002", TenMuc = "Sản phẩm B", GiaTri = "15M", TrangThai = "Chờ duyệt", NgayTao = "2026-04-02" },
                    new() { Stt = 3, MaSo = "R003", TenMuc = "Sản phẩm C", GiaTri = "5M", TrangThai = "Đã hủy", NgayTao = "2026-04-03" }
                ],

                AiSummary = "Doanh thu tăng trưởng tốt, chi phí giảm nhẹ, lợi nhuận cải thiện rõ rệt.",

                ChartLabels = ["T1", "T2", "T3", "T4"],
                ChartValues = [10, 20, 15, 30]
            };

            return Json(new
            {
                success = true,
                data
            });
        }
    }
}