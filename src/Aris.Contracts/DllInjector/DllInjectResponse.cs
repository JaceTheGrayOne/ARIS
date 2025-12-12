namespace Aris.Contracts.DllInjector;

/// <summary>
/// Response for DLL injection operation.
/// </summary>
public sealed record DllInjectResponse(
    /// <summary>
    /// Unique operation identifier.
    /// </summary>
    string OperationId,
    /// <summary>
    /// Operation status.
    /// </summary>
    OperationStatus Status,
    /// <summary>
    /// Operation result (present on success).
    /// </summary>
    DllInjectResultDto? Result,
    /// <summary>
    /// Error information (present on failure).
    /// </summary>
    ErrorInfo? Error,
    /// <summary>
    /// Time when operation started.
    /// </summary>
    DateTimeOffset StartedAt,
    /// <summary>
    /// Time when operation completed.
    /// </summary>
    DateTimeOffset CompletedAt
);
