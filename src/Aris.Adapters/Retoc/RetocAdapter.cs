using System.Diagnostics;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.Retoc;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Process;
using Aris.Infrastructure.Tools;
using Aris.Tools.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aris.Adapters.Retoc;

public class RetocAdapter : IRetocAdapter
{
    private readonly IProcessRunner _processRunner;
    private readonly IDependencyValidator _dependencyValidator;
    private readonly ILogger<RetocAdapter> _logger;
    private readonly RetocOptions _options;
    private readonly string _retocExePath;

    public RetocAdapter(
        IProcessRunner processRunner,
        IDependencyValidator dependencyValidator,
        ILogger<RetocAdapter> logger,
        IOptions<RetocOptions> options)
    {
        _processRunner = processRunner;
        _dependencyValidator = dependencyValidator;
        _logger = logger;
        _options = options.Value;

        // Determine retoc.exe path from manifest
        var manifest = ToolManifestLoader.Load();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
        var retocEntry = manifest.Tools.FirstOrDefault(t => t.Id == "retoc")
            ?? throw new DependencyMissingError("retoc", "Retoc entry not found in tool manifest");

        _retocExePath = Path.Combine(toolsRoot, retocEntry.RelativePath);
    }

    public (string ExecutablePath, string[] Arguments, string CommandLine) BuildCommand(RetocCommand command)
    {
        _logger.LogDebug(
            "Building Retoc command: commandType={CommandType}, mode={Mode}",
            command.CommandType,
            command.Mode);

        var (executablePath, argumentList, argumentsString) = RetocCommandBuilder.BuildWithList(command, _options, _retocExePath);

        // Format command line for preview (quote executable path if it contains spaces)
        var quotedExePath = executablePath.Contains(' ') ? $"\"{executablePath}\"" : executablePath;
        var commandLine = $"{quotedExePath} {argumentsString}";

        return (executablePath, argumentList.ToArray(), commandLine);
    }

    public async Task<RetocResult> ConvertAsync(
        RetocCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting Retoc conversion: mode={Mode}, operationId={OperationId}",
            command.Mode,
            operationId);

        // Emit initial progress
        ReportProgress(progress, "staging", "Preparing workspace and staging files", 0);

        // Build command line
        var (executablePath, arguments) = RetocCommandBuilder.Build(command, _options, _retocExePath);

        // TODO: Redact keys from logged command line
        _logger.LogDebug("Retoc command: {Executable} {Arguments}", executablePath, "[arguments redacted]");

        // Determine working directory
        var workingDirectory = command.WorkingDirectory;
        if (string.IsNullOrEmpty(workingDirectory))
        {
            // Default to system temp/aris/retoc/{operationId}/
            workingDirectory = Path.Combine(Path.GetTempPath(), "aris", "retoc", operationId);
            Directory.CreateDirectory(workingDirectory);
        }

        // Determine timeout
        var timeoutSeconds = command.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;

        // Emit decrypting progress (if keys are present)
        if (command.MountKeys.Count > 0)
        {
            ReportProgress(progress, "decrypting", "Decrypting input package", 20);
        }

        // Emit converting progress
        ReportProgress(progress, "converting", $"Converting package ({command.Mode})", 40);

        // Set environment variables if structured logging enabled
        var environmentVariables = _options.EnableStructuredLogs
            ? new Dictionary<string, string> { ["RETOC_LOG_JSON"] = "1" }
            : null;

        // Execute process
        var startTime = DateTimeOffset.UtcNow;
        ProcessResult processResult;

        try
        {
            processResult = await _processRunner.ExecuteAsync(
                executablePath,
                arguments,
                workingDirectory,
                timeoutSeconds,
                environmentVariables,
                cancellationToken);
        }
        catch (TimeoutException ex)
        {
            throw new ToolExecutionError("retoc", -1, $"Retoc operation timed out after {timeoutSeconds} seconds", ex)
            {
                RemediationHint = "Try increasing the timeout in RetocOptions or simplifying the conversion."
            };
        }

        var endTime = DateTimeOffset.UtcNow;

        // Emit finalizing progress
        ReportProgress(progress, "finalizing", "Finalizing output files", 90);

