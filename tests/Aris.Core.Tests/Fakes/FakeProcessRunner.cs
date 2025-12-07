using Aris.Core.Models;
using Aris.Infrastructure.Process;

namespace Aris.Core.Tests.Fakes;

/// <summary>
/// Fake IProcessRunner for testing. Captures calls and returns configurable results.
/// </summary>
public class FakeProcessRunner : IProcessRunner
{
    public string? LastExecutablePath { get; private set; }
    public string? LastArguments { get; private set; }
    public string? LastWorkingDirectory { get; private set; }
    public int LastTimeoutSeconds { get; private set; }
    public IReadOnlyDictionary<string, string>? LastEnvironmentVariables { get; private set; }

    public ProcessResult ResultToReturn { get; set; } = new ProcessResult
    {
        ExitCode = 0,
        StdOut = "Fake stdout",
        StdErr = string.Empty,
        Duration = TimeSpan.FromSeconds(1),
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
    };

    public Exception? ExceptionToThrow { get; set; }

    public Task<ProcessResult> ExecuteAsync(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        int timeoutSeconds = 0,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        LastExecutablePath = executablePath;
        LastArguments = arguments;
        LastWorkingDirectory = workingDirectory;
        LastTimeoutSeconds = timeoutSeconds;
        LastEnvironmentVariables = environmentVariables;

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(ResultToReturn);
    }
}
