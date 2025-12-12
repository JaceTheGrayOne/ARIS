namespace Aris.Contracts.DllInjector;

/// <summary>
/// HTTP-transport version of DllInjectResult.
/// </summary>
public sealed record DllInjectResultDto(
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
    /// Path to the injected DLL.
    /// </summary>
    string DllPath,
    /// <summary>
    /// Injection method that was used (e.g., "CreateRemoteThread").
    /// </summary>
    string Method,
    /// <summary>
    /// Whether elevation was used for this operation.
    /// </summary>
    bool ElevationUsed,
    /// <summary>
    /// Duration of the injection operation.
    /// </summary>
    string Duration,
    /// <summary>
    /// Warnings encountered during injection.
    /// </summary>
    List<string> Warnings,
    /// <summary>
    /// Excerpt from the operation log.
    /// </summary>
    string? LogExcerpt
);
