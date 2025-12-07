namespace Aris.Core.Retoc;

/// <summary>
/// Result of a Retoc conversion operation.
/// </summary>
public class RetocResult
{
    /// <summary>
    /// Exit code returned by the Retoc process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Primary output path (main PAK or UTOC file).
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Output format ("pak" or "iostore").
    /// </summary>
    public string OutputFormat { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Warnings generated during the operation (non-fatal issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Files produced by the operation (PAK, UTOC, UCAS, etc.).
    /// </summary>
    public IReadOnlyList<ProducedFile> ProducedFiles { get; init; } = Array.Empty<ProducedFile>();

    /// <summary>
    /// Truncated log excerpt from the operation (for diagnostics).
    /// </summary>
    public string? LogExcerpt { get; init; }

    /// <summary>
    /// Whether the operation completed successfully (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;
}
