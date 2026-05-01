namespace OmniBizAI.Models.Entities;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
