namespace Aris.Core.DllInjector;

/// <summary>
/// Result of a DLL ejection (unload) operation.
/// </summary>
public class DllEjectResult
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
    /// Module name that was ejected.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the ejection operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the DLL was loaded in the target process before the eject operation.
    /// </summary>
    public bool WasLoadedBefore { get; init; }

    /// <summary>
    /// Whether the DLL is confirmed unloaded after the operation.
    /// </summary>
    public bool IsUnloaded { get; init; }

    /// <summary>
    /// Warnings encountered during ejection (non-fatal issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Excerpt from the operation log (bounded by MaxLogBytes).
    /// </summary>
    public string? LogExcerpt { get; init; }
}
