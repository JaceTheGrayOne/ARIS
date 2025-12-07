namespace Aris.Core.Models;

/// <summary>
/// Result of an external process execution.
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// Exit code returned by the process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Captured standard output (may be truncated).
    /// </summary>
    public string StdOut { get; init; } = string.Empty;

    /// <summary>
    /// Captured standard error (may be truncated).
    /// </summary>
    public string StdErr { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the process execution.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When the process started.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// When the process exited.
    /// </summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Whether the process completed successfully (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;
}
