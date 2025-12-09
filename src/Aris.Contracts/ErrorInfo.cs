namespace Aris.Contracts;

/// <summary>
/// Simple error envelope for API responses.
/// </summary>
public sealed record ErrorInfo(
    /// <summary>
    /// Error code identifying the type of error.
    /// </summary>
    string Code,
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    string Message,
    /// <summary>
    /// Optional hint for how to fix or work around the error.
    /// </summary>
    string? RemediationHint
);
