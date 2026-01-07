using System.Diagnostics;
using Aris.Core.Models;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aris.Adapters.UAsset;

public class UAssetService : IUAssetService
{
    private readonly IUAssetBackend _backend;
    private readonly ILogger<UAssetService> _logger;
    private readonly UAssetOptions _options;

    public UAssetService(
        IUAssetBackend backend,
        ILogger<UAssetService> logger,
        IOptions<UAssetOptions> options)
    {
        _backend = backend;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<UAssetResult> SerializeAsync(
        UAssetSerializeCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting UAsset serialization: operationId={OperationId}, input={InputPath}, output={OutputPath}",
            operationId,
            command.InputJsonPath,
            command.OutputAssetPath);

        UAssetCommandValidator.ValidateSerializeCommand(command, _options);

        ReportProgress(progress, "opening", "Opening input JSON file", 0);

        var stagingDir = CreateStagingDirectory(operationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ReportProgress(progress, "parsing", "Parsing JSON structure", 20);
            ReportProgress(progress, "converting", "Converting JSON to asset", 40);
            ReportProgress(progress, "writing", "Writing asset file", 60);

            var backendResult = await _backend.SerializeAsync(command, stagingDir, cancellationToken);

            ReportProgress(progress, "hashing", "Computing file hashes", 80);

            var producedFiles = GatherProducedFiles(backendResult.ProducedFilePaths);

            ReportProgress(progress, "finalizing", "Finalizing output", 90);

            stopwatch.Stop();

            var result = new UAssetResult
            {
                Operation = UAssetOperation.Serialize,
                InputPath = command.InputJsonPath,
                OutputPath = backendResult.OutputPath,
                Duration = stopwatch.Elapsed,
                Warnings = backendResult.Warnings,
                ProducedFiles = producedFiles,
                SchemaVersion = backendResult.UsedSchemaVersion,
                UEVersion = backendResult.DetectedUEVersion
            };

            ReportProgress(progress, "complete", "Serialization complete", 100);

            _logger.LogInformation(
                "UAsset serialization completed successfully in {DurationMs}ms, operationId={OperationId}",
                stopwatch.ElapsedMilliseconds,
                operationId);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "UAsset serialization failed, operationId={OperationId}", operationId);

            if (!_options.KeepTempOnFailure && Directory.Exists(stagingDir))
            {
                try
                {
                    Directory.Delete(stagingDir, recursive: true);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up staging directory {StagingDir}", stagingDir);
                }
            }

            throw;
        }
    }

    public async Task<UAssetResult> DeserializeAsync(
        UAssetDeserializeCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting UAsset deserialization: operationId={OperationId}, input={InputPath}, output={OutputPath}",
            operationId,
            command.InputAssetPath,
            command.OutputJsonPath);

        UAssetCommandValidator.ValidateDeserializeCommand(command, _options);

        ReportProgress(progress, "opening", "Opening input asset file", 0);

        var stagingDir = CreateStagingDirectory(operationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ReportProgress(progress, "parsing", "Parsing asset structure", 20);
            ReportProgress(progress, "converting", "Converting asset to JSON", 40);
            ReportProgress(progress, "writing", "Writing JSON file", 60);

            var backendResult = await _backend.DeserializeAsync(command, stagingDir, cancellationToken);

            ReportProgress(progress, "hashing", "Computing file hashes", 80);

            var producedFiles = GatherProducedFiles(backendResult.ProducedFilePaths);

            ReportProgress(progress, "finalizing", "Finalizing output", 90);

            stopwatch.Stop();

            var result = new UAssetResult
            {
                Operation = UAssetOperation.Deserialize,
                InputPath = command.InputAssetPath,
                OutputPath = backendResult.OutputPath,
                Duration = stopwatch.Elapsed,
                Warnings = backendResult.Warnings,
                ProducedFiles = producedFiles,
                SchemaVersion = backendResult.UsedSchemaVersion,
                UEVersion = backendResult.DetectedUEVersion
            };

            ReportProgress(progress, "complete", "Deserialization complete", 100);

            _logger.LogInformation(
                "UAsset deserialization completed successfully in {DurationMs}ms, operationId={OperationId}",
                stopwatch.ElapsedMilliseconds,
                operationId);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "UAsset deserialization failed, operationId={OperationId}", operationId);

            if (!_options.KeepTempOnFailure && Directory.Exists(stagingDir))
            {
                try
                {
                    Directory.Delete(stagingDir, recursive: true);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up staging directory {StagingDir}", stagingDir);
                }
            }

            throw;
        }
    }

    public async Task<UAssetInspection> InspectAsync(
        UAssetInspectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null)
    {
        var operationId = command.OperationId;

        _logger.LogInformation(
            "Starting UAsset inspection: operationId={OperationId}, input={InputPath}",
            operationId,
            command.InputAssetPath);

        UAssetCommandValidator.ValidateInspectCommand(command, _options);

        ReportProgress(progress, "opening", "Opening asset file", 0);
        ReportProgress(progress, "parsing", "Parsing asset metadata", 50);

        var inspection = await _backend.InspectAsync(command, cancellationToken);

        ReportProgress(progress, "complete", "Inspection complete", 100);

        _logger.LogInformation(
            "UAsset inspection completed, operationId={OperationId}",
            operationId);

        return inspection;
    }

    private static string CreateStagingDirectory(string operationId)
    {
        // Use system temp directory for staging
        var stagingDir = Path.Combine(Path.GetTempPath(), "aris", "temp", $"uasset-{operationId}");
        Directory.CreateDirectory(stagingDir);

        return stagingDir;
    }

    private static void ReportProgress(
        IProgress<ProgressEvent>? progress,
        string step,
        string message,
        double? percent = null)
    {
        progress?.Report(new ProgressEvent(step, message, percent));
    }

    private static IReadOnlyList<ProducedFile> GatherProducedFiles(string[] filePaths)
    {
        var files = new List<ProducedFile>();

        foreach (var path in filePaths)
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

                files.Add(new ProducedFile
                {
                    Path = path,
                    SizeBytes = fileInfo.Length,
                    FileType = extension,
                    Sha256 = null
                });
            }
        }

        return files;
    }
}
