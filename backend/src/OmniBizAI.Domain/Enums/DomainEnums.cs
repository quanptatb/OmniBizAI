namespace OmniBizAI.Domain.Enums;

public enum RoleName
{
    Admin,
    Director,
    Manager,
    Accountant,
    HR,
    Staff
}

public enum EntityStatus
{
    Active,
    Inactive,
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    Cancelled,
    Completed,
    Closed
}

public enum BudgetStatus
{
    Active,
    Frozen,
    Closed
}

public enum PaymentRequestStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    Paid,
    Cancelled
}

public enum TransactionType
{
    Income,
    Expense
}

public enum WorkflowStatus
{
    Pending,
    InProgress,
    Approved,
    Rejected,
    Cancelled,
    Expired
}

public enum ApprovalActionType
{
    Approve,
    Reject,
    Comment,
    RequestChange,
    Delegate
}

public enum OwnerType
{
    Company,
    Department,
    Individual
}

public enum MetricType
{
    Number,
    Percentage,
    Currency,
    Boolean,
    Milestone
}

public enum ProgressDirection
{
    Increase,
    Decrease
}

public enum CheckInStatus
{
    Submitted,
    Approved,
    Rejected
}
