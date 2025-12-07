using Aris.Core.Models;

namespace Aris.Infrastructure.Process;

/// <summary>
/// Executes external processes with timeout, cancellation, and output capture.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Executes an external process and captures its output.
    /// </summary>
    /// <param name="executablePath">Full path to the executable.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="timeoutSeconds">Timeout in seconds (0 or negative means no timeout).</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <param name="cancellationToken">Cancellation token to abort execution.</param>
    /// <returns>Process execution result.</returns>
    Task<ProcessResult> ExecuteAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        int timeoutSeconds = 0,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default);
}
