using System.Diagnostics;
using System.Text;
using Aris.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aris.Infrastructure.Process;

public class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;
    private const int MaxOutputBytes = 10 * 1024 * 1024; // 10 MB per stream
    private const int MaxOutputLines = 100000; // Safety limit on line count

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> ExecuteAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        int timeoutSeconds = 0,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        // TODO: Add command-line redaction for sensitive data (keys, tokens, etc.)
        _logger.LogInformation(
            "Starting process: {Executable} {Arguments}",
            executablePath,
            arguments);

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            _logger.LogDebug("Working directory: {WorkingDirectory}", workingDirectory);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                processStartInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };

        var stdOutBuilder = new BoundedStringBuilder(MaxOutputBytes, MaxOutputLines);
        var stdErrBuilder = new BoundedStringBuilder(MaxOutputBytes, MaxOutputLines);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdOutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdErrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var processId = process.Id;
        _logger.LogDebug("Process started with PID {ProcessId}", processId);

        try
        {
            var hasTimeout = timeoutSeconds > 0;
            var timeout = hasTimeout ? TimeSpan.FromSeconds(timeoutSeconds) : Timeout.InfiniteTimeSpan;

            var completedTask = await WaitForExitAsync(process, timeout, cancellationToken);

            if (!completedTask)
            {
                _logger.LogWarning(
                    "Process {ProcessId} timed out after {TimeoutSeconds}s or was cancelled, killing process",
                    processId,
                    timeoutSeconds);

                KillProcessTree(process);

                throw new TimeoutException(
                    $"Process {Path.GetFileName(executablePath)} timed out after {timeoutSeconds} seconds or was cancelled.");
            }

            var endTime = DateTimeOffset.UtcNow;
            var duration = endTime - startTime;
            var exitCode = process.ExitCode;

            _logger.LogInformation(
                "Process {ProcessId} exited with code {ExitCode} after {Duration}ms",
                processId,
                exitCode,
                duration.TotalMilliseconds);

            return new ProcessResult
            {
                ExitCode = exitCode,
                StdOut = stdOutBuilder.ToString(),
                StdErr = stdErrBuilder.ToString(),
                Duration = duration,
                StartTime = startTime,
                EndTime = endTime
            };
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            _logger.LogError(ex, "Error executing process {Executable}", executablePath);
            throw;
        }
    }

    private static async Task<bool> WaitForExitAsync(
        System.Diagnostics.Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        void ProcessExited(object? sender, EventArgs e) => tcs.TrySetResult(true);

        process.EnableRaisingEvents = true;
        process.Exited += ProcessExited;

        try
        {
            if (process.HasExited)
            {
                return true;
            }

            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            var completedTask = timeout == Timeout.InfiniteTimeSpan
                ? await tcs.Task
                : await Task.WhenAny(tcs.Task, Task.Delay(timeout, CancellationToken.None)) == tcs.Task && await tcs.Task;

            return completedTask;
        }
        finally
        {
            process.Exited -= ProcessExited;
        }
    }

    private void KillProcessTree(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                _logger.LogDebug("Killed process tree for PID {ProcessId}", process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to kill process tree for PID {ProcessId}", process.Id);
        }
    }

    private class BoundedStringBuilder
    {
        private readonly StringBuilder _builder = new();
        private readonly int _maxBytes;
        private readonly int _maxLines;
        private int _currentBytes;
        private int _currentLines;
        private bool _truncated;

        public BoundedStringBuilder(int maxBytes, int maxLines)
        {
            _maxBytes = maxBytes;
            _maxLines = maxLines;
        }

        public void AppendLine(string line)
        {
            if (_truncated) return;

            var lineBytes = Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

            if (_currentBytes + lineBytes > _maxBytes || _currentLines >= _maxLines)
            {
                _builder.AppendLine("... [output truncated due to size limits]");
                _truncated = true;
                return;
            }

            _builder.AppendLine(line);
            _currentBytes += lineBytes;
            _currentLines++;
        }

        public override string ToString() => _builder.ToString();
    }
}
