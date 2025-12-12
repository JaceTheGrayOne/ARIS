namespace Aris.Contracts.DllInjector;

/// <summary>
/// HTTP-transport version of DllEjectResult.
/// </summary>
public sealed record DllEjectResultDto(
    /// <summary>
    /// Operation identifier.
    /// </summary>
    string OperationId,
    /// <summary>
    /// Target process ID.
    /// </summary>
    int ProcessId,
    /// <summary>
    /// Target process name.
    /// </summary>
    string ProcessName,
    /// <summary>
    /// Module name that was ejected.
    /// </summary>
    string ModuleName,
    /// <summary>
    /// Whether the DLL was loaded before the eject operation.
    /// </summary>
    bool WasLoadedBefore,
    /// <summary>
    /// Whether the DLL is confirmed unloaded after the operation.
    /// </summary>
    bool IsUnloaded,
    /// <summary>
    /// Duration of the ejection operation.
    /// </summary>
    string Duration,
    /// <summary>
    /// Warnings encountered during ejection.
    /// </summary>
    List<string> Warnings,
    /// <summary>
    /// Excerpt from the operation log.
    /// </summary>
    string? LogExcerpt
);
