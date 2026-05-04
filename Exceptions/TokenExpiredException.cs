// PR #23 — transaction boundary
namespace OmniBizAI.Exceptions;

/// <summary>
/// Thrown when a customer approval token is expired, already used, or not found.
/// </summary>
public sealed class TokenExpiredException : Exception
{
    public string TokenDetail { get; }

    public TokenExpiredException(string detail)
        : base($"Token is invalid or expired: {detail}")
    {
        TokenDetail = detail;
    }

    public TokenExpiredException(string message, Exception innerException)
        : base(message, innerException)
    {
        TokenDetail = "";
    }
}
