using Aris.Core.Models;

namespace Aris.Core.UAsset;

/// <summary>
/// Result of a UAsset serialize or deserialize operation.
/// </summary>
public class UAssetResult
{
    /// <summary>
    /// Type of operation performed.
    /// </summary>
    public UAssetOperation Operation { get; init; }

    /// <summary>
    /// Input path (JSON for serialize, asset for deserialize).
    /// </summary>
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Output path (asset for serialize, JSON for deserialize).
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Warnings generated during the operation (non-fatal issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Files produced by the operation (asset, sidecar files, etc.).
    /// </summary>
    public IReadOnlyList<ProducedFile> ProducedFiles { get; init; } = Array.Empty<ProducedFile>();

    /// <summary>
    /// JSON schema version used.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Unreal Engine version detected or used.
    /// </summary>
    public string? UEVersion { get; init; }

    /// <summary>
    /// Truncated log excerpt from the operation (for diagnostics).
    /// </summary>
    public string? LogExcerpt { get; init; }
}
