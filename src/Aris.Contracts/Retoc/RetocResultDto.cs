namespace Aris.Contracts.Retoc;

/// <summary>
/// HTTP-transport version of RetocResult.
/// </summary>
public sealed record RetocResultDto(
    /// <summary>
    /// Exit code from the Retoc process.
    /// </summary>
    int ExitCode,
    /// <summary>
    /// Path to the output produced by Retoc.
    /// </summary>
    string OutputPath,
    /// <summary>
    /// Output format (e.g., "IoStore", "Pak").
    /// </summary>
    string? OutputFormat,
    /// <summary>
    /// Duration of the operation.
    /// </summary>
    TimeSpan Duration,
    /// <summary>
    /// Warnings emitted during the operation.
    /// </summary>
    IReadOnlyList<string> Warnings,
    /// <summary>
    /// Files produced by the operation.
    /// </summary>
    IReadOnlyList<ProducedFileDto> ProducedFiles,
    /// <summary>
    /// Schema version used.
    /// </summary>
    string? SchemaVersion,
    /// <summary>
    /// Unreal Engine version targeted.
    /// </summary>
    string? UEVersion,
    /// <summary>
    /// Excerpt from the operation log.
    /// </summary>
    string? LogExcerpt
);
