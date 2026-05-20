using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class ProductionStep : TenantEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    [Required, StringLength(150)]
    public string StepName { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public ProductionStepStatus Status { get; set; } = ProductionStepStatus.Todo;

    public Guid? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }

    public QcStatus QcStatus { get; set; } = QcStatus.Pending;

    [StringLength(500)]
    public string? QcNotes { get; set; }

    public Guid? QcUserId { get; set; }
    public AppUser? QcUser { get; set; }

    public DateTimeOffset? QcCheckedAt { get; set; }
}
