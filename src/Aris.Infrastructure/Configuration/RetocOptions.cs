namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Retoc integration.
/// </summary>
public class RetocOptions
{
    /// <summary>
    /// Default timeout in seconds for Retoc operations.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Default compression format (e.g., "Zlib", "Oodle", "None").
    /// </summary>
    public string DefaultCompressionFormat { get; set; } = "Zlib";

    /// <summary>
    /// Default compression level (0-9, tool-specific).
    /// </summary>
    public int DefaultCompressionLevel { get; set; } = 6;

    /// <summary>
    /// Allowed additional command-line arguments (allowlist).
    /// </summary>
    public List<string> AllowedAdditionalArgs { get; set; } = new();

    /// <summary>
    /// Maximum log output size in bytes to capture.
    /// </summary>
    public int MaxLogBytes { get; set; } = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Maximum streaming output size in bytes (per stream: stdout/stderr).
    /// Aligns with ProcessRunner buffering limits.
    /// </summary>
    public int MaxStreamingOutputBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Maximum streaming output lines (per stream: stdout/stderr).
    /// Aligns with ProcessRunner buffering limits.
    /// </summary>
    public int MaxStreamingOutputLines { get; set; } = 100_000;

    /// <summary>
    /// Optional staging root override (defaults to workspace temp if empty).
    /// </summary>
    public string StagingRoot { get; set; } = string.Empty;

    /// <summary>
    /// Enable structured JSON logging for Retoc (if supported by tool).
    /// </summary>
    public bool EnableStructuredLogs { get; set; } = false;
}
