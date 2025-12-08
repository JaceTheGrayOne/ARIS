using Aris.Core.Models;

namespace Aris.Core.UwpDumper;

/// <summary>
/// Result of a UWPDumper operation.
/// </summary>
public class UwpDumpResult
{
    /// <summary>
    /// Operation identifier.
    /// </summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>
    /// Package Family Name of the dumped UWP application.
    /// </summary>
    public string PackageFamilyName { get; init; } = string.Empty;

    /// <summary>
    /// Application ID if one was specified or resolved.
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// Root path where dump artifacts were written.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the dump operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Warnings generated during the operation (non-fatal issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Files and directories produced by the dump operation.
    /// FileType values may include: "Headers", "Metadata", "Symbols", "Manifest", "PackageLayout".
    /// </summary>
    public IReadOnlyList<ProducedFile> Artifacts { get; init; } = Array.Empty<ProducedFile>();

    /// <summary>
    /// Truncated log excerpt from the operation (for diagnostics).
    /// </summary>
    public string? LogExcerpt { get; init; }
}
