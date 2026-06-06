using FluentValidation;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Validators;

// ── OperationRequest Create ──────────────────────────────────────────────────
public class OperationRequestCreateValidator : AbstractValidator<OperationRequestCreateViewModel>
{
    public OperationRequestCreateValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .MaximumLength(250).WithMessage("Tiêu đề không quá 250 ký tự.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loại yêu cầu không được để trống.");

        RuleFor(x => x.OrganizationUnitId)
            .NotEqual(Guid.Empty).WithMessage("Phòng ban phụ trách không được để trống.");

        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Hạn hoàn thành không được nhỏ hơn ngày hôm nay.");

        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).When(x => x.TotalAmount.HasValue)
            .WithMessage("Số tiền không hợp lệ.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không quá 2000 ký tự.");
    }
}

// ── OperationRequest Edit ────────────────────────────────────────────────────
public class OperationRequestEditValidator : AbstractValidator<OperationRequestEditViewModel>
{
    public OperationRequestEditValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề không được để trống.")
            .MaximumLength(250).WithMessage("Tiêu đề không quá 250 ký tự.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loại yêu cầu không được để trống.");

        RuleFor(x => x.OrganizationUnitId)
            .NotEqual(Guid.Empty).WithMessage("Phòng ban phụ trách không được để trống.");

        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Hạn hoàn thành không được nhỏ hơn ngày hôm nay.");

        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).When(x => x.TotalAmount.HasValue)
            .WithMessage("Số tiền không hợp lệ.");
    }
}

// ── WorkOrder Create ─────────────────────────────────────────────────────────
public class WorkOrderCreateValidator : AbstractValidator<WorkOrderCreateFormViewModel>
{
    public WorkOrderCreateValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEqual(Guid.Empty).WithMessage("Vui lòng chọn thiết bị.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề lệnh công việc không được để trống.")
            .MaximumLength(250).WithMessage("Tiêu đề không quá 250 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không quá 2000 ký tự.");

        RuleFor(x => x.ScheduledEnd)
            .GreaterThan(x => x.ScheduledStart)
            .When(x => x.ScheduledStart.HasValue && x.ScheduledEnd.HasValue)
            .WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu.");

        RuleFor(x => x.EstimatedHours)
            .GreaterThan(0).When(x => x.EstimatedHours.HasValue)
            .WithMessage("Số giờ dự kiến phải lớn hơn 0.");

        RuleFor(x => x.EstimatedCost)
            .GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue)
            .WithMessage("Chi phí dự kiến không hợp lệ.");
    }
}

// ── SparePartRequisition Create ──────────────────────────────────────────────
public class SparePartRequisitionCreateValidator : AbstractValidator<SparePartRequisitionFormViewModel>
{
    public SparePartRequisitionCreateValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Vui lòng nhập lý do yêu cầu cấp phụ tùng.")
            .MaximumLength(500).WithMessage("Lý do không quá 500 ký tự.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Phải có ít nhất một dòng phụ tùng.")
            .Must(lines => lines.All(l => l.Quantity > 0))
            .WithMessage("Số lượng phụ tùng phải lớn hơn 0.");
    }
}
