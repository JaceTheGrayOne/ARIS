namespace Aris.Core.UAsset;

/// <summary>
/// Command to serialize JSON to a binary Unreal asset.
/// </summary>
public class UAssetSerializeCommand
{
    /// <summary>
    /// Absolute path to the input JSON file.
    /// </summary>
    public string InputJsonPath { get; init; } = string.Empty;

    /// <summary>
    /// Absolute path to the output asset file (*.uasset).
    /// </summary>
    public string OutputAssetPath { get; init; } = string.Empty;

    /// <summary>
    /// Game identifier (used for compatibility and key resolution).
    /// </summary>
    public string? Game { get; init; }

    /// <summary>
    /// Unreal Engine version (e.g., "5.3", "4.27").
    /// </summary>
    public string? UEVersion { get; init; }

    /// <summary>
    /// JSON schema version used by ARIS.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Compression format (e.g., "Zlib", "Oodle", "None").
    /// </summary>
    public string? CompressionFormat { get; init; }

    /// <summary>
    /// Compression level (tool-specific, typically 0-9).
    /// </summary>
    public int? CompressionLevel { get; init; }

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
