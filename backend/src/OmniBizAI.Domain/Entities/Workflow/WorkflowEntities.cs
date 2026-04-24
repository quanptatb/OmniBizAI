using OmniBizAI.Domain.Common;
using OmniBizAI.Domain.Enums;

namespace OmniBizAI.Domain.Entities.Workflow;

public sealed class WorkflowTemplate : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowCondition> Conditions { get; set; } = new List<WorkflowCondition>();
}

public sealed class WorkflowStep : BaseEntity
{
    public Guid TemplateId { get; set; }
    public WorkflowTemplate Template { get; set; } = null!;
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApproverType { get; set; } = "Role";
    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverUserId { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool CanDelegate { get; set; }
    public int TimeoutHours { get; set; } = 48;
}

public sealed class WorkflowCondition : BaseEntity
{
    public Guid TemplateId { get; set; }
    public WorkflowTemplate Template { get; set; } = null!;
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "gte";
    public string Value { get; set; } = string.Empty;
    public string ThenAction { get; set; } = "AddStep";
    public int? ThenStepOrder { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WorkflowInstance : BaseEntity
{
    public Guid TemplateId { get; set; }
    public WorkflowTemplate Template { get; set; } = null!;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public int CurrentStepOrder { get; set; } = 1;
    public int TotalSteps { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public Guid? InitiatedBy { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public ICollection<WorkflowInstanceStep> Steps { get; set; } = new List<WorkflowInstanceStep>();
}

public sealed class WorkflowInstanceStep : BaseEntity
{
    public Guid InstanceId { get; set; }
    public WorkflowInstance Instance { get; set; } = null!;
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public Guid? AssignedTo { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public ICollection<ApprovalAction> Actions { get; set; } = new List<ApprovalAction>();
}

public sealed class ApprovalAction : BaseEntity
{
    public Guid InstanceStepId { get; set; }
    public WorkflowInstanceStep InstanceStep { get; set; } = null!;
    public Guid InstanceId { get; set; }
    public Guid UserId { get; set; }
    public ApprovalActionType Action { get; set; }
    public string? Comment { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
