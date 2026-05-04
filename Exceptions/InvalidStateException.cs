// PR #23 — transaction boundary
namespace OmniBizAI.Exceptions;

/// <summary>
/// Thrown when an entity is not in the expected state for the requested operation.
/// </summary>
public sealed class InvalidStateException : Exception
{
    public string EntityType { get; }
    public string CurrentState { get; }
    public string ExpectedState { get; }

    public InvalidStateException(string entityType, string currentState, string expectedState)
        : base($"{entityType} is in state '{currentState}', expected '{expectedState}'.")
    {
        EntityType = entityType;
        CurrentState = currentState;
        ExpectedState = expectedState;
    }

    public InvalidStateException(string message) : base(message)
    {
        EntityType = "";
        CurrentState = "";
        ExpectedState = "";
    }

    public InvalidStateException(string message, Exception innerException)
        : base(message, innerException)
    {
        EntityType = "";
        CurrentState = "";
        ExpectedState = "";
    }
}
