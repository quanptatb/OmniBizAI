namespace OmniBizAI.Models.Entities;

/// <summary>
/// Base entity with a GUID primary key.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}
