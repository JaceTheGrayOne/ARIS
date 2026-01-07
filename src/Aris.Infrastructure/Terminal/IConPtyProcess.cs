namespace Aris.Infrastructure.Terminal;

/// <summary>
/// Abstraction for a process attached to a Windows pseudo-console (ConPTY).
/// Provides proper TTY environment for applications that require terminal support
/// (e.g., indicatif progress bars in Rust applications).
/// </summary>
public interface IConPtyProcess : IDisposable
{
    /// <summary>
    /// Gets the process ID, or -1 if not started.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// Gets whether the process has been started.
    /// </summary>
    bool HasStarted { get; }

    /// <summary>
    /// Gets whether the process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Starts the process attached to a pseudo-console.
    /// </summary>
    /// <param name="executable">Path to the executable.</param>
    /// <param name="arguments">Command line arguments.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="terminalWidth">Terminal width in columns (default 120).</param>
    /// <param name="terminalHeight">Terminal height in rows (default 30).</param>
    Task StartAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        short terminalWidth = 120,
        short terminalHeight = 30);

    /// <summary>
    /// Reads output from the pseudo-console as an async stream of byte arrays.
    /// The output is raw VT/ANSI data suitable for xterm rendering.
    /// With ConPTY, stdout and stderr are merged into a single stream.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of byte arrays containing terminal output.</returns>
    IAsyncEnumerable<byte[]> ReadOutputAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes input to the pseudo-console.
    /// </summary>
    /// <param name="data">Data to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteInputAsync(byte[] data, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the process to exit and returns the exit code.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process exit code.</returns>
    Task<int> WaitForExitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Kills the process and its descendants.
    /// </summary>
    void Kill();

    /// <summary>
    /// Resizes the pseudo-console.
    /// </summary>
    /// <param name="width">New width in columns.</param>
    /// <param name="height">New height in rows.</param>
    void Resize(short width, short height);
}
