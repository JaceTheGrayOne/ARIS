using System.Diagnostics;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Process;
using Aris.Infrastructure.Tools;
using Aris.Tools.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Adapter for executing DLL injection and ejection operations.
/// </summary>
public class DllInjectorAdapter : IDllInjectorAdapter
{
    private readonly IProcessRunner _processRunner;
    private readonly IProcessResolver _processResolver;
    private readonly IDependencyValidator _dependencyValidator;
    private readonly ILogger<DllInjectorAdapter> _logger;
    private readonly DllInjectorOptions _options;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly string _dllInjectorExePath;

    public DllInjectorAdapter(
        IProcessRunner processRunner,
        IProcessResolver processResolver,
        IDependencyValidator dependencyValidator,
        ILogger<DllInjectorAdapter> logger,
        IOptions<DllInjectorOptions> options,
        IOptions<WorkspaceOptions> workspaceOptions)
    {
        _processRunner = processRunner;
        _processResolver = processResolver;
        _dependencyValidator = dependencyValidator;
        _logger = logger;
        _options = options.Value;
        _workspaceOptions = workspaceOptions.Value;

        var manifest = ToolManifestLoader.Load();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
        var dllInjectorEntry = manifest.Tools.FirstOrDefault(t => t.Id == "dllinjector")
            ?? throw new DependencyMissingError("dllinjector", "DLL injector entry not found in tool manifest");

        _dllInjectorExePath = Path.Combine(toolsRoot, dllInjectorEntry.RelativePath);
    }

    public async Task<DllInjectResult> InjectAsync(
        DllInjectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting DLL injection: method={Method}, dll={DllPath}, operationId={OperationId}",
            command.Method,
            Path.GetFileName(command.DllPath),
            operationId);

        ReportProgress(progress, "resolving", "Resolving target process", 0);

        var workspaceRoot = GetWorkspaceRoot();
        var pid = DllInjectCommandValidator.ValidateAndResolveTarget(
            command,
            _options,
            workspaceRoot,
            _processResolver);

        var processName = GetProcessName(pid);

        _logger.LogInformation(
            "Target process resolved: pid={ProcessId}, name={ProcessName}",
            pid,
            processName);

        ReportProgress(progress, "validating", "Validating payload", 25);

        var workingDirectory = DetermineWorkingDirectory(command.WorkingDirectory, workspaceRoot, operationId, "inject");
        Directory.CreateDirectory(workingDirectory);

        var timeoutSeconds = command.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;
        var requireElevation = command.RequireElevationOverride ?? _options.RequireElevation;

        var arguments = BuildInjectArguments(command, pid);

        _logger.LogDebug(
            "DLL injector command: {Executable} [arguments redacted for security]",
            _dllInjectorExePath);

        ReportProgress(progress, "injecting", "Injecting DLL", 50);

        var startTime = DateTimeOffset.UtcNow;
        ProcessResult processResult;

        try
        {
            processResult = await _processRunner.ExecuteAsync(
                _dllInjectorExePath,
                arguments,
                workingDirectory,
                timeoutSeconds,
                environmentVariables: null,
                cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "DLL injection timed out after {TimeoutSeconds} seconds", timeoutSeconds);

            throw new ToolExecutionError("dllinjector", -1, $"DLL injection timed out after {timeoutSeconds} seconds", ex)
            {
                RemediationHint = "Try increasing the timeout in DllInjectorOptions or check if the target process is responsive."
            };
        }

        var endTime = DateTimeOffset.UtcNow;

        WriteOperationLog(workspaceRoot, operationId, processResult, arguments);

        ReportProgress(progress, "verifying", "Verifying injection", 75);

        if (processResult.ExitCode != 0)
        {
            if (IsElevationError(processResult) && requireElevation)
            {
                _logger.LogError(
                    "DLL injection failed due to elevation requirement, exit code {ExitCode}",
                    processResult.ExitCode);

                throw new ElevationRequiredError("DLL injection requires elevation (run as administrator)")
                {
                    OperationId = operationId,
                    RemediationHint = "Run ARIS as administrator or disable RequireElevation in DllInjectorOptions."
                };
            }

            _logger.LogError(
                "DLL injection failed with exit code {ExitCode}, stderr: {StdErr}",
                processResult.ExitCode,
                TruncateForLog(processResult.StdErr, 500));

            throw new ToolExecutionError("dllinjector", processResult.ExitCode, "DLL injection operation failed")
            {
                CommandLine = "[command redacted]",
                StandardOutput = TruncateForLog(processResult.StdOut, _options.MaxLogBytes),
                StandardError = TruncateForLog(processResult.StdErr, _options.MaxLogBytes),
                RemediationHint = "Check the DLL injector logs for details. Ensure the target process and payload DLL are compatible (both x64)."
            };
        }

