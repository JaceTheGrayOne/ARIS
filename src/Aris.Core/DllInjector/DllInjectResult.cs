namespace Aris.Core.DllInjector;

/// <summary>
/// Result of a DLL injection operation.
/// </summary>
public class DllInjectResult
{
    /// <summary>
    /// Operation identifier for tracking and correlation.
    /// </summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>
    /// Target process ID.
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Target process name.
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// Path to the injected DLL.
    /// </summary>
    public string DllPath { get; init; } = string.Empty;

    /// <summary>
    /// Injection method that was used.
    /// </summary>
    public DllInjectionMethod Method { get; init; }

    /// <summary>
    /// Whether elevation was used for this operation.
    /// </summary>
    public bool ElevationUsed { get; init; }

    /// <summary>
    /// Duration of the injection operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Warnings encountered during injection (non-fatal issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Excerpt from the operation log (bounded by MaxLogBytes).
    /// </summary>
    public string? LogExcerpt { get; init; }
}
