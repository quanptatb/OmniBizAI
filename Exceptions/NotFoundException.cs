// PR #23 — transaction boundary
namespace OmniBizAI.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found.
/// </summary>
public sealed class NotFoundException : Exception
{
    public string EntityType { get; }
    public string EntityId { get; }

    public NotFoundException(string entityType, object entityId)
        : base($"{entityType} with id '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId.ToString() ?? "";
    }

    public NotFoundException(string message) : base(message)
    {
        EntityType = "";
        EntityId = "";
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        EntityType = "";
        EntityId = "";
    }
}
