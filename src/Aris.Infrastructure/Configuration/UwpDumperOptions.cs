namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Configuration options for UWPDumper integration.
/// </summary>
public class UwpDumperOptions
{
    /// <summary>
    /// Default timeout in seconds for UWPDumper operations.
    /// </summary>
    public int DefaultTimeoutSeconds { get; init; } = 300; // 5 minutes

    /// <summary>
    /// Require UAC elevation for UWPDumper operations (default true).
    /// </summary>
    public bool RequireElevation { get; init; } = true;

    /// <summary>
    /// Allowed dump modes (must match UwpDumpMode enum values).
    /// </summary>
    public string[] AllowedModes { get; init; } = new[] { "FullDump", "MetadataOnly", "ValidateOnly" };

    /// <summary>
    /// Maximum log output size in bytes to capture per operation.
    /// </summary>
    public long MaxLogBytes { get; init; } = 5L * 1024 * 1024; // 5 MB

    /// <summary>
    /// Optional staging root override (defaults to workspace temp if empty).
    /// </summary>
    public string? StagingRoot { get; init; }

    /// <summary>
    /// Retain temporary files on operation failure for debugging.
    /// </summary>
    public bool KeepTempOnFailure { get; init; } = false;
}
