using Aris.Core.Models;
using Aris.Core.Retoc;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Adapter for executing Retoc conversion operations.
/// </summary>
public interface IRetocAdapter
{
    /// <summary>
    /// Executes a Retoc conversion operation.
    /// </summary>
    /// <param name="command">The Retoc command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result of the conversion operation.</returns>
    Task<RetocResult> ConvertAsync(
        RetocCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);

    /// <summary>
    /// Validates that Retoc dependencies are present and correct.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Retoc is available and valid, false otherwise.</returns>
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
}
