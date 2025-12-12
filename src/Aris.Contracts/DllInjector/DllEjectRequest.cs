namespace Aris.Contracts.DllInjector;

/// <summary>
/// Request for DLL ejection (unload) operation.
/// </summary>
public sealed record DllEjectRequest(
    /// <summary>
    /// Target process ID (optional, either this or ProcessName must be provided).
    /// </summary>
    int? ProcessId,
    /// <summary>
    /// Target process name (optional, either this or ProcessId must be provided).
    /// </summary>
    string? ProcessName,
    /// <summary>
    /// Module name or path of the DLL to eject.
    /// </summary>
    string ModuleName,
    /// <summary>
    /// Optional override for elevation requirement.
    /// </summary>
    bool? RequireElevation
);
