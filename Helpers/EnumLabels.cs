using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Helpers;

/// <summary>
/// Vietnamese labels for all domain enums. Single source of truth — edit here to change labels everywhere.
/// </summary>
public static class EnumLabels
{
    private static readonly Dictionary<Type, Dictionary<int, string>> _labels = new()
    {
        [typeof(OperationStatus)] = new()
        {
            [(int)OperationStatus.Draft] = "Bản nháp",
            [(int)OperationStatus.Submitted] = "Đã gửi duyệt",
            [(int)OperationStatus.InReview] = "Đang xem xét",
            [(int)OperationStatus.Approved] = "Đã duyệt",
            [(int)OperationStatus.InProgress] = "Đang xử lý",
            [(int)OperationStatus.Completed] = "Hoàn thành",
            [(int)OperationStatus.Rejected] = "Từ chối",
            [(int)OperationStatus.Cancelled] = "Đã hủy"
        },
        [typeof(PriorityLevel)] = new()
        {
            [(int)PriorityLevel.Low] = "Thấp",
            [(int)PriorityLevel.Normal] = "Bình thường",
            [(int)PriorityLevel.High] = "Cao",
            [(int)PriorityLevel.Critical] = "Nghiêm trọng"
        },
        [typeof(ProcurementStatus)] = new()
        {
            [(int)ProcurementStatus.Draft] = "Nháp",
            [(int)ProcurementStatus.Submitted] = "Đã gửi",
            [(int)ProcurementStatus.Approved] = "Đã duyệt",
            [(int)ProcurementStatus.Ordered] = "Đã đặt",
            [(int)ProcurementStatus.Received] = "Đã nhận",
            [(int)ProcurementStatus.Cancelled] = "Đã hủy"
        },
        [typeof(PurchaseOrderStatus)] = new()
        {
            [(int)PurchaseOrderStatus.Draft] = "Nháp",
            [(int)PurchaseOrderStatus.Sent] = "Đã gửi",
            [(int)PurchaseOrderStatus.PartiallyReceived] = "Nhận một phần",
            [(int)PurchaseOrderStatus.Completed] = "Hoàn thành",
            [(int)PurchaseOrderStatus.Cancelled] = "Đã hủy"
        },
        [typeof(PaymentStatus)] = new()
        {
            [(int)PaymentStatus.Draft] = "Nháp",
            [(int)PaymentStatus.Submitted] = "Đã gửi",
            [(int)PaymentStatus.Approved] = "Đã duyệt",
            [(int)PaymentStatus.Paid] = "Đã thanh toán",
            [(int)PaymentStatus.Rejected] = "Từ chối",
            [(int)PaymentStatus.Cancelled] = "Đã hủy"
        },
        [typeof(BudgetStatus)] = new()
        {
            [(int)BudgetStatus.Draft] = "Nháp",
            [(int)BudgetStatus.Active] = "Hoạt động",
            [(int)BudgetStatus.Closed] = "Đã đóng",
            [(int)BudgetStatus.Cancelled] = "Đã hủy"
        },
        [typeof(ExpenseStatus)] = new()
        {
            [(int)ExpenseStatus.Recorded] = "Đã ghi nhận",
            [(int)ExpenseStatus.Approved] = "Đã duyệt",
            [(int)ExpenseStatus.Reversed] = "Đã hoàn"
        },
        [typeof(LeaveType)] = new()
        {
            [(int)LeaveType.Annual] = "Phép năm",
            [(int)LeaveType.Sick] = "Ốm đau",
            [(int)LeaveType.Personal] = "Việc riêng",
            [(int)LeaveType.Maternity] = "Thai sản",
            [(int)LeaveType.Unpaid] = "Không lương"
        },
        [typeof(LeaveStatus)] = new()
        {
            [(int)LeaveStatus.Draft] = "Nháp",
            [(int)LeaveStatus.Submitted] = "Chờ duyệt",
            [(int)LeaveStatus.Approved] = "Đã duyệt",
            [(int)LeaveStatus.Rejected] = "Từ chối",
            [(int)LeaveStatus.Cancelled] = "Đã hủy"
        },
        [typeof(UserStatus)] = new()
        {
            [(int)UserStatus.Active] = "Hoạt động",
            [(int)UserStatus.Locked] = "Đã khóa",
            [(int)UserStatus.Inactive] = "Nghỉ việc"
        },
        [typeof(OkrLevel)] = new()
        {
            [(int)OkrLevel.Company] = "Công ty",
            [(int)OkrLevel.Department] = "Phòng ban",
            [(int)OkrLevel.Individual] = "Cá nhân"
        },
        [typeof(OkrStatus)] = new()
        {
            [(int)OkrStatus.Draft] = "Bản nháp",
            [(int)OkrStatus.Active] = "Đang thực hiện",
            [(int)OkrStatus.Completed] = "Hoàn thành",
            [(int)OkrStatus.Cancelled] = "Đã hủy"
        },
        [typeof(KpiStatus)] = new()
        {
            [(int)KpiStatus.Draft] = "Bản nháp",
            [(int)KpiStatus.PendingApproval] = "Chờ duyệt",
            [(int)KpiStatus.Active] = "Đang thực hiện",
            [(int)KpiStatus.NearTarget] = "Gần mục tiêu",
            [(int)KpiStatus.Completed] = "Hoàn thành",
            [(int)KpiStatus.Failed] = "Không đạt",
            [(int)KpiStatus.Rejected] = "Từ chối",
            [(int)KpiStatus.Cancelled] = "Đã hủy"
        },
        [typeof(KpiOwnerType)] = new()
        {
            [(int)KpiOwnerType.Company] = "Công ty",
            [(int)KpiOwnerType.Department] = "Phòng ban",
            [(int)KpiOwnerType.User] = "Cá nhân"
        },
        [typeof(KpiPeriodType)] = new()
        {
            [(int)KpiPeriodType.Monthly] = "Hàng tháng",
            [(int)KpiPeriodType.Quarterly] = "Hàng quý",
            [(int)KpiPeriodType.Yearly] = "Hàng năm",
            [(int)KpiPeriodType.Custom] = "Tùy chỉnh"
        },
        [typeof(KpiPropertyType)] = new()
        {
            [(int)KpiPropertyType.Growth] = "Tăng trưởng",
            [(int)KpiPropertyType.Stability] = "Ổn định",
            [(int)KpiPropertyType.Reduction] = "Giảm thiểu"
        },
        [typeof(KpiMeasureType)] = new()
        {
            [(int)KpiMeasureType.Quantitative] = "Định lượng",
            [(int)KpiMeasureType.Qualitative] = "Định tính",
            [(int)KpiMeasureType.Behavioral] = "Hành vi"
        },
        [typeof(ApprovalStatus)] = new()
        {
            [(int)ApprovalStatus.Pending] = "Chờ duyệt",
            [(int)ApprovalStatus.Approved] = "Đã duyệt",
            [(int)ApprovalStatus.Rejected] = "Từ chối",
            [(int)ApprovalStatus.Skipped] = "Bỏ qua",
            [(int)ApprovalStatus.Cancelled] = "Đã hủy"
        },
        [typeof(WorkItemStatus)] = new()
        {
            [(int)WorkItemStatus.Todo] = "Cần làm",
            [(int)WorkItemStatus.InProgress] = "Đang thực hiện",
            [(int)WorkItemStatus.Blocked] = "Bị chặn",
            [(int)WorkItemStatus.Done] = "Hoàn thành",
            [(int)WorkItemStatus.Cancelled] = "Đã hủy"
        },
        [typeof(CheckInReviewStatus)] = new()
        {
            [(int)CheckInReviewStatus.Pending] = "Chờ duyệt",
            [(int)CheckInReviewStatus.Approved] = "Đã duyệt",
            [(int)CheckInReviewStatus.Rejected] = "Từ chối"
        },
        [typeof(MissionVisionType)] = new()
        {
            [(int)MissionVisionType.Vision] = "Tầm nhìn",
            [(int)MissionVisionType.Mission] = "Sứ mệnh",
            [(int)MissionVisionType.YearlyGoal] = "Mục tiêu năm"
        },
        [typeof(ModuleStatus)] = new()
        {
            [(int)ModuleStatus.Disabled] = "Tắt",
            [(int)ModuleStatus.Enabled] = "Bật",
            [(int)ModuleStatus.Trial] = "Dùng thử"
        },
        [typeof(RiskLevel)] = new()
        {
            [(int)RiskLevel.Low] = "Thấp",
            [(int)RiskLevel.Medium] = "Trung bình",
            [(int)RiskLevel.High] = "Cao"
        }
    };

    /// <summary>
    /// Get Vietnamese label for an enum value. Falls back to .ToString() if not mapped.
    /// </summary>
    public static string Get<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var key = Convert.ToInt32(value);
        if (_labels.TryGetValue(typeof(TEnum), out var dict) && dict.TryGetValue(key, out var label))
            return label;
        return value.ToString();
    }
}
