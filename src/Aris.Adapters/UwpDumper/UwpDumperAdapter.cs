using System.Diagnostics;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.UwpDumper;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Process;
using Aris.Infrastructure.Tools;
using Aris.Tools.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aris.Adapters.UwpDumper;

public class UwpDumperAdapter : IUwpDumperAdapter
{
    private readonly IProcessRunner _processRunner;
    private readonly IDependencyValidator _dependencyValidator;
    private readonly ILogger<UwpDumperAdapter> _logger;
    private readonly UwpDumperOptions _options;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly string _uwpDumperExePath;

    public UwpDumperAdapter(
        IProcessRunner processRunner,
        IDependencyValidator dependencyValidator,
        ILogger<UwpDumperAdapter> logger,
        IOptions<UwpDumperOptions> options,
        IOptions<WorkspaceOptions> workspaceOptions)
    {
        _processRunner = processRunner;
        _dependencyValidator = dependencyValidator;
        _logger = logger;
        _options = options.Value;
        _workspaceOptions = workspaceOptions.Value;

        // Determine uwpdumper.exe path from manifest
        var manifest = ToolManifestLoader.Load();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
        var uwpDumperEntry = manifest.Tools.FirstOrDefault(t => t.Id == "uwpdumper")
            ?? throw new DependencyMissingError("uwpdumper", "UWPDumper entry not found in tool manifest");

        _uwpDumperExePath = Path.Combine(toolsRoot, uwpDumperEntry.RelativePath);
    }

    public async Task<UwpDumpResult> DumpAsync(
        UwpDumpCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting UWPDumper operation: mode={Mode}, pfn={PackageFamilyName}, operationId={OperationId}",
            command.Mode,
            command.PackageFamilyName,
            operationId);

        UwpDumpCommandValidator.ValidateDumpCommand(command, _options, _workspaceOptions.DefaultWorkspacePath);

        ReportProgress(progress, "locating", "Locating package", 0);

