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
    /// Optional staging root override (defaults to workspace temp if empty).
    /// </summary>
    public string StagingRoot { get; set; } = string.Empty;

    /// <summary>
    /// Enable structured JSON logging for Retoc (if supported by tool).
    /// </summary>
    public bool EnableStructuredLogs { get; set; } = false;
}
