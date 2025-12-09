using Aris.Core.DllInjector;
using Aris.Core.Models;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Adapter for executing DLL injection and ejection operations.
/// </summary>
public interface IDllInjectorAdapter
{
    /// <summary>
    /// Injects a DLL into a target process.
    /// </summary>
    /// <param name="command">The injection command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result of the injection operation.</returns>
    Task<DllInjectResult> InjectAsync(
        DllInjectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);

    /// <summary>
    /// Ejects (unloads) a DLL from a target process.
    /// </summary>
    /// <param name="command">The ejection command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result of the ejection operation.</returns>
    Task<DllEjectResult> EjectAsync(
        DllEjectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);

    /// <summary>
    /// Validates that DLL injector dependencies are present and correct.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if DLL injector is available and valid, false otherwise.</returns>
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
}
