namespace Aris.Core.DllInjector;

/// <summary>
/// Command to eject (unload) a DLL from a target process.
/// Immutable domain model representing all parameters for a DLL ejection operation.
/// </summary>
public class DllEjectCommand
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
    /// Module name or path of the DLL to eject (e.g., "aris_payload.dll").
    /// Resolved by adapter to actual module handle in target process.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// Working directory for the operation (defaults to workspace/temp/eject-{operationId}/).
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
