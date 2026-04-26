namespace OmniBizAI.Models.Entities;

/// <summary>
/// Entity with audit trail fields (created/updated timestamps and actors).
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
