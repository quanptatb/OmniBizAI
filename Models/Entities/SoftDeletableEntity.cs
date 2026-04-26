namespace OmniBizAI.Models.Entities;

/// <summary>
/// Entity supporting soft delete with IsDeleted flag and DeletedAt timestamp.
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
