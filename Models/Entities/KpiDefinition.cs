using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class KpiDefinition : TenantEntity
{
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

    public KpiOwnerType OwnerType { get; set; } = KpiOwnerType.Department;

    public KpiPeriodType PeriodType { get; set; } = KpiPeriodType.Monthly;

    public KpiMeasureType MeasureType { get; set; } = KpiMeasureType.Quantitative;

    public KpiPropertyType PropertyType { get; set; } = KpiPropertyType.Growth;

    public KpiStatus Status { get; set; } = KpiStatus.Draft;

    public bool IsActive { get; set; } = true;

    // OKR linkage
    public Guid? OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    public Guid? OkrKeyResultId { get; set; }
    public OkrKeyResult? OkrKeyResult { get; set; }

    // Evaluation period
    public Guid? EvaluationPeriodId { get; set; }
    public EvaluationPeriod? EvaluationPeriod { get; set; }

    // Assigner (who created/assigned this KPI)
    public Guid? AssignerUserId { get; set; }
    public AppUser? AssignerUser { get; set; }

    // Navigation collections
    public ICollection<KpiTarget> Targets { get; set; } = new List<KpiTarget>();
    public ICollection<KpiDepartmentAssignment> DepartmentAssignments { get; set; } = new List<KpiDepartmentAssignment>();
    public ICollection<KpiEmployeeAssignment> EmployeeAssignments { get; set; } = new List<KpiEmployeeAssignment>();
}
