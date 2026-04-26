namespace OmniBizAI.Models.Entities.Finance;

public enum PaymentRequestStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    Paid,
    Cancelled
}

public enum PaymentPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum BudgetStatus
{
    Active,
    Frozen,
    Closed
}
