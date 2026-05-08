using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class DashboardWidget : TenantEntity
{
    public Guid? ReportDefinitionId { get; set; }
    public ReportDefinition? ReportDefinition { get; set; }

    public Guid? RoleDefinitionId { get; set; }
    public RoleDefinition? RoleDefinition { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public WidgetType Type { get; set; } = WidgetType.Number;

    public int SortOrder { get; set; }

    public string? ConfigurationJson { get; set; }

    public bool IsActive { get; set; } = true;
}
