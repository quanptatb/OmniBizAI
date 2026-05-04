// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Configuration;

/// <summary>
/// Cấu hình workflow duyệt nội bộ theo tenant.
/// Mỗi row = 1 bước trong workflow.
/// </summary>
public sealed class ApprovalWorkflowConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>
    /// Workflow type, e.g. "menu_internal_approval".
    /// </summary>
    [MaxLength(100)]
    public string WorkflowType { get; set; } = "";

    /// <summary>
    /// Step number (1-based, sequential).
    /// </summary>
    public int StepNo { get; set; }

    [MaxLength(100)]
    public string StepName { get; set; } = "";

    /// <summary>
    /// FK to RoleDefinition — the role required for this step.
    /// </summary>
    public Guid RequiredRoleDefinitionId { get; set; }

    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