        var workingDirectory = command.WorkingDirectory;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            var workspaceRoot = _workspaceOptions.DefaultWorkspacePath;
            if (string.IsNullOrEmpty(workspaceRoot))
            {
                workspaceRoot = Path.Combine(Path.GetTempPath(), "aris");
            }
            workingDirectory = Path.Combine(workspaceRoot, "temp", $"uwp-{operationId}");
            Directory.CreateDirectory(workingDirectory);
        }

        Directory.CreateDirectory(command.OutputPath);

        var timeoutSeconds = command.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;

        var arguments = BuildArguments(command);

        _logger.LogDebug(
            "UWPDumper command: {Executable} {Arguments}",
            _uwpDumperExePath,
            "[arguments redacted for security]");

        ReportProgress(progress, "preparing", "Preparing dump", 25);

        var startTime = DateTimeOffset.UtcNow;
        ProcessResult processResult;

        try
        {
            ReportProgress(progress, "dumping", "Dumping package", 50);

            processResult = await _processRunner.ExecuteAsync(
                _uwpDumperExePath,
                arguments,
                workingDirectory,
                timeoutSeconds,
                environmentVariables: null,
                cancellationToken);
        }
        catch (TimeoutException ex)
        {
            throw new ToolExecutionError("uwpdumper", -1, $"UWPDumper operation timed out after {timeoutSeconds} seconds", ex)
            {
                RemediationHint = "Try increasing the timeout in UwpDumperOptions or use a less intensive dump mode."
            };
        }

        var endTime = DateTimeOffset.UtcNow;

        WriteOperationLog(command, processResult, arguments);

        ReportProgress(progress, "finalizing", "Finalizing output", 90);

        if (processResult.ExitCode != 0)
        {
            var errorMessage = processResult.StdErr;

            if (IsElevationError(processResult))
            {
                _logger.LogError(
                    "UWPDumper failed due to elevation requirement, exit code {ExitCode}",
                    processResult.ExitCode);

                throw new ElevationRequiredError("UWPDumper requires elevation (run as administrator)")
                {
                    OperationId = operationId,
                    RemediationHint = "Run ARIS as administrator or disable RequireElevation in UwpDumperOptions if your environment supports non-elevated dumps"
                };
            }

            _logger.LogError(
                "UWPDumper failed with exit code {ExitCode}, stderr: {StdErr}",
                processResult.ExitCode,
                TruncateForLog(errorMessage, 500));

            throw new ToolExecutionError("uwpdumper", processResult.ExitCode, "UWPDumper dump operation failed")
            {
                CommandLine = "[command redacted]",
                StandardOutput = TruncateForLog(processResult.StdOut, _options.MaxLogBytes),
                StandardError = TruncateForLog(processResult.StdErr, _options.MaxLogBytes),
                RemediationHint = "Check the UWPDumper logs for details. Ensure the package family name is correct and the package is installed."
            };
        }

        ReportProgress(progress, "complete", "Dump complete", 100);

        _logger.LogInformation(
            "UWPDumper operation completed successfully in {DurationMs}ms, operationId={OperationId}",
            processResult.Duration.TotalMilliseconds,
            operationId);

        var result = new UwpDumpResult
        {
            OperationId = operationId,
            PackageFamilyName = command.PackageFamilyName,
            ApplicationId = command.ApplicationId,
            OutputPath = command.OutputPath,
            Duration = processResult.Duration,
            Warnings = ExtractWarnings(processResult.StdOut),
            Artifacts = GatherProducedFiles(command.OutputPath),
            LogExcerpt = TruncateForLog(processResult.StdOut, _options.MaxLogBytes)
        };

        return result;
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating UWPDumper dependency");

        var result = await _dependencyValidator.ValidateToolAsync("uwpdumper", cancellationToken);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "UWPDumper validation failed: {Status} - {ErrorMessage}",
                result.Status,
                result.ErrorMessage);
            return false;
        }

        _logger.LogDebug("UWPDumper dependency validated successfully");
        return true;
    }

    private string BuildArguments(UwpDumpCommand command)
    {
        var args = new List<string>();

        args.Add("--pfn");
        args.Add($"\"{command.PackageFamilyName}\"");

        if (!string.IsNullOrEmpty(command.ApplicationId))
        {
            args.Add("--appid");
            args.Add($"\"{command.ApplicationId}\"");
        }

        args.Add("--output");
        args.Add($"\"{command.OutputPath}\"");

        args.Add("--mode");
        args.Add(command.Mode switch
        {
            UwpDumpMode.FullDump => "full",
            UwpDumpMode.MetadataOnly => "metadata",
            UwpDumpMode.ValidateOnly => "validate",
            _ => throw new ValidationError($"Unsupported UwpDumpMode: {command.Mode}", nameof(command.Mode))
        });

        if (command.IncludeSymbols)
        {
            args.Add("--symbols");
        }

        return string.Join(" ", args);
    }

    private bool IsElevationError(ProcessResult result)
    {
        if (_options.RequireElevation && result.ExitCode != 0)
        {
            var combinedOutput = result.StdOut + " " + result.StdErr;
            return combinedOutput.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                   combinedOutput.Contains("elevation required", StringComparison.OrdinalIgnoreCase) ||
                   combinedOutput.Contains("administrator", StringComparison.OrdinalIgnoreCase) ||
                   result.ExitCode == 5;
        }

        return false;
    }

    private static void ReportProgress(
        IProgress<ProgressEvent>? progress,
        string step,
        string message,
        double? percent = null)
    {
        progress?.Report(new ProgressEvent(step, message, percent));
    }

    private static IReadOnlyList<string> ExtractWarnings(string output)
    {
        var warnings = new List<string>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains("warning", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(line.Trim());
            }
        }

        return warnings;
    }

    private static IReadOnlyList<ProducedFile> GatherProducedFiles(string outputPath)
    {
        var files = new List<ProducedFile>();

        try
        {
            if (Directory.Exists(outputPath))
            {
                foreach (var file in Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories))
                {
                    files.Add(CreateProducedFile(file));
                }
            }
        }
        catch
        {
            // If we can't gather files, return empty list
        }

        return files;
    }

    private static ProducedFile CreateProducedFile(string path)
    {
        var fileInfo = new FileInfo(path);
        var fileName = Path.GetFileName(path).ToLowerInvariant();

        var fileType = DetermineFileType(fileName);

        return new ProducedFile
        {
            Path = path,
            SizeBytes = fileInfo.Length,
            FileType = fileType,
            Sha256 = null
        };
    }

    private static string DetermineFileType(string fileName)
    {
        if (fileName.Contains("header") || fileName.EndsWith(".h") || fileName.EndsWith(".hpp"))
        {
            return "Headers";
        }

        if (fileName.Contains("metadata") || fileName.EndsWith(".json") || fileName.EndsWith(".xml"))
        {
            return "Metadata";
        }

        if (fileName.Contains("symbol") || fileName.EndsWith(".pdb") || fileName.EndsWith(".sym"))
        {
            return "Symbols";
        }

        if (fileName.Contains("manifest") || fileName.EndsWith(".manifest"))
        {
            return "Manifest";
        }

        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return string.IsNullOrEmpty(extension) ? "Unknown" : extension;
    }

    private static string TruncateForLog(string text, long maxBytes)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(text) <= maxBytes)
        {
            return text;
        }

        var truncated = text.Substring(0, Math.Min(text.Length, (int)(maxBytes / 2)));
        return truncated + "\n... [truncated] ...";
    }

    private void WriteOperationLog(
        UwpDumpCommand command,
        ProcessResult processResult,
        string commandLine)
    {
        try
        {
            var workspaceRoot = _workspaceOptions.DefaultWorkspacePath;
            if (string.IsNullOrEmpty(workspaceRoot))
            {
                _logger.LogWarning("WorkspaceOptions.DefaultWorkspacePath is not configured; skipping operation log");
                return;
            }

            var logsDir = Path.Combine(workspaceRoot, "logs");
            Directory.CreateDirectory(logsDir);

            var logFileName = $"uwpdumper-{command.OperationId}.log";
            var logFilePath = Path.Combine(logsDir, logFileName);

            var logContent = new System.Text.StringBuilder();
            logContent.AppendLine("=== ARIS UWPDumper Operation Log ===");
            logContent.AppendLine($"Operation ID: {command.OperationId}");
            logContent.AppendLine($"Package Family Name: {command.PackageFamilyName}");

            if (!string.IsNullOrEmpty(command.ApplicationId))
            {
                logContent.AppendLine($"Application ID: {command.ApplicationId}");
            }

            logContent.AppendLine($"Mode: {command.Mode}");
            logContent.AppendLine($"Include Symbols: {command.IncludeSymbols}");
            logContent.AppendLine($"Output Path: {command.OutputPath}");
            logContent.AppendLine($"Timestamp: {processResult.StartTime:yyyy-MM-dd HH:mm:ss UTC}");
            logContent.AppendLine($"Exit Code: {processResult.ExitCode}");
            logContent.AppendLine($"Duration: {processResult.Duration.TotalSeconds:F2}s");
            logContent.AppendLine();

            logContent.AppendLine("Command Line:");
            logContent.AppendLine(commandLine);
            logContent.AppendLine();

            var maxOutputBytes = _options.MaxLogBytes / 2;
            logContent.AppendLine("Standard Output:");
            logContent.AppendLine(TruncateForLog(processResult.StdOut, maxOutputBytes));
            logContent.AppendLine();

            logContent.AppendLine("Standard Error:");
            logContent.AppendLine(TruncateForLog(processResult.StdErr, maxOutputBytes));

            File.WriteAllText(logFilePath, logContent.ToString());

            _logger.LogDebug("Wrote operation log to {LogFilePath}", logFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write operation log for operation {OperationId}", command.OperationId);
        }
    }
}
