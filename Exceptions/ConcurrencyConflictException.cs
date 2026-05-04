// PR #23 — transaction boundary
namespace OmniBizAI.Exceptions;

/// <summary>
/// Thrown when a concurrency conflict is detected (RowVersion mismatch).
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public string EntityType { get; }
    public string EntityId { get; }

    public ConcurrencyConflictException(string entityType, object entityId)
        : base($"Concurrency conflict on {entityType} '{entityId}'. The record was modified by another user. Please reload and try again.")
    {
        EntityType = entityType;
        EntityId = entityId.ToString() ?? "";
    }

    public ConcurrencyConflictException(string entityType, object entityId, Exception innerException)
        : base($"Concurrency conflict on {entityType} '{entityId}'. The record was modified by another user. Please reload and try again.", innerException)
    {
        EntityType = entityType;
        EntityId = entityId.ToString() ?? "";
    }

    public ConcurrencyConflictException(string message) : base(message)
    {
        EntityType = "";
        EntityId = "";
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        EntityType = "";
        EntityId = "";
    }
}
