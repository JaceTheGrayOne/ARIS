namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Configuration options for UAssetAPI integration.
/// </summary>
public class UAssetOptions
{
    /// <summary>
    /// Default Unreal Engine version to use when not specified (e.g., "5.3", "4.27").
    /// </summary>
    public string DefaultUEVersion { get; set; } = "5.3";

    /// <summary>
    /// Default JSON schema version used by ARIS for asset serialization.
    /// </summary>
    public string DefaultSchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Maximum asset file size in bytes that will be processed.
    /// </summary>
    public long MaxAssetSizeBytes { get; set; } = 500 * 1024 * 1024; // 500 MB

    /// <summary>
    /// Default timeout in seconds for UAsset operations.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Enable CLI fallback mode (process isolation). Default is false (in-process).
    /// </summary>
    public bool EnableCliFallback { get; set; } = false;

    /// <summary>
    /// Retain temporary files on operation failure for debugging.
    /// </summary>
    public bool KeepTempOnFailure { get; set; } = false;

    /// <summary>
    /// Maximum log output size in bytes to capture per operation.
    /// </summary>
    public int MaxLogBytes { get; set; } = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Enable detailed JSON output logging for diagnostics.
    /// </summary>
    public bool LogJsonOutput { get; set; } = false;

    /// <summary>
    /// Optional staging root override (defaults to workspace temp if empty).
    /// </summary>
    public string StagingRoot { get; set; } = string.Empty;
}
