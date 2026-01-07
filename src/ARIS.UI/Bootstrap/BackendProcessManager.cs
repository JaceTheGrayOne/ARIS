using System.Diagnostics;
using System.IO;

namespace ARIS.UI.Bootstrap;

/// <summary>
/// Manages the backend process lifecycle - starting, stopping, and URL discovery.
/// </summary>
public sealed class BackendProcessManager : IDisposable
{
    private const string UrlPrefix = "ARIS_BACKEND_URL=";
    private static readonly TimeSpan DefaultUrlTimeout = TimeSpan.FromSeconds(10);

    private Process? _backendProcess;
    private readonly object _processLock = new();
    private bool _disposed;

    /// <summary>
    /// Gets whether the backend process is currently running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_processLock)
            {
                return _backendProcess != null && !_backendProcess.HasExited;
            }
        }
    }

    /// <summary>
    /// Builds a ProcessStartInfo for the backend executable with hidden console.
    /// </summary>
    public static ProcessStartInfo BuildProcessStartInfo(string executablePath, string? workingDirectory = null)
    {
        return new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(executablePath) ?? string.Empty,
            // Set environment variable for dynamic port binding
            Environment =
            {
                ["ASPNETCORE_URLS"] = "http://127.0.0.1:0",
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            }
        };
    }

    /// <summary>
    /// Parses the backend URL from a stdout line.
    /// </summary>
    public static string? ParseUrlFromStdout(string? line)
    {
        if (string.IsNullOrEmpty(line))
            return null;

        if (!line.StartsWith(UrlPrefix, StringComparison.Ordinal))
            return null;

        return line.Substring(UrlPrefix.Length).Trim();
    }

    /// <summary>
    /// Starts the backend process and waits for it to announce its URL.
    /// </summary>
    /// <param name="executablePath">Path to Aris.Hosting.exe</param>
    /// <param name="timeout">Timeout for URL announcement (default 10 seconds)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The announced backend URL</returns>
    public async Task<string> StartAndWaitForUrlAsync(
        string executablePath,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= DefaultUrlTimeout;

        var psi = BuildProcessStartInfo(executablePath);
        var urlTcs = new TaskCompletionSource<string>();

        lock (_processLock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackendProcessManager));

            _backendProcess = new Process { StartInfo = psi };
            _backendProcess.OutputDataReceived += (_, e) =>
            {
                var url = ParseUrlFromStdout(e.Data);
                if (url != null)
                {
                    urlTcs.TrySetResult(url);
                }
            };
            _backendProcess.ErrorDataReceived += (_, e) =>
            {
                // Log stderr but don't fail on it
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Debug.WriteLine($"Backend stderr: {e.Data}");
                }
            };
        }

        try
        {
            _backendProcess.Start();
            _backendProcess.BeginOutputReadLine();
            _backendProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            throw new BackendStartException($"Failed to start backend process: {ex.Message}", ex);
        }

        // Wait for URL with timeout
        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var completedTask = await Task.WhenAny(
                urlTcs.Task,
                Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == urlTcs.Task)
            {
                return await urlTcs.Task;
            }

            // Check if process exited prematurely
            if (_backendProcess.HasExited)
            {
                throw new BackendStartException(
                    $"Backend process exited with code {_backendProcess.ExitCode} before announcing URL.");
            }

            throw new BackendUrlTimeoutException(timeout.Value);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Kill the process on timeout
            Stop();
            throw new BackendUrlTimeoutException(timeout.Value);
        }
    }

    /// <summary>
    /// Stops the backend process gracefully, or kills it if necessary.
    /// </summary>
    public void Stop()
    {
        lock (_processLock)
        {
            if (_backendProcess == null || _backendProcess.HasExited)
                return;

            try
            {
                // Try graceful shutdown first
                _backendProcess.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore errors during shutdown
            }
            finally
            {
                try
                {
                    _backendProcess.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _backendProcess = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
    }
}
