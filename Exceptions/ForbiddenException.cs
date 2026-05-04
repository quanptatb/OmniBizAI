// PR #23 — transaction boundary
namespace OmniBizAI.Exceptions;

/// <summary>
/// Thrown when the current user does not have the required role or permission.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public string UserId { get; }
    public string RequiredPermission { get; }

    public ForbiddenException(string userId, string requiredPermission)
        : base($"User '{userId}' does not have the required permission '{requiredPermission}'.")
    {
        UserId = userId;
        RequiredPermission = requiredPermission;
    }

    public ForbiddenException(string message) : base(message)
    {
        UserId = "";
        RequiredPermission = "";
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
        UserId = "";
        RequiredPermission = "";
    }
}
