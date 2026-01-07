namespace Aris.Core.DllInjector;

/// <summary>
/// Result of a DLL injection operation performed by the native injector.
/// </summary>
public class DllInjectionResult
{
    /// <summary>
    /// Whether the injection succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The process ID that was targeted.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// The full path to the DLL that was injected.
    /// </summary>
    public required string DllPath { get; init; }

    /// <summary>
    /// The base address where the DLL was loaded in the target process.
    /// Returns IntPtr.Zero if injection failed.
    /// </summary>
    public required IntPtr LoadedModuleAddress { get; init; }

    /// <summary>
    /// Duration of the injection operation.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if injection failed. Null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The Windows error code if a Win32 API call failed. Null if no error or not applicable.
    /// </summary>
    public int? Win32ErrorCode { get; init; }

    /// <summary>
    /// Indicates whether the failure was due to insufficient privileges (elevation required).
    /// </summary>
    public bool RequiresElevation { get; init; }
}

/// <summary>
/// Native C# service for injecting DLLs into target processes using Windows API.
/// Implements CreateRemoteThread + LoadLibraryW injection technique.
/// </summary>
public interface IDllInjectionService
{
    /// <summary>
    /// Injects a DLL into the specified process using CreateRemoteThread + LoadLibraryW.
    /// </summary>
    /// <param name="processId">Target process ID.</param>
    /// <param name="dllPath">Full path to the DLL to inject (must be absolute path).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the injection operation.</returns>
    /// <exception cref="ArgumentException">If dllPath is not absolute or DLL does not exist.</exception>
    /// <exception cref="InvalidOperationException">If called on non-Windows platform.</exception>
    Task<DllInjectionResult> InjectAsync(
        int processId,
        string dllPath,
        CancellationToken cancellationToken = default);
}
