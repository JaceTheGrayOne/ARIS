using Aris.Contracts.Retoc;

namespace Aris.Contracts.UwpDumper;

/// <summary>
/// HTTP-transport version of UwpDumpResult.
/// </summary>
public sealed record UwpDumpResultDto(
    /// <summary>
    /// Operation identifier.
    /// </summary>
    string OperationId,
    /// <summary>
    /// Package Family Name of the dumped UWP application.
    /// </summary>
    string PackageFamilyName,
    /// <summary>
    /// Application ID if one was specified or resolved.
    /// </summary>
    string? ApplicationId,
    /// <summary>
    /// Root path where dump artifacts were written.
    /// </summary>
    string OutputPath,
    /// <summary>
    /// Dump operation mode (e.g., "FullDump", "MetadataOnly", "ValidateOnly").
    /// </summary>
    string Mode,
    /// <summary>
    /// Duration of the dump operation.
    /// </summary>
    TimeSpan Duration,
    /// <summary>
    /// Warnings generated during the operation.
    /// </summary>
    IReadOnlyList<string> Warnings,
    /// <summary>
    /// Files and directories produced by the dump operation.
    /// </summary>
    IReadOnlyList<ProducedFileDto> Artifacts,
    /// <summary>
    /// Excerpt from the operation log.
    /// </summary>
    string? LogExcerpt
);
