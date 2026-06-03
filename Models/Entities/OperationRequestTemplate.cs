using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class OperationRequestTemplate : TenantEntity
{
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public Guid DefaultDepartmentId { get; set; }
    public OrganizationUnit? DefaultDepartment { get; set; }

    public string? DefaultLinesJson { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int UsageCount { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }
}

public class OperationRequestTemplateLineDefinition
{
    public Guid? ProductServiceId { get; set; }

    public string? ProductName { get; set; }

    public decimal Quantity { get; set; } = 1;

    public decimal? UnitPrice { get; set; }

    public string? Note { get; set; }
}
