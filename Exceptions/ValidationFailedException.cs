// PR #23 — transaction boundary
namespace OmniBizAI.Exceptions;

/// <summary>
/// Thrown when business validation fails with one or more error details.
/// </summary>
public sealed class ValidationFailedException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationFailedException(IReadOnlyList<string> errors)
        : base($"Validation failed: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }

    public ValidationFailedException(string singleError)
        : base($"Validation failed: {singleError}")
    {
        Errors = [singleError];
    }

    public ValidationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = [];
    }
}
