using Microsoft.AspNetCore.Mvc;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers
{
    public class KpiController : Controller
    {
        // ─────────────────────────────────────────────────────────────
        // SEED DATA (mock – replace with real DB/service later)
        // ─────────────────────────────────────────────────────────────
        private static readonly List<KpiViewModel> _kpis =
        [
            new KpiViewModel
            {
                Id = 1,
                Objective = "Tăng trưởng doanh thu Q2 2026",
                Description = "Đẩy mạnh doanh số bán hàng qua các kênh trực tuyến và offline.",
                OwnerName = "Nguyễn Lan Anh",
                OwnerAvatar = "NL",
                StartDate = new DateTime(2026, 4, 1),
                EndDate = new DateTime(2026, 6, 30),
                Status = KpiStatus.OnTrack,
                KeyResults =
                [
                    new KeyResultViewModel { Id = 1, Title = "Doanh thu tháng đạt 5 tỷ VNĐ", StartValue = 3.5, CurrentValue = 4.2, TargetValue = 5, Unit = "tỷ", Direction = KpiDirection.Higher },
                    new KeyResultViewModel { Id = 2, Title = "Số đơn hàng mới đạt 500 đơn/tháng", StartValue = 280, CurrentValue = 390, TargetValue = 500, Unit = "đơn", Direction = KpiDirection.Higher },
                    new KeyResultViewModel { Id = 3, Title = "Tỷ lệ chuyển đổi khách hàng đạt 8%", StartValue = 4, CurrentValue = 6.2, TargetValue = 8, Unit = "%", Direction = KpiDirection.Higher }
                ]
            },
            new KpiViewModel
            {
                Id = 2,
                Objective = "Cải thiện mức độ hài lòng khách hàng",
                Description = "Tối ưu quy trình chăm sóc khách hàng và rút ngắn thời gian phản hồi.",
                OwnerName = "Trần Minh Tuấn",
                OwnerAvatar = "TM",
                StartDate = new DateTime(2026, 4, 1),
                EndDate = new DateTime(2026, 6, 30),
                Status = KpiStatus.AtRisk,
                KeyResults =
                [
                    new KeyResultViewModel { Id = 4, Title = "Điểm NPS đạt >= 70", StartValue = 50, CurrentValue = 58, TargetValue = 70, Unit = "điểm", Direction = KpiDirection.Higher },
                    new KeyResultViewModel { Id = 5, Title = "Thời gian phản hồi trung bình < 2 giờ", StartValue = 8, CurrentValue = 5, TargetValue = 2, Unit = "giờ", Direction = KpiDirection.Lower },
                    new KeyResultViewModel { Id = 6, Title = "Tỷ lệ khiếu nại < 3%", StartValue = 9, CurrentValue = 6.5, TargetValue = 3, Unit = "%", Direction = KpiDirection.Lower }
                ]
            },
            new KpiViewModel
            {
                Id = 3,
                Objective = "Nâng cao chất lượng vận hành nội bộ",
                Description = "Tự động hoá quy trình và giảm thiểu lỗi vận hành.",
                OwnerName = "Lê Thị Hương",
                OwnerAvatar = "LT",
                StartDate = new DateTime(2026, 4, 1),
                EndDate = new DateTime(2026, 6, 30),
                Status = KpiStatus.Behind,
                KeyResults =
                [
                    new KeyResultViewModel { Id = 7, Title = "Tỷ lệ lỗi hệ thống < 1%", StartValue = 5, CurrentValue = 3.8, TargetValue = 1, Unit = "%", Direction = KpiDirection.Lower },
                    new KeyResultViewModel { Id = 8, Title = "Tự động hoá 80% báo cáo định kỳ", StartValue = 20, CurrentValue = 35, TargetValue = 80, Unit = "%", Direction = KpiDirection.Higher }
                ]
            }
        ];

        private static readonly List<CheckInViewModel> _checkIns =
        [
            new CheckInViewModel { Id = 1, KpiId = 1, KeyResultId = 1, KpiObjective = "Tăng trưởng doanh thu Q2 2026", KeyResultTitle = "Doanh thu tháng đạt 5 tỷ VNĐ", PreviousValue = 3.8, NewValue = 4.2, Unit = "tỷ", SubmittedBy = "Nguyễn Lan Anh", SubmittedAt = DateTime.Now.AddHours(-3), Status = CheckInStatus.Pending },
            new CheckInViewModel { Id = 2, KpiId = 1, KeyResultId = 2, KpiObjective = "Tăng trưởng doanh thu Q2 2026", KeyResultTitle = "Số đơn hàng mới đạt 500 đơn/tháng", PreviousValue = 310, NewValue = 390, Unit = "đơn", SubmittedBy = "Nguyễn Lan Anh", SubmittedAt = DateTime.Now.AddHours(-5), Status = CheckInStatus.Pending },
            new CheckInViewModel { Id = 3, KpiId = 2, KeyResultId = 4, KpiObjective = "Cải thiện mức độ hài lòng khách hàng", KeyResultTitle = "Điểm NPS đạt >= 70", PreviousValue = 54, NewValue = 58, Unit = "điểm", SubmittedBy = "Trần Minh Tuấn", SubmittedAt = DateTime.Now.AddHours(-1), Status = CheckInStatus.Pending }
        ];

        // ─────────────────────────────────────────────────────────────
        // ACTIONS
        // ─────────────────────────────────────────────────────────────

        // GET /Kpi – danh sách KPI của nhân viên / quản lý
        public IActionResult Index()
        {
            ViewData["Title"] = "KPI / OKR";
            // In production, filter by logged-in user role
            ViewData["UserRole"] = "Manager"; // TODO: lấy từ claim thực tế khi tích hợp auth
            return View(_kpis);
        }

        // GET /Kpi/Detail/{id}
        public IActionResult Detail(int id)
        {
            var kpi = _kpis.Find(k => k.Id == id);
            if (kpi is null) return NotFound();
            ViewData["Title"] = $"Chi tiết KPI – {kpi.Objective}";
            return View(kpi);
        }

        // GET /Kpi/CheckIn/{id}  – form check-in cho KPI
        public IActionResult CheckIn(int id)
        {
            var kpi = _kpis.Find(k => k.Id == id);
            if (kpi is null) return NotFound();
            ViewData["Title"] = "Check-in KPI";

            var form = new CheckInFormViewModel
            {
                KpiId = kpi.Id,
                KpiObjective = kpi.Objective,
                KeyResults = kpi.KeyResults
            };
            return View(form);
        }

        // POST /Kpi/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckIn(CheckInFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var kpi = _kpis.Find(k => k.Id == form.KpiId);
                if (kpi is not null) form.KeyResults = kpi.KeyResults;
                form.KpiObjective = kpi?.Objective ?? string.Empty;
                ViewData["Title"] = "Check-in KPI";
                return View(form);
            }

            // In production: save to DB
            TempData["SuccessMessage"] = "Check-in đã được gửi thành công. Đang chờ quản lý duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Kpi/Pending – manager xem danh sách check-in chờ duyệt
        public IActionResult Pending()
        {
            ViewData["Title"] = "Chờ duyệt Check-in";
            var vm = new PendingCheckInViewModel
            {
                PendingItems = _checkIns.FindAll(c => c.Status == CheckInStatus.Pending)
            };
            return View(vm);
        }

        // POST /Kpi/Approve/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            var item = _checkIns.Find(c => c.Id == id);
            if (item is not null)
            {
                item.Status = CheckInStatus.Approved;
                // Update KPI current value
                var kr = _kpis.SelectMany(k => k.KeyResults).FirstOrDefault(r => r.Id == item.KeyResultId);
                if (kr is not null) kr.CurrentValue = item.NewValue;
            }
            TempData["SuccessMessage"] = "Đã duyệt check-in thành công.";
            return RedirectToAction(nameof(Pending));
        }

        // POST /Kpi/Reject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string? managerNote)
        {
            var item = _checkIns.Find(c => c.Id == id);
            if (item is not null)
            {
                item.Status = CheckInStatus.Rejected;
                item.ManagerNote = managerNote;
                // current value is NOT updated on reject
            }
            TempData["SuccessMessage"] = "Đã từ chối check-in.";
            return RedirectToAction(nameof(Pending));
        }
    }
}
