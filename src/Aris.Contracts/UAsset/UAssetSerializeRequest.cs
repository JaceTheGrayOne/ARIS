namespace Aris.Contracts.UAsset;

/// <summary>
/// Request for UAsset serialize operation (JSON to binary asset).
/// </summary>
public sealed record UAssetSerializeRequest(
    /// <summary>
    /// Absolute path to the input JSON file.
    /// </summary>
    string InputJsonPath,
    /// <summary>
    /// Absolute path to the output asset file (*.uasset).
    /// </summary>
    string OutputAssetPath,
    /// <summary>
    /// Game identifier (optional).
    /// </summary>
    string? Game,
    /// <summary>
    /// Unreal Engine version (optional, e.g., "5.3", "4.27").
    /// </summary>
    string? UEVersion,
    /// <summary>
    /// JSON schema version used by ARIS (optional).
    /// </summary>
    string? SchemaVersion,
    /// <summary>
    /// Compression format (optional, e.g., "Zlib", "Oodle", "None").
    /// </summary>
    string? CompressionFormat,
    /// <summary>
    /// Compression level (optional, tool-specific, typically 0-9).
    /// </summary>
    int? CompressionLevel,
    /// <summary>
    /// Operation timeout in seconds (optional).
    /// </summary>
    int? TimeoutSeconds
);
