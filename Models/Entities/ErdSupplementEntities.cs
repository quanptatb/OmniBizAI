using System;

namespace OmniBizAI.Models.Entities;

public partial class DepartmentBudgetAllocation
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public int FiscalYear { get; set; }
    public int? FiscalQuarter { get; set; }
    public decimal AllocatedAmount { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class BudgetLineItem
{
    public Guid Id { get; set; }
    public Guid BudgetId { get; set; }
    public string Description { get; set; } = null!;
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public partial class VendorContact
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string ContactName { get; set; } = null!;
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

public partial class VendorBankAccount
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string BankName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string? AccountHolder { get; set; }
    public string? Branch { get; set; }
    public string? SwiftCode { get; set; }
    public bool IsDefault { get; set; }
}

public partial class VendorRating
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public Guid? RatedBy { get; set; }
    public decimal Score { get; set; }
    public string? Criteria { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class TransactionTag
{
    public Guid TransactionId { get; set; }
    public string Tag { get; set; } = null!;
}

public partial class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public partial class PaymentRequestComment
{
    public Guid Id { get; set; }
    public Guid PaymentRequestId { get; set; }
    public Guid? UserId { get; set; }
    public string Comment { get; set; } = null!;
    public string CommentType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public partial class KpiTemplate
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string MetricType { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? DefaultTarget { get; set; }
    public string? Formula { get; set; }
    public string? DataSource { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class KrCheckIn
{
    public Guid Id { get; set; }
    public Guid KeyResultId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public decimal? PreviousValue { get; set; }
    public decimal NewValue { get; set; }
    public decimal? Progress { get; set; }
    public string? Confidence { get; set; }
    public string Note { get; set; } = null!;
    public string? Blockers { get; set; }
    public string? NextSteps { get; set; }
    public string EvidenceUrls { get; set; } = "[]";
    public string Status { get; set; } = null!;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class EvaluationScore
{
    public Guid Id { get; set; }
    public Guid EvaluationId { get; set; }
    public string SourceType { get; set; } = null!;
    public Guid SourceId { get; set; }
    public string? SourceName { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Score { get; set; }
    public string? Rating { get; set; }
    public string? Comment { get; set; }
}

public partial class KpiTargetsHistory
{
    public Guid Id { get; set; }
    public Guid KpiId { get; set; }
    public decimal? OldTarget { get; set; }
    public decimal? NewTarget { get; set; }
    public string? Reason { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class OkrAlignment
{
    public Guid Id { get; set; }
    public Guid? SourceObjectiveId { get; set; }
    public Guid? TargetObjectiveId { get; set; }
    public string? AlignmentType { get; set; }
    public decimal? ContributionWeight { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class KpiComment
{
    public Guid Id { get; set; }
    public Guid KpiId { get; set; }
    public Guid? UserId { get; set; }
    public string Comment { get; set; } = null!;
    public string CommentType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public partial class AiEmbedding
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = null!;
    public Guid SourceId { get; set; }
    public string Content { get; set; } = null!;
    public byte[] Embedding { get; set; } = null!;
    public string Metadata { get; set; } = "{}";
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public partial class AiPromptTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string SystemPrompt { get; set; } = null!;
    public string UserPromptTemplate { get; set; } = null!;
    public string Variables { get; set; } = "[]";
    public string ModelConfig { get; set; } = "{}";
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public partial class EmailQueue
{
    public Guid Id { get; set; }
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? TemplateName { get; set; }
    public string? TemplateData { get; set; }
    public string Status { get; set; } = null!;
    public int Attempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class SystemSetting
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string ValueType { get; set; } = null!;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public partial class BackgroundJob
{
    public Guid Id { get; set; }
    public string JobType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? InputData { get; set; }
    public string? OutputData { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
}