        ReportProgress(progress, "finalizing", "Finalizing", 100);

        _logger.LogInformation(
            "DLL injection completed successfully in {DurationMs}ms, operationId={OperationId}",
            processResult.Duration.TotalMilliseconds,
            operationId);

        var result = new DllInjectResult
        {
            OperationId = operationId,
            ProcessId = pid,
            ProcessName = processName,
            DllPath = command.DllPath,
            Method = command.Method,
            ElevationUsed = requireElevation,
            Duration = processResult.Duration,
            Warnings = ExtractWarnings(processResult.StdOut),
            LogExcerpt = TruncateForLog(processResult.StdOut, _options.MaxLogBytes)
        };

        return result;
    }

    public async Task<DllEjectResult> EjectAsync(
        DllEjectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting DLL ejection: module={ModuleName}, operationId={OperationId}",
            command.ModuleName,
            operationId);

        ReportProgress(progress, "resolving", "Resolving target process", 0);

        var workspaceRoot = GetWorkspaceRoot();
        var pid = DllEjectCommandValidator.ValidateAndResolveTarget(
            command,
            _options,
            _processResolver);

        var processName = GetProcessName(pid);

        _logger.LogInformation(
            "Target process resolved: pid={ProcessId}, name={ProcessName}",
            pid,
            processName);

        var workingDirectory = DetermineWorkingDirectory(command.WorkingDirectory, workspaceRoot, operationId, "eject");
        Directory.CreateDirectory(workingDirectory);

        var timeoutSeconds = command.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;

        var arguments = BuildEjectArguments(command, pid);

        _logger.LogDebug(
            "DLL injector command: {Executable} [arguments redacted for security]",
            _dllInjectorExePath);

        ReportProgress(progress, "ejecting", "Ejecting DLL", 50);

        var startTime = DateTimeOffset.UtcNow;
        ProcessResult processResult;

        try
        {
            processResult = await _processRunner.ExecuteAsync(
                _dllInjectorExePath,
                arguments,
                workingDirectory,
                timeoutSeconds,
                environmentVariables: null,
                cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "DLL ejection timed out after {TimeoutSeconds} seconds", timeoutSeconds);

            throw new ToolExecutionError("dllinjector", -1, $"DLL ejection timed out after {timeoutSeconds} seconds", ex)
            {
                RemediationHint = "Try increasing the timeout in DllInjectorOptions or check if the target process is responsive."
            };
        }

        var endTime = DateTimeOffset.UtcNow;

        WriteOperationLog(workspaceRoot, operationId, processResult, arguments);

        ReportProgress(progress, "verifying", "Verifying ejection", 75);

        if (processResult.ExitCode != 0)
        {
            _logger.LogError(
                "DLL ejection failed with exit code {ExitCode}, stderr: {StdErr}",
                processResult.ExitCode,
                TruncateForLog(processResult.StdErr, 500));

            throw new ToolExecutionError("dllinjector", processResult.ExitCode, "DLL ejection operation failed")
            {
                CommandLine = "[command redacted]",
                StandardOutput = TruncateForLog(processResult.StdOut, _options.MaxLogBytes),
                StandardError = TruncateForLog(processResult.StdErr, _options.MaxLogBytes),
                RemediationHint = "Check the DLL injector logs for details. Ensure the module name is correct and the DLL is loaded in the target process."
            };
        }

        ReportProgress(progress, "finalizing", "Finalizing", 100);

        _logger.LogInformation(
            "DLL ejection completed successfully in {DurationMs}ms, operationId={OperationId}",
            processResult.Duration.TotalMilliseconds,
            operationId);

        var wasLoadedBefore = !processResult.StdOut.Contains("not loaded", StringComparison.OrdinalIgnoreCase);
        var isUnloaded = processResult.StdOut.Contains("unloaded", StringComparison.OrdinalIgnoreCase) || processResult.ExitCode == 0;

        var result = new DllEjectResult
        {
            OperationId = operationId,
            ProcessId = pid,
            ProcessName = processName,
            ModuleName = command.ModuleName,
            Duration = processResult.Duration,
            WasLoadedBefore = wasLoadedBefore,
            IsUnloaded = isUnloaded,
            Warnings = ExtractWarnings(processResult.StdOut),
            LogExcerpt = TruncateForLog(processResult.StdOut, _options.MaxLogBytes)
        };

        return result;
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating DLL injector dependency");

        var result = await _dependencyValidator.ValidateToolAsync("dllinjector", cancellationToken);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "DLL injector validation failed: {Status} - {ErrorMessage}",
                result.Status,
                result.ErrorMessage);
            return false;
        }

        _logger.LogDebug("DLL injector dependency validated successfully");
        return true;
    }

    private string BuildInjectArguments(DllInjectCommand command, int pid)
    {
        var args = new List<string>
        {
            "inject",
            "--pid",
            pid.ToString(),
            "--dll",
            $"\"{command.DllPath}\"",
            "--method",
            command.Method.ToString().ToLowerInvariant()
        };

        if (command.Arguments != null && command.Arguments.Count > 0)
        {
            args.Add("--args");
            foreach (var arg in command.Arguments)
            {
                args.Add($"\"{arg}\"");
            }
        }

        return string.Join(" ", args);
    }

    private string BuildEjectArguments(DllEjectCommand command, int pid)
    {
        var args = new List<string>
        {
            "eject",
            "--pid",
            pid.ToString(),
            "--module",
            $"\"{command.ModuleName}\""
        };

        return string.Join(" ", args);
    }

    private string GetWorkspaceRoot()
    {
        var workspaceRoot = _workspaceOptions.DefaultWorkspacePath;
        if (string.IsNullOrEmpty(workspaceRoot))
        {
            workspaceRoot = Path.Combine(Path.GetTempPath(), "aris");
        }
        return workspaceRoot;
    }

    private string DetermineWorkingDirectory(string? commandWorkingDir, string workspaceRoot, string operationId, string operation)
    {
        if (!string.IsNullOrEmpty(commandWorkingDir))
        {
            return commandWorkingDir;
        }

        return Path.Combine(workspaceRoot, "temp", $"{operation}-{operationId}");
    }

    private string GetProcessName(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return $"pid-{pid}";
        }
    }

    private void WriteOperationLog(string workspaceRoot, string operationId, ProcessResult processResult, string arguments)
    {
        try
        {
            var logsDir = Path.Combine(workspaceRoot, "logs");
            Directory.CreateDirectory(logsDir);

            var logPath = Path.Combine(logsDir, $"dllinjector-{operationId}.log");

            var logContent = $@"DLL Injector Operation Log
Operation ID: {operationId}
Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC
Exit Code: {processResult.ExitCode}
Duration: {processResult.Duration.TotalMilliseconds:F2}ms

Command (redacted):
dllinjector.exe [arguments redacted for security]

Standard Output:
{TruncateForLog(processResult.StdOut, _options.MaxLogBytes)}

Standard Error:
{TruncateForLog(processResult.StdErr, _options.MaxLogBytes)}
";

            File.WriteAllText(logPath, logContent);

            _logger.LogDebug("Operation log written to {LogPath}", logPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write operation log for {OperationId}", operationId);
        }
    }

    private static void ReportProgress(IProgress<ProgressEvent>? progress, string step, string message, int percent)
    {
        progress?.Report(new ProgressEvent
        {
            Step = step,
            Message = message,
            Percent = percent
        });
    }

    private static bool IsElevationError(ProcessResult processResult)
    {
        if (processResult.ExitCode == 5)
        {
            return true;
        }

        var combinedOutput = (processResult.StdErr + " " + processResult.StdOut).ToLowerInvariant();
        return combinedOutput.Contains("access denied") ||
               combinedOutput.Contains("access is denied") ||
               combinedOutput.Contains("elevation required") ||
               combinedOutput.Contains("administrator");
    }

    private static string TruncateForLog(string text, long maxBytes)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxBytes)
        {
            return text;
        }

        var halfMax = (int)(maxBytes / 2);
        var head = text.Substring(0, halfMax);
        var tail = text.Substring(text.Length - halfMax);

        return $"{head}\n\n... [truncated {text.Length - maxBytes} bytes] ...\n\n{tail}";
    }

    private static List<string> ExtractWarnings(string stdout)
    {
        var warnings = new List<string>();

        if (string.IsNullOrEmpty(stdout))
        {
            return warnings;
        }

        var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("warn:", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(line.Trim());
            }
        }

        return warnings;
    }
}
