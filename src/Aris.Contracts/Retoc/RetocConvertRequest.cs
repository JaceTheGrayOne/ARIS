namespace Aris.Contracts.Retoc;

/// <summary>
/// Request for Retoc convert operation.
/// </summary>
public sealed record RetocConvertRequest(
    /// <summary>
    /// Input path (absolute or workspace-relative).
    /// </summary>
    string InputPath,
    /// <summary>
    /// Desired output path.
    /// </summary>
    string OutputPath,
    /// <summary>
    /// Conversion mode (e.g., "PakToIoStore", "IoStoreToPak", "Repack").
    /// </summary>
    string Mode,
    /// <summary>
    /// Game identifier (optional).
    /// </summary>
    string? Game,
    /// <summary>
    /// Unreal Engine version (optional).
    /// </summary>
    string? UEVersion,
    /// <summary>
    /// Compression format (optional).
    /// </summary>
    string? CompressionFormat,
    /// <summary>
    /// Compression level (optional).
    /// </summary>
    int? CompressionLevel,
    /// <summary>
    /// Timeout in seconds (optional).
    /// </summary>
    int? TimeoutSeconds,
    /// <summary>
    /// Mount point to encryption key mappings (optional).
    /// </summary>
    Dictionary<string, string>? MountKeys,
    /// <summary>
    /// Include file filters (optional).
    /// </summary>
    List<string>? IncludeFilters,
    /// <summary>
    /// Exclude file filters (optional).
    /// </summary>
    List<string>? ExcludeFilters
);
