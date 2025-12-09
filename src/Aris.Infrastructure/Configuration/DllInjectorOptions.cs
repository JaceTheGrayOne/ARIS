namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Configuration options for DLL injector integration.
/// </summary>
public class DllInjectorOptions
{
    /// <summary>
    /// Default timeout in seconds for injection/ejection operations.
    /// </summary>
    public int DefaultTimeoutSeconds { get; init; } = 60; // 1 minute

    /// <summary>
    /// Require UAC elevation for injection operations (default true for safety).
    /// </summary>
    public bool RequireElevation { get; init; } = true;

    /// <summary>
    /// Allowed target process names (allowlist; empty = allow all except denied).
    /// Can be exact names ("Game.exe") or simple wildcards ("Game*.exe").
    /// </summary>
    public string[] AllowedTargets { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Denied target process names (denylist; takes precedence over AllowedTargets).
    /// Includes critical system processes by default.
    /// Note: explorer.exe is not denied by default as it is a common injection target
    /// for legitimate tooling. Advanced users may add it via configuration if needed.
    /// </summary>
    public string[] DeniedTargets { get; init; } = new[]
    {
        "csrss.exe",
        "smss.exe",
        "wininit.exe",
        "services.exe",
        "lsass.exe",
        "svchost.exe",
        "winlogon.exe"
    };

    /// <summary>
    /// Allowed injection methods (must be valid DllInjectionMethod enum values).
    /// Default: all three methods allowed.
    /// </summary>
    public string[] AllowedMethods { get; init; } = new[]
    {
        "CreateRemoteThread",
        "ApcQueue",
        "ManualMap"
    };

    /// <summary>
    /// Maximum log output size in bytes per operation.
    /// </summary>
    public long MaxLogBytes { get; init; } = 5L * 1024 * 1024; // 5 MB

    /// <summary>
    /// Optional staging root override (defaults to workspace temp if null/empty).
    /// </summary>
    public string? StagingRoot { get; init; }

    /// <summary>
    /// Retain temporary files on operation failure for debugging.
    /// </summary>
    public bool KeepTempOnFailure { get; init; } = false;
}
