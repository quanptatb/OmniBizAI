using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Helpers;

/// <summary>KPI/OKR workflow state machine — controls valid status transitions.</summary>
public static class WorkflowStatusHelper
{
    // ── KPI Status Transitions ───────────────────────────────────────────────
    private static readonly Dictionary<KpiStatus, KpiStatus[]> KpiTransitions = new()
    {
        [KpiStatus.Draft] = [KpiStatus.PendingApproval, KpiStatus.Cancelled],
        [KpiStatus.PendingApproval] = [KpiStatus.Active, KpiStatus.Rejected],
        [KpiStatus.Active] = [KpiStatus.NearTarget, KpiStatus.Completed, KpiStatus.Failed, KpiStatus.Cancelled],
        [KpiStatus.NearTarget] = [KpiStatus.Completed, KpiStatus.Failed],
        [KpiStatus.Rejected] = [KpiStatus.Draft],
        [KpiStatus.Completed] = [],
        [KpiStatus.Failed] = [],
        [KpiStatus.Cancelled] = [],
    };

    public static bool CanTransition(KpiStatus from, KpiStatus to) =>
        KpiTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static KpiStatus[] GetAllowedTransitions(KpiStatus current) =>
        KpiTransitions.TryGetValue(current, out var allowed) ? allowed : [];

    // ── OKR Status Transitions ───────────────────────────────────────────────
    private static readonly Dictionary<OkrStatus, OkrStatus[]> OkrTransitions = new()
    {
        [OkrStatus.Draft] = [OkrStatus.Active, OkrStatus.Cancelled],
        [OkrStatus.Active] = [OkrStatus.Completed, OkrStatus.Cancelled],
        [OkrStatus.Completed] = [],
        [OkrStatus.Cancelled] = [],
    };

    public static bool CanOkrTransition(OkrStatus from, OkrStatus to) =>
        OkrTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    // ── Evaluation Submission Transitions ────────────────────────────────────
    private static readonly Dictionary<EvaluationSubmissionStatus, EvaluationSubmissionStatus[]> EvalTransitions = new()
    {
        [EvaluationSubmissionStatus.Draft] = [EvaluationSubmissionStatus.Submitted],
        [EvaluationSubmissionStatus.Submitted] = [EvaluationSubmissionStatus.DirectorReviewed, EvaluationSubmissionStatus.Draft],
        [EvaluationSubmissionStatus.DirectorReviewed] = [],
    };

    public static bool CanEvalTransition(EvaluationSubmissionStatus from, EvaluationSubmissionStatus to) =>
        EvalTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    // ── Display names ────────────────────────────────────────────────────────
    public static string GetKpiStatusDisplay(KpiStatus status) => status switch
    {
        KpiStatus.Draft => "Bản nháp",
        KpiStatus.PendingApproval => "Chờ duyệt",
        KpiStatus.Active => "Đang thực hiện",
        KpiStatus.NearTarget => "Gần đạt",
        KpiStatus.Completed => "Hoàn thành",
        KpiStatus.Failed => "Không đạt",
        KpiStatus.Rejected => "Từ chối",
        KpiStatus.Cancelled => "Hủy bỏ",
        _ => status.ToString()
    };

    public static string GetKpiStatusBadgeClass(KpiStatus status) => status switch
    {
        KpiStatus.Draft => "bg-secondary",
        KpiStatus.PendingApproval => "bg-warning text-dark",
        KpiStatus.Active => "bg-primary",
        KpiStatus.NearTarget => "bg-info",
        KpiStatus.Completed => "bg-success",
        KpiStatus.Failed => "bg-danger",
        KpiStatus.Rejected => "bg-danger",
        KpiStatus.Cancelled => "bg-dark",
        _ => "bg-secondary"
    };
}
