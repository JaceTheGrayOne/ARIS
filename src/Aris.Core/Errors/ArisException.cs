namespace Aris.Core.Errors;

/// <summary>
/// Base exception for ARIS-specific errors.
/// Provides structured error information for logging and frontend display.
/// </summary>
public abstract class ArisException : Exception
{
    /// <summary>
    /// Machine-readable error code for categorization.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Optional remediation hint for users.
    /// </summary>
    public string? RemediationHint { get; init; }

    protected ArisException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    protected ArisException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