        // Check exit code
        if (processResult.ExitCode != 0)
        {
            _logger.LogError(
                "Retoc failed with exit code {ExitCode}, stderr: {StdErr}",
                processResult.ExitCode,
                TruncateForLog(processResult.StdErr, 500));

            throw new ToolExecutionError("retoc", processResult.ExitCode, "Retoc conversion failed")
            {
                CommandLine = "[command redacted]", // TODO: Implement full redaction
                StandardOutput = TruncateForLog(processResult.StdOut, _options.MaxLogBytes),
                StandardError = TruncateForLog(processResult.StdErr, _options.MaxLogBytes),
                RemediationHint = "Check the Retoc logs for details. Ensure input file is valid and keys are correct."
            };
        }

        // Emit completion
        ReportProgress(progress, "complete", "Conversion complete", 100);

        _logger.LogInformation(
            "Retoc conversion completed successfully in {DurationMs}ms, operationId={OperationId}",
            processResult.Duration.TotalMilliseconds,
            operationId);

        // Build result
        var result = new RetocResult
        {
            ExitCode = processResult.ExitCode,
            OutputPath = command.OutputPath,
            OutputFormat = DetermineOutputFormat(command.Mode),
            Duration = processResult.Duration,
            Warnings = ExtractWarnings(processResult.StdOut),
            ProducedFiles = GatherProducedFiles(command.OutputPath),
            LogExcerpt = TruncateForLog(processResult.StdOut, _options.MaxLogBytes)
        };

        return result;
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating Retoc dependency");

        var result = await _dependencyValidator.ValidateToolAsync("retoc", cancellationToken);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Retoc validation failed: {Status} - {ErrorMessage}",
                result.Status,
                result.ErrorMessage);
            return false;
        }

        _logger.LogDebug("Retoc dependency validated successfully");
        return true;
    }

    private static void ReportProgress(
        IProgress<ProgressEvent>? progress,
        string step,
        string message,
        double? percent = null)
    {
        progress?.Report(new ProgressEvent(step, message, percent));
    }

    private static string DetermineOutputFormat(RetocMode? mode)
    {
        if (!mode.HasValue)
        {
            return "unknown";
        }

        return mode.Value switch
        {
            RetocMode.PakToIoStore => "iostore",
            RetocMode.IoStoreToPak => "pak",
            RetocMode.Repack => "pak", // Assumes repack maintains format
            RetocMode.Validate => "unknown",
            _ => "unknown"
        };
    }

    private static IReadOnlyList<string> ExtractWarnings(string output)
    {
        // Simple warning extraction - look for lines containing "warning"
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
            // Check if output is a directory or file
            if (Directory.Exists(outputPath))
            {
                foreach (var file in Directory.GetFiles(outputPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    files.Add(CreateProducedFile(file));
                }
            }
            else if (File.Exists(outputPath))
            {
                files.Add(CreateProducedFile(outputPath));

                // Check for associated files (e.g., .utoc, .ucas for IoStore)
                var dir = Path.GetDirectoryName(outputPath);
                var baseName = Path.GetFileNameWithoutExtension(outputPath);

                if (dir != null && Directory.Exists(dir))
                {
                    foreach (var associatedFile in Directory.GetFiles(dir, $"{baseName}.*"))
                    {
                        if (associatedFile != outputPath)
                        {
                            files.Add(CreateProducedFile(associatedFile));
                        }
                    }
                }
            }
        }
        catch
        {
            // If we can't gather files (e.g., directory doesn't exist), return empty list
            // This is not a critical failure - the operation may have succeeded but output isn't accessible
        }

        return files;
    }

    private static ProducedFile CreateProducedFile(string path)
    {
        var fileInfo = new FileInfo(path);
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

        return new ProducedFile
        {
            Path = path,
            SizeBytes = fileInfo.Length,
            FileType = extension,
            Sha256 = null // TODO: Compute hash if required by configuration
        };
    }

    private static string TruncateForLog(string text, int maxBytes)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(text) <= maxBytes)
        {
            return text;
        }

        var truncated = text.Substring(0, Math.Min(text.Length, maxBytes / 2));
        return truncated + "\n... [truncated] ...";
    }
}
