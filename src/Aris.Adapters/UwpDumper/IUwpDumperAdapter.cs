using Aris.Core.Models;
using Aris.Core.UwpDumper;

namespace Aris.Adapters.UwpDumper;

/// <summary>
/// Adapter for executing UWPDumper operations.
/// </summary>
public interface IUwpDumperAdapter
{
    /// <summary>
    /// Executes a UWPDumper dump operation.
    /// </summary>
    /// <param name="command">The UWPDumper command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result of the dump operation.</returns>
    Task<UwpDumpResult> DumpAsync(
        UwpDumpCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);

    /// <summary>
    /// Validates that UWPDumper dependencies are present and correct.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if UWPDumper is available and valid, false otherwise.</returns>
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
}
