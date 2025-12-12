namespace Aris.Contracts.DllInjector;

/// <summary>
/// Request for DLL injection operation.
/// </summary>
public sealed record DllInjectRequest(
    /// <summary>
    /// Target process ID (optional, either this or ProcessName must be provided).
    /// </summary>
    int? ProcessId,
    /// <summary>
    /// Target process name (optional, either this or ProcessId must be provided).
    /// </summary>
    string? ProcessName,
    /// <summary>
    /// Absolute path to the payload DLL to inject.
    /// </summary>
    string DllPath,
    /// <summary>
    /// Injection method (e.g., "CreateRemoteThread", "ApcQueue", "ManualMap").
    /// </summary>
    string Method,
    /// <summary>
    /// Optional override for elevation requirement.
    /// </summary>
    bool? RequireElevation,
    /// <summary>
    /// Optional arguments to pass to the DLL entrypoint.
    /// </summary>
    List<string>? Arguments
);
