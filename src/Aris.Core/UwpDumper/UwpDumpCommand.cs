namespace Aris.Core.UwpDumper;

/// <summary>
/// Command to execute a UWPDumper operation.
/// Immutable domain model representing all parameters for a UWPDumper invocation.
/// </summary>
public class UwpDumpCommand
{
    /// <summary>
    /// Package Family Name (PFN) of the target UWP application.
    /// </summary>
    public string PackageFamilyName { get; init; } = string.Empty;

    /// <summary>
    /// Application ID or App User Model ID (optional; used for disambiguation if multiple apps share a PFN).
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// Absolute path to the output directory or archive where dump artifacts will be written.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Dump operation mode.
    /// </summary>
    public UwpDumpMode Mode { get; init; }

    /// <summary>
    /// Include symbol files in the dump (if available and supported).
    /// </summary>
    public bool IncludeSymbols { get; init; }

    /// <summary>
    /// Working directory for the operation (defaults to workspace temp/uwp-{operationId}/ if not specified).
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Operation timeout in seconds (overrides default from UwpDumperOptions).
    /// </summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Operation identifier for logging and workspace organization.
    /// </summary>
    public string OperationId { get; init; } = Guid.NewGuid().ToString("N");
}
