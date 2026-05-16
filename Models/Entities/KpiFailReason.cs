using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Catalog of reasons why a KPI check-in might fail.</summary>
public class KpiFailReason : TenantEntity
{
    [StringLength(200)]
    public string ReasonName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
