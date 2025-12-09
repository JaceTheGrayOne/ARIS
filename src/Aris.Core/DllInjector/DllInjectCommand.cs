namespace Aris.Core.DllInjector;

/// <summary>
/// Command to inject a DLL into a target process.
/// Immutable domain model representing all parameters for a DLL injection operation.
/// </summary>
public class DllInjectCommand
{
    /// <summary>
    /// Target process ID.
    /// Exactly one of ProcessId or ProcessName must be provided; resolved by validator.
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Target process name (e.g., "Game.exe").
    /// Exactly one of ProcessId or ProcessName must be provided; resolved by validator.
    /// </summary>
    public string? ProcessName { get; init; }

    /// <summary>
    /// Absolute path to the payload DLL to inject.
    /// Must be under workspace/input/payloads/ or dependencies/payloads/.
    /// </summary>
    public string DllPath { get; init; } = string.Empty;

    /// <summary>
    /// Injection method to use.
    /// </summary>
    public DllInjectionMethod Method { get; init; } = DllInjectionMethod.CreateRemoteThread;

    /// <summary>
    /// Override for elevation requirement.
    /// Null = use DllInjectorOptions.RequireElevation; true/false overrides it.
    /// </summary>
    public bool? RequireElevationOverride { get; init; }

    /// <summary>
    /// Arguments to pass to the DLL entrypoint (must be allowlisted).
    /// </summary>
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Working directory for the operation (defaults to workspace/temp/inject-{operationId}/).
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Operation timeout in seconds (overrides default from DllInjectorOptions).
    /// </summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Operation identifier for logging and workspace organization.
    /// </summary>
    public string OperationId { get; init; } = Guid.NewGuid().ToString("N");
}
