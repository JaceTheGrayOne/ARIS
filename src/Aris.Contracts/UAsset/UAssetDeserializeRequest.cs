namespace Aris.Contracts.UAsset;

/// <summary>
/// Request for UAsset deserialize operation (binary asset to JSON).
/// </summary>
public sealed record UAssetDeserializeRequest(
    /// <summary>
    /// Absolute path to the input asset file (*.uasset).
    /// </summary>
    string InputAssetPath,
    /// <summary>
    /// Absolute path to the output JSON file.
    /// </summary>
    string OutputJsonPath,
    /// <summary>
    /// Whether to include bulk data in the JSON output.
    /// </summary>
    bool IncludeBulkData,
    /// <summary>
    /// Game identifier (optional).
    /// </summary>
    string? Game,
    /// <summary>
    /// Unreal Engine version (optional, e.g., "5.3", "4.27").
    /// </summary>
    string? UEVersion,
    /// <summary>
    /// JSON schema version to use for output (optional).
    /// </summary>
    string? SchemaVersion,
    /// <summary>
    /// Operation timeout in seconds (optional).
    /// </summary>
    int? TimeoutSeconds
);
