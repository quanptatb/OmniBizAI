using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models;

namespace OmniBizAI.Controllers
{
    public class ApprovalQueueController : Controller
    {
        // Simulated data – replace with real DB/service calls
        private static List<PurchaseRequest> GetSampleQueue() =>
        [
            new PurchaseRequest
            {
                Id = 1,
                Code = "PR-2024-001",
                CreatedBy = "Nguyễn Văn A",
                CreatedAt = new System.DateTime(2024, 4, 20, 9, 30, 0),
                TotalAmount = 45_000_000,
                AiRisk = RiskLevel.Low,
                Status = PrStatus.Pending,
                Items =
                [
                    new() { Name = "Laptop Dell XPS 15", Quantity = 2, UnitPrice = 18_000_000 },
                    new() { Name = "Chuột không dây Logitech MX", Quantity = 5, UnitPrice = 1_200_000 },
                    new() { Name = "Bàn phím cơ Keychron K2", Quantity = 4, UnitPrice = 1_350_000 }
                ],
                Workflow =
                [
                    new() { StepName = "Created",  Actor = "Nguyễn Văn A",  CompletedAt = new System.DateTime(2024,4,20,9,30,0),  IsCompleted = true },
                    new() { StepName = "Reviewed", Actor = "Trần Thị B",    CompletedAt = new System.DateTime(2024,4,21,10,0,0),  IsCompleted = true },
                    new() { StepName = "Approved", Actor = "—",             CompletedAt = null,                                   IsCompleted = false }
                ]
            },
            new PurchaseRequest
            {
                Id = 2,
                Code = "PR-2024-002",
                CreatedBy = "Lê Thị C",
                CreatedAt = new System.DateTime(2024, 4, 21, 14, 0, 0),
                TotalAmount = 128_500_000,
                AiRisk = RiskLevel.High,
                Status = PrStatus.Pending,
                Items =
                [
                    new() { Name = "Máy chủ Dell PowerEdge R740", Quantity = 1, UnitPrice = 120_000_000 },
                    new() { Name = "UPS APC 3000VA", Quantity = 1, UnitPrice = 8_500_000 }
                ],
                Workflow =
                [
                    new() { StepName = "Created",  Actor = "Lê Thị C",  CompletedAt = new System.DateTime(2024,4,21,14,0,0), IsCompleted = true },
                    new() { StepName = "Reviewed", Actor = "—",         CompletedAt = null,                                  IsCompleted = false },
                    new() { StepName = "Approved", Actor = "—",         CompletedAt = null,                                  IsCompleted = false }
                ]
            },
            new PurchaseRequest
            {
                Id = 3,
                Code = "PR-2024-003",
                CreatedBy = "Phạm Minh D",
                CreatedAt = new System.DateTime(2024, 4, 22, 8, 0, 0),
                TotalAmount = 22_750_000,
                AiRisk = RiskLevel.Medium,
                Status = PrStatus.NeedRevision,
                Items =
                [
                    new() { Name = "Màn hình LG 27\" 4K", Quantity = 3, UnitPrice = 7_000_000 },
                    new() { Name = "Hub USB-C 7 in 1",     Quantity = 5, UnitPrice = 350_000 }
                ],
                Workflow =
                [
                    new() { StepName = "Created",  Actor = "Phạm Minh D", CompletedAt = new System.DateTime(2024,4,22,8,0,0),   IsCompleted = true },
                    new() { StepName = "Reviewed", Actor = "Trần Thị B",  CompletedAt = new System.DateTime(2024,4,22,11,30,0), IsCompleted = true },
                    new() { StepName = "Approved", Actor = "—",           CompletedAt = null,                                   IsCompleted = false }
                ]
            }
        ];

        // GET: /Approval
        public IActionResult Index()
        {
            var queue = GetSampleQueue();
            return View(queue);
        }

        // GET: /Approval/Detail/5
        public IActionResult Detail(int id)
        {
            var pr = GetSampleQueue().Find(x => x.Id == id);
            if (pr == null) return NotFound();

            // Simulate role: in real app read from claims/session
            // For demo: user "admin" is approver
            ViewBag.IsApprover = true;
            return View(pr);
        }

        // POST: /Approval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            // TODO: update DB status
            TempData["Success"] = $"PR #{id} đã được duyệt thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Approval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            // TODO: update DB status + reason
            TempData["Success"] = $"PR #{id} đã bị từ chối.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Approval/RequestRevision
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestRevision(int id, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                TempData["Error"] = "Vui lòng nhập nội dung yêu cầu chỉnh sửa.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            // TODO: update DB status + note
            TempData["Success"] = $"Đã yêu cầu chỉnh sửa PR #{id}.";
            return RedirectToAction(nameof(Index));
        }
    }
}