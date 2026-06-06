using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class OperationRequestAssignment : TenantEntity
{
    public Guid OperationRequestId { get; set; }
    public OperationRequest? OperationRequest { get; set; }

    public Guid? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public OperationAssignmentRole Role { get; set; } = OperationAssignmentRole.Support;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(500)]
    public string? Note { get; set; }
}
