using System.Diagnostics;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Process;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Adapter for executing DLL injection and ejection operations.
/// Uses native C# DLL injection via Win32 API.
/// </summary>
public class DllInjectorAdapter : IDllInjectorAdapter
{
    private readonly IDllInjectionService _injectionService;
    private readonly IProcessResolver _processResolver;
    private readonly ILogger<DllInjectorAdapter> _logger;
    private readonly DllInjectorOptions _options;

    public DllInjectorAdapter(
        IDllInjectionService injectionService,
        IProcessResolver processResolver,
        ILogger<DllInjectorAdapter> logger,
        IOptions<DllInjectorOptions> options)
    {
        _injectionService = injectionService;
        _processResolver = processResolver;
        _logger = logger;
        _options = options.Value;
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

        var tempRoot = GetTempRoot();
        var pid = DllInjectCommandValidator.ValidateAndResolveTarget(
            command,
            _options,
            _processResolver);

        var processName = GetProcessName(pid);

        _logger.LogInformation(
            "Target process resolved: pid={ProcessId}, name={ProcessName}",
            pid,
            processName);

        ReportProgress(progress, "validating", "Validating payload", 25);

        var workingDirectory = DetermineWorkingDirectory(command.WorkingDirectory, tempRoot, operationId, "inject");
        Directory.CreateDirectory(workingDirectory);

        ReportProgress(progress, "injecting", "Injecting DLL", 50);

        var startTime = DateTimeOffset.UtcNow;

        // Perform native DLL injection
        DllInjectionResult injectionResult = await _injectionService.InjectAsync(
            pid,
            command.DllPath,
            cancellationToken);

        var endTime = DateTimeOffset.UtcNow;

        // Write operation log to temp directory
        WriteNativeInjectionLog(tempRoot, operationId, injectionResult);

        ReportProgress(progress, "verifying", "Verifying injection", 75);

        // Handle injection failure
        if (!injectionResult.Success)
        {
            if (injectionResult.RequiresElevation)
            {
                _logger.LogError(
                    "DLL injection failed due to elevation requirement. Win32 error={Win32Error}",
                    injectionResult.Win32ErrorCode);

                throw new ElevationRequiredError("DLL injection requires elevation (run as administrator)")
                {
                    OperationId = operationId,
                    RemediationHint = "Run ARIS as administrator to inject DLLs into protected processes."
                };
            }

            _logger.LogError(
                "DLL injection failed: {ErrorMessage}. Win32Error={Win32Error}",
                injectionResult.ErrorMessage,
                injectionResult.Win32ErrorCode);

            throw new ToolExecutionError("dllinjector", injectionResult.Win32ErrorCode ?? -1, injectionResult.ErrorMessage ?? "DLL injection failed")
            {
                RemediationHint = "Check the DLL injector logs for details. Ensure the target process and payload DLL are compatible (both x64)."
            };
        }

        ReportProgress(progress, "finalizing", "Finalizing", 100);

        _logger.LogInformation(
            "DLL injection completed successfully in {DurationMs}ms, operationId={OperationId}, moduleBase=0x{ModuleBase:X}",
            injectionResult.Duration.TotalMilliseconds,
            operationId,
            injectionResult.LoadedModuleAddress.ToInt64());

        var result = new DllInjectResult
        {
            OperationId = operationId,
            ProcessId = pid,
            ProcessName = processName,
            DllPath = command.DllPath,
            Method = command.Method,
            ElevationUsed = injectionResult.RequiresElevation,
            Duration = injectionResult.Duration,
            Warnings = new List<string>(), // Native injector doesn't produce warnings currently
            LogExcerpt = $"DLL loaded at base address: 0x{injectionResult.LoadedModuleAddress.ToInt64():X}"
        };

        return result;
    }

    public Task<DllEjectResult> EjectAsync(
        DllEjectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        // DLL ejection is not yet implemented in the native injector
        throw new NotImplementedException("DLL ejection is not yet implemented in the native C# injector. This feature will be added in a future release.");
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        // Native DLL injector doesn't require external tools, always available on Windows
        _logger.LogDebug("Native DLL injector is always available on Windows");
        await Task.CompletedTask; // Satisfy async signature
        return true;
    }


    private static string GetTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "aris");
    }

    private static string DetermineWorkingDirectory(string? commandWorkingDir, string tempRoot, string operationId, string operation)
    {
        if (!string.IsNullOrEmpty(commandWorkingDir))
        {
            return commandWorkingDir;
        }

        return Path.Combine(tempRoot, "temp", $"{operation}-{operationId}");
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

    private void WriteNativeInjectionLog(string tempRoot, string operationId, DllInjectionResult injectionResult)
    {
        try
        {
            var logsDir = Path.Combine(tempRoot, "logs");
            Directory.CreateDirectory(logsDir);

            var logPath = Path.Combine(logsDir, $"dllinjector-{operationId}.log");

            var logContent = $@"DLL Injector Operation Log (Native C# Implementation)
Operation ID: {operationId}
Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC
Success: {injectionResult.Success}
Process ID: {injectionResult.ProcessId}
DLL Path: {injectionResult.DllPath}
Duration: {injectionResult.Duration.TotalMilliseconds:F2}ms
Module Base Address: 0x{injectionResult.LoadedModuleAddress.ToInt64():X}
Win32 Error Code: {injectionResult.Win32ErrorCode?.ToString() ?? "N/A"}
Requires Elevation: {injectionResult.RequiresElevation}
Error Message: {injectionResult.ErrorMessage ?? "N/A"}
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

}
