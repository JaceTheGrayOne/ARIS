using Aris.Core.Models;
using Aris.Core.Retoc;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Adapter for executing Retoc conversion operations.
/// </summary>
public interface IRetocAdapter
{
    /// <summary>
    /// Builds a Retoc command for preview or execution.
    /// Returns the executable path, arguments, and formatted command line.
    /// This is the single source of truth for command construction.
    /// </summary>
    /// <param name="command">The Retoc command to build.</param>
    /// <returns>Built command information (executable path, arguments, command line).</returns>
    (string ExecutablePath, string[] Arguments, string CommandLine) BuildCommand(RetocCommand command);

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
