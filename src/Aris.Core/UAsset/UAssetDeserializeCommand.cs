namespace Aris.Core.UAsset;

/// <summary>
/// Command to deserialize a binary Unreal asset to JSON.
/// </summary>
public class UAssetDeserializeCommand
{
    /// <summary>
    /// Absolute path to the input asset file (*.uasset).
    /// Sidecar files (*.uexp, *.ubulk) are expected in the same directory.
    /// </summary>
    public string InputAssetPath { get; init; } = string.Empty;

    /// <summary>
    /// Absolute path to the output JSON file.
    /// </summary>
    public string OutputJsonPath { get; init; } = string.Empty;

    /// <summary>
    /// Game identifier (used for compatibility and key resolution).
    /// </summary>
    public string? Game { get; init; }

    /// <summary>
    /// Unreal Engine version (e.g., "5.3", "4.27").
    /// </summary>
    public string? UEVersion { get; init; }

    /// <summary>
    /// JSON schema version to use for output.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Whether to include bulk data (large binary blobs) in the JSON output.
    /// </summary>
    public bool IncludeBulkData { get; init; }

    /// <summary>
    /// Working directory for the operation (defaults to workspace temp).
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Operation timeout in seconds (overrides default from UAssetOptions).
    /// </summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Operation identifier for logging and workspace organization.
    /// </summary>
    public string OperationId { get; init; } = Guid.NewGuid().ToString("N");
}
