using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Controllers;

[Authorize]
public class OperationsController : Controller
{
    private readonly OperationRequestService _service;
    private readonly OperationRequestQueryService _queries;
    private readonly OperationAttachmentService _attachments;
    private readonly NotificationService _notif;
    private readonly ITenantContext _tenant;

    public OperationsController(
        OperationRequestService service,
        OperationRequestQueryService queries,
        OperationAttachmentService attachments,
        NotificationService notif,
        ITenantContext tenant)
    {
        _service = service;
        _queries = queries;
        _attachments = attachments;
        _notif = notif;
        _tenant = tenant;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? priority, Guid? dept, int page = 1)
    {
        var vm = await _queries.GetListAsync(search, status, priority, dept, page);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _queries.GetDetailAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = OperationRoles.CanCreate)]
    public async Task<IActionResult> Create(string? type = null, Guid? templateId = null)
    {
        var vm = await _queries.GetCreateFormAsync(templateId);
        if (!string.IsNullOrWhiteSpace(type)) vm.Type = type;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanCreate)]
    public async Task<IActionResult> Create(OperationRequestCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _queries.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            vm.Products = form.Products;
            vm.Templates = form.Templates;
            return View(vm);
        }

        if (vm.DueDate.HasValue && vm.DueDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("DueDate", "Hạn xử lý không được nhỏ hơn ngày hôm nay.");
            var form = await _queries.GetCreateFormAsync();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            vm.Products = form.Products;
            vm.Templates = form.Templates;
            return View(vm);
        }

        var id = await _service.CreateAsync(vm);

        // Notify managers about new operation request
        await _notif.SendToManagersAsync(
            $"📋 {_tenant.UserFullName} tạo yêu cầu mới",
            $"{_tenant.UserFullName} đã tạo yêu cầu vận hành \"{vm.Title}\" (ưu tiên: {vm.Priority})",
            "OperationRequest", id);

        if (vm.Priority == PriorityLevel.Critical)
        {
            await _notif.SendToDepartmentAsync(
                $"🚩 Yêu cầu Critical mới",
                $"{_tenant.UserFullName} đã tạo yêu cầu Critical \"{vm.Title}\". Phòng ban cần ưu tiên tiếp nhận.",
                vm.OrganizationUnitId,
                "OperationRequest", id);
        }

        TempData["SuccessMessage"] = "Tạo yêu cầu thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> Templates(string? search)
    {
        var vm = await _queries.GetTemplatesAsync(search);
        return View(vm);
    }

    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> TemplateCreate()
    {
        var vm = await _queries.GetTemplateFormAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> TemplateCreate(OperationRequestTemplateEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _queries.GetTemplateFormAsync();
            vm.Departments = form?.Departments ?? new();
            return View(vm);
        }

        var result = await _service.CreateTemplateAsync(vm);
        if (!result.Success)
        {
            ModelState.AddModelError(nameof(vm.DefaultLinesJson), result.Message);
            var form = await _queries.GetTemplateFormAsync();
            vm.Departments = form?.Departments ?? new();
            return View(vm);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> TemplateEdit(Guid id)
    {
        var vm = await _queries.GetTemplateFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> TemplateEdit(OperationRequestTemplateEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _queries.GetTemplateFormAsync(vm.Id);
            vm.Departments = form?.Departments ?? new();
            return View(vm);
        }

        var result = await _service.UpdateTemplateAsync(vm);
        if (!result.Success)
        {
            ModelState.AddModelError(nameof(vm.DefaultLinesJson), result.Message);
            var form = await _queries.GetTemplateFormAsync(vm.Id);
            vm.Departments = form?.Departments ?? new();
            return View(vm);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Templates));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> TemplateDelete(Guid id)
    {
        var success = await _service.DeleteTemplateAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã xóa template."
            : "Không thể xóa template.";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = OperationRoles.CanCreate)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _queries.GetEditFormAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanCreate)]
    public async Task<IActionResult> Edit(OperationRequestEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await _queries.GetEditFormAsync(vm.Id);
            if (form is null) return NotFound();
            vm.Departments = form.Departments;
            vm.Customers = form.Customers;
            return View(vm);
        }

        var success = await _service.UpdateAsync(vm);
        if (!success)
        {
            TempData["ErrorMessage"] = "Không thể cập nhật yêu cầu này.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        await _notif.SendToManagersAsync(
            $"📝 {_tenant.UserFullName} cập nhật yêu cầu",
            $"{_tenant.UserFullName} đã cập nhật yêu cầu vận hành \"{vm.Title}\".",
            "OperationRequest", vm.Id);

        TempData["SuccessMessage"] = "Cập nhật yêu cầu thành công!";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id)
    {
        var success = await _service.SubmitAsync(id);
        if (success)
        {
            await _notif.SendToManagersAsync(
                $"📤 {_tenant.UserFullName} gửi yêu cầu chờ duyệt",
                $"{_tenant.UserFullName} đã gửi yêu cầu vận hành #{id.ToString()[..8]} để phê duyệt.",
                "OperationRequest", id);
            await NotifyAssignmentRecipientsAsync(
                id,
                $"📤 {_tenant.UserFullName} gửi yêu cầu chờ duyệt",
                $"{_tenant.UserFullName} đã gửi yêu cầu vận hành #{id.ToString()[..8]} để phê duyệt.");

            var detail = await _queries.GetDetailAsync(id);
            if (detail?.Priority == PriorityLevel.Critical.ToString())
            {
                await _notif.SendToDepartmentAsync(
                    $"🚩 Yêu cầu Critical chờ xử lý",
                    $"{_tenant.UserFullName} đã gửi yêu cầu Critical \"{detail.Title}\" vào hàng đợi ưu tiên.",
                    detail.DepartmentId,
                    "OperationRequest", id);
            }
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Yêu cầu đã được gửi duyệt."
            : "Không thể gửi duyệt yêu cầu này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var success = await _service.CancelAsync(id);
        if (success)
        {
            await _notif.SendToManagersAsync(
                $"🚫 {_tenant.UserFullName} hủy yêu cầu",
                $"{_tenant.UserFullName} đã hủy yêu cầu vận hành #{id.ToString()[..8]}.",
                "OperationRequest",
                id);
            await NotifyCancelRecipientsAsync(
                id,
                $"🚫 {_tenant.UserFullName} hủy yêu cầu",
                $"{_tenant.UserFullName} đã hủy yêu cầu vận hành #{id.ToString()[..8]}.");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Yêu cầu đã bị hủy."
            : "Không thể hủy yêu cầu này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã xóa yêu cầu."
            : "Không thể xóa yêu cầu này.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanContribute)]
    public async Task<IActionResult> StartWork(Guid id)
    {
        var success = await _service.StartWorkAsync(id);
        if (success)
        {
            await NotifyAssignmentRecipientsAsync(
                id,
                $"🔧 {_tenant.UserFullName} bắt đầu xử lý",
                $"{_tenant.UserFullName} đã bắt đầu xử lý yêu cầu #{id.ToString()[..8]}.");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã bắt đầu xử lý yêu cầu."
            : "Không thể bắt đầu xử lý.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanContribute)]
    public async Task<IActionResult> Complete(Guid id)
    {
        var success = await _service.CompleteAsync(id);
        if (success)
        {
            await NotifyAssignmentRecipientsAsync(
                id,
                $"✅ {_tenant.UserFullName} hoàn thành yêu cầu",
                $"{_tenant.UserFullName} đã hoàn thành yêu cầu #{id.ToString()[..8]}.");
            var detail = await _queries.GetDetailAsync(id);
            if (detail?.IsCostOverrun == true)
            {
                await _notif.SendToManagersAsync(
                    $"⚠️ Yêu cầu vượt ngân sách",
                    $"Yêu cầu {detail.RequestNo} vượt {detail.CostVariancePercent:0.#}% so với chi phí dự kiến.",
                    "OperationRequest", id);
            }
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã hoàn thành yêu cầu."
            : "Không thể hoàn thành.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageTemplates)]
    public async Task<IActionResult> SaveAsTemplate(Guid id)
    {
        var result = await _service.CreateTemplateFromRequestAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.TemplateId.HasValue
            ? RedirectToAction(nameof(TemplateEdit), new { id = result.TemplateId.Value })
            : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageAssignments)]
    public async Task<IActionResult> ConvertToPlan(Guid id)
    {
        var result = await _service.ConvertToPlanAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.Success && result.PlanId.HasValue
            ? RedirectToAction("Details", "OperationPlans", new { id = result.PlanId.Value })
            : RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(Guid requestId, OrderLineInputViewModel input)
    {
        var lineId = await _service.AddLineAsync(requestId, input);
        TempData[lineId == Guid.Empty ? "ErrorMessage" : "SuccessMessage"] = lineId == Guid.Empty
            ? "Không thể thêm mục hàng."
            : "Đã thêm mục hàng.";
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLine(Guid lineId, Guid requestId)
    {
        var success = await _service.RemoveLineAsync(lineId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã xóa mục hàng."
            : "Không thể xóa mục hàng.";
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        Guid requestId,
        string? content,
        OperationCommentType type = OperationCommentType.Note,
        Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Nội dung bình luận không được để trống.";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        var result = await _service.AddCommentAsync(requestId, content, type, parentCommentId);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            if (result.MentionedUserIds.Any())
            {
                var commentTypeLabel = GetOperationCommentTypeLabel(type).ToLowerInvariant();
                await _notif.SendAsync(
                    $"💬 {_tenant.UserFullName} nhắc đến bạn",
                    $"{_tenant.UserFullName} đã nhắc đến bạn trong {commentTypeLabel} của yêu cầu #{requestId.ToString()[..8]}.",
                    "OperationRequest",
                    requestId,
                    result.MentionedUserIds.ToArray());
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageAssignments)]
    public async Task<IActionResult> AddAssignment(OperationAssignmentInputViewModel input)
    {
        var result = await _service.AddAssignmentAsync(input);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = input.OperationRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanManageAssignments)]
    public async Task<IActionResult> RemoveAssignment(Guid assignmentId, Guid requestId)
    {
        var result = await _service.RemoveAssignmentAsync(assignmentId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanContribute)]
    public async Task<IActionResult> AddProgress(OperationProgressInputViewModel input)
    {
        var result = await _service.AddProgressAsync(input);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = input.OperationRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(262_144_000)]
    [Authorize(Roles = OperationRoles.CanContribute)]
    public async Task<IActionResult> UploadAttachment(Guid requestId, List<IFormFile>? files)
    {
        var result = await _attachments.UploadAsync(requestId, files ?? new List<IFormFile>());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = requestId });
    }

    public async Task<IActionResult> DownloadAttachment(Guid id)
    {
        var download = await _attachments.OpenAsync(id);
        if (download is null) return NotFound();
        return File(download.Stream, download.ContentType, download.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OperationRoles.CanContribute)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid requestId)
    {
        var result = await _attachments.DeleteAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.RequestId ?? requestId });
    }

    public async Task<IActionResult> Statistics()
    {
        var vm = await _queries.GetStatisticsAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hold(Guid id)
    {
        var success = await _service.HoldAsync(id);
        if (success)
        {
            await NotifyAssignmentRecipientsAsync(
                id,
                $"⏸️ {_tenant.UserFullName} tạm dừng yêu cầu",
                $"{_tenant.UserFullName} đã tạm dừng yêu cầu #{id.ToString()[..8]}.");
            TempData["SuccessMessage"] = "Đã tạm dừng yêu cầu xử lý.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể tạm dừng yêu cầu này.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resume(Guid id)
    {
        var success = await _service.ResumeAsync(id);
        if (success)
        {
            await NotifyAssignmentRecipientsAsync(
                id,
                $"▶️ {_tenant.UserFullName} tiếp tục yêu cầu",
                $"{_tenant.UserFullName} đã tiếp tục yêu cầu #{id.ToString()[..8]}.");
            TempData["SuccessMessage"] = "Đã tiếp tục xử lý yêu cầu.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể tiếp tục xử lý yêu cầu này.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id)
    {
        var success = await _service.ReopenAsync(id);
        if (success)
        {
            await NotifyAssignmentRecipientsAsync(
                id,
                $"🔄 {_tenant.UserFullName} mở lại yêu cầu",
                $"{_tenant.UserFullName} đã mở lại yêu cầu #{id.ToString()[..8]}.");
            TempData["SuccessMessage"] = "Đã mở lại yêu cầu xử lý.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể mở lại yêu cầu này.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task NotifyAssignmentRecipientsAsync(Guid requestId, string title, string body)
    {
        var recipientIds = await _service.GetAssignmentNotificationUserIdsAsync(requestId);
        if (!recipientIds.Any()) return;
        await _notif.SendAsync(title, body, "OperationRequest", requestId, recipientIds.ToArray());
    }

    private async Task NotifyCancelRecipientsAsync(Guid requestId, string title, string body)
    {
        var recipientIds = await _service.GetCancelNotificationUserIdsAsync(requestId);
        if (!recipientIds.Any()) return;
        await _notif.SendAsync(title, body, "OperationRequest", requestId, recipientIds.ToArray());
    }

    private static string GetOperationCommentTypeLabel(OperationCommentType type) => type switch
    {
        OperationCommentType.Question => "Câu hỏi",
        OperationCommentType.Decision => "Quyết định",
        _ => "Ghi chú"
    };
}
