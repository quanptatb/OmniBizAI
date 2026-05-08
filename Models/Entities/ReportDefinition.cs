using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class ReportDefinition : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public ReportType Type { get; set; } = ReportType.Tabular;

    public string? QueryDefinitionJson { get; set; }

    public string? FilterDefinitionJson { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DashboardWidget> DashboardWidgets { get; set; } = new List<DashboardWidget>();
}
