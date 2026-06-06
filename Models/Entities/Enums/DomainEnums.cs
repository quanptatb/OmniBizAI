namespace OmniBizAI.Models.Entities.Enums;

public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Archived = 3
}

public enum UserStatus
{
    Active = 1,
    Locked = 2,
    Inactive = 3
}

public enum ModuleStatus
{
    Disabled = 0,
    Enabled = 1,
    Trial = 2
}

public enum PriorityLevel
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum OperationStatus
{
    Draft = 1,
    Submitted = 2,
    InReview = 3,
    Approved = 4,
    InProgress = 5,
    Completed = 6,
    Rejected = 7,
    Cancelled = 8,
    OnHold = 9
}

public enum WorkItemStatus
{
    Todo = 1,
    InProgress = 2,
    Blocked = 3,
    Done = 4,
    Cancelled = 5
}

public enum SprintStatus
{
    Planned = 1,
    Active = 2,
    Closed = 3
}

public enum WorkItemDependencyType
{
    BlockedBy = 1,
    RelatesTo = 2,
    Duplicates = 3
}

public enum OperationPlanStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}

public enum PlanChangeOrderStatus
{
    Approved = 1,
    Cancelled = 2
}

public enum PlanTaskDependencyType
{
    FinishToStart = 1,
    StartToStart = 2,
    FinishToFinish = 3,
    StartToFinish = 4
}

public enum PlanTaskStatus
{
    Todo = 1,
    InProgress = 2,
    Done = 3,
    Delayed = 4,
    Cancelled = 5
}

public enum EquipmentStatus
{
    Available = 1,
    InUse = 2,
    Maintenance = 3,
    OutOfOrder = 4,
    Retired = 5
}

public enum EquipmentCostType
{
    Purchase = 1,
    Maintenance = 2,
    Repair = 3,
    SparePart = 4,
    Other = 5
}

public enum ShiftAssignmentStatus
{
    Scheduled = 1,
    CheckedIn = 2,
    CheckedOut = 3,
    Absent = 4,
    Late = 5,
    Cancelled = 6
}

public enum MaintenanceType
{
    Preventive = 1,
    Corrective = 2,
    Predictive = 3,
    Emergency = 4,
    Inspection = 5
}

public enum MaintenanceRecordStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum WorkspaceStatus
{
    Active = 1,
    Maintenance = 2,
    Inactive = 3
}

public enum IncidentStatus
{
    Open = 1,
    Investigating = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
    Reopened = 6
}

public enum IncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum PmFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5,
    ByRunHours = 6,
    ByCycles = 7
}

public enum SensorReadingStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3
}

public enum OperationSlaBreachType
{
    ApprovalWarning = 1,
    ApprovalOverdue = 2,
    ResolutionWarning = 3,
    ResolutionOverdue = 4
}

public enum OperationAssignmentRole
{
    Primary = 1,
    Support = 2,
    Watcher = 3
}

public enum OperationCommentType
{
    Note = 1,
    Question = 2,
    Decision = 3
}

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Skipped = 4,
    Cancelled = 5
}

public enum WorkflowInstanceStatus
{
    Running = 1,
    Completed = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum ProcurementStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Ordered = 4,
    Received = 5,
    Cancelled = 6
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyReceived = 3,
    Completed = 4,
    Cancelled = 5
}

public enum GoodsReceiptStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum GoodsIssueStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum StockAlertStatus
{
    Active = 1,
    Acknowledged = 2,
    Resolved = 3
}

public enum PaymentStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Paid = 4,
    Rejected = 5,
    Cancelled = 6
}

public enum BudgetStatus
{
    Draft = 1,
    Active = 2,
    Closed = 3,
    Cancelled = 4
}

public enum ExpenseStatus
{
    Recorded = 1,
    Approved = 2,
    Reversed = 3
}

public enum CashTransactionStatus
{
    Recorded = 1,
    Approved = 2,
    Rejected = 3,
    Voided = 4
}

public enum LeaveType
{
    Annual = 1,
    Sick = 2,
    Personal = 3,
    Maternity = 4,
    Unpaid = 5
}

public enum LeaveStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum KpiPeriodType
{
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3,
    Custom = 4
}

public enum KpiOwnerType
{
    Company = 1,
    Department = 2,
    User = 3
}

public enum ReportType
{
    Dashboard = 1,
    Tabular = 2,
    Chart = 3,
    Export = 4
}

public enum WidgetType
{
    Number = 1,
    Chart = 2,
    Table = 3,
    AiInsight = 4
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum AiInsightStatus
{
    Draft = 1,
    Reviewed = 2,
    Applied = 3,
    Dismissed = 4
}

public enum ImportJobStatus
{
    Uploaded = 1,
    Validating = 2,
    ReadyToCommit = 3,
    Committed = 4,
    Failed = 5,
    Cancelled = 6
}

public enum NotificationStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Delivered = 2,
    Read = 3,
    Failed = 4
}

// ── KPI/OKR Module Enums ─────────────────────────────────────────────────────

public enum MissionVisionType
{
    Vision = 1,
    Mission = 2,
    YearlyGoal = 3
}

public enum OkrLevel
{
    Company = 1,
    Department = 2,
    Individual = 3
}

public enum OkrStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Cancelled = 4
}

public enum KpiPropertyType
{
    Growth = 1,
    Stability = 2,
    Reduction = 3
}

public enum KpiMeasureType
{
    Quantitative = 1,
    Qualitative = 2,
    Behavioral = 3
}

public enum KpiStatus
{
    Draft = 1,
    PendingApproval = 2,
    Active = 3,
    NearTarget = 4,
    Completed = 5,
    Failed = 6,
    Rejected = 7,
    Cancelled = 8
}

public enum CheckInReviewStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum EvaluationPeriodStatus
{
    Open = 1,
    InProgress = 2,
    Closed = 3
}

public enum EvaluationSubmissionStatus
{
    Draft = 1,
    Submitted = 2,
    DirectorReviewed = 3
}

public enum GradingRankCode
{
    S = 1,
    APlus = 2,
    A = 3,
    BPlus = 4,
    B = 5,
    C = 6,
    D = 7
}

public enum SalesOrderStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    InProduction = 4,
    QualityChecking = 5,
    Completed = 6,
    Cancelled = 7
}

public enum ProductionStepStatus
{
    Todo = 1,
    InProgress = 2,
    Completed = 3
}

public enum QcStatus
{
    Pending = 1,
    Passed = 2,
    Failed = 3
}

// ── Maintenance Module Enums (F5.x) ──────────────────────────────────────────

public enum WorkOrderType
{
    Preventive = 1,
    Corrective = 2,
    Inspection = 3,
    Predictive = 4,
    Emergency = 5
}

public enum WorkOrderStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    OnHold = 4,
    Completed = 5,
    Cancelled = 6
}

public enum SparePartRequisitionStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Issued = 4,
    Rejected = 5,
    Cancelled = 6
}

public enum PmTriggerType
{
    TimeBased = 1,
    RunHoursBased = 2,
    CyclesBased = 3,
    ConditionBased = 4
}

public enum FailureModeCategory
{
    Mechanical = 1,
    Electrical = 2,
    Hydraulic = 3,
    Pneumatic = 4,
    Software = 5,
    Human = 6,
    Environmental = 7,
    Other = 8
}
