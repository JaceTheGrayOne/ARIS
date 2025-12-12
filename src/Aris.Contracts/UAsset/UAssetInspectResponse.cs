namespace Aris.Contracts.UAsset;

/// <summary>
/// Response for UAsset inspect operation.
/// </summary>
public sealed record UAssetInspectResponse(
    /// <summary>
    /// Unique operation identifier.
    /// </summary>
    string OperationId,
    /// <summary>
    /// Operation status.
    /// </summary>
    OperationStatus Status,
    /// <summary>
    /// Inspection result (present on success).
    /// </summary>
    UAssetInspectionDto? Result,
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
