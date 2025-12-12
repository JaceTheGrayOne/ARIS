using Aris.Contracts.Retoc;

namespace Aris.Contracts.UAsset;

/// <summary>
/// HTTP-transport version of UAssetResult.
/// </summary>
public sealed record UAssetResultDto(
    /// <summary>
    /// Operation identifier.
    /// </summary>
    string OperationId,
    /// <summary>
    /// Input path (JSON for serialize, asset for deserialize).
    /// </summary>
    string InputPath,
    /// <summary>
    /// Output path (asset for serialize, JSON for deserialize).
    /// </summary>
    string OutputPath,
    /// <summary>
    /// Operation type (e.g., "Serialize", "Deserialize").
    /// </summary>
    string Operation,
    /// <summary>
    /// Unreal Engine version detected or used.
    /// </summary>
    string? UEVersion,
    /// <summary>
    /// JSON schema version used.
    /// </summary>
    string? SchemaVersion,
    /// <summary>
    /// Duration of the operation.
    /// </summary>
    TimeSpan Duration,
    /// <summary>
    /// Warnings generated during the operation.
    /// </summary>
    IReadOnlyList<string> Warnings,
    /// <summary>
    /// Files produced by the operation.
    /// </summary>
    IReadOnlyList<ProducedFileDto> ProducedFiles,
    /// <summary>
    /// Excerpt from the operation log.
    /// </summary>
    string? LogExcerpt
);
