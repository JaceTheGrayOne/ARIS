namespace Aris.Core.Retoc;

/// <summary>
/// Command to execute a Retoc conversion operation.
/// Immutable domain model representing all parameters for a Retoc invocation.
/// </summary>
public class RetocCommand
{
    /// <summary>
    /// Absolute path to the input file (PAK, UTOC, or UCAS).
    /// </summary>
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Absolute path to the output file or directory.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Conversion mode.
    /// </summary>
    public RetocMode Mode { get; init; }

    /// <summary>
    /// AES mount key identifiers or values (resolved via KeyStore).
    /// </summary>
    public IReadOnlyList<string> MountKeys { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Game version identifier (used for key resolution and compatibility).
    /// </summary>
    public string? GameVersion { get; init; }

    /// <summary>
    /// Unreal Engine version (e.g., "5.3", "4.27").
    /// </summary>
    public string? UEVersion { get; init; }

    /// <summary>
    /// Compression format (e.g., "Zlib", "Oodle", "None").
    /// </summary>
    public string? CompressionFormat { get; init; }

    /// <summary>
    /// Compression level (tool-specific, typically 0-9 or named levels).
    /// </summary>
    public int? CompressionLevel { get; init; }

    /// <summary>
    /// Compression block size in bytes.
    /// </summary>
    public int? CompressionBlockSize { get; init; }

    /// <summary>
    /// Include filters (glob patterns for selective repacking).
    /// </summary>
    public IReadOnlyList<string> IncludeFilters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Exclude filters (glob patterns for selective repacking).
    /// </summary>
    public IReadOnlyList<string> ExcludeFilters { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Additional command-line arguments (must pass allowlist validation).
    /// </summary>
    public IReadOnlyList<string> AdditionalArgs { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Working directory for the operation (defaults to workspace temp/retoc-{operationId}/).
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Operation timeout in seconds (overrides default from RetocOptions).
    /// </summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Operation identifier for logging and workspace organization.
    /// </summary>
    public string OperationId { get; init; } = Guid.NewGuid().ToString("N");
}
