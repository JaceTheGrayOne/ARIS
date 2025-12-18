using Aris.Core.Errors;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace Aris.Adapters.UAsset;

public class UAssetApiBackend : IUAssetBackend
{
    private readonly ILogger<UAssetApiBackend> _logger;
    private readonly UAssetOptions _options;

    public UAssetApiBackend(
        ILogger<UAssetApiBackend> logger,
        IOptions<UAssetOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<UAssetBackendResult> SerializeAsync(
        UAssetSerializeCommand command,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Serializing JSON to asset with UAssetAPI: {InputPath} → {OutputPath}",
            command.InputJsonPath, command.OutputAssetPath);

        var timeoutSeconds = command.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var token = linkedCts.Token;

        try
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(command.InputJsonPath))
                {
                    throw new FileNotFoundException($"Input JSON file not found: {command.InputJsonPath}");
                }

                token.ThrowIfCancellationRequested();

                var jsonContent = File.ReadAllText(command.InputJsonPath);

                token.ThrowIfCancellationRequested();

                var asset = global::UAssetAPI.UAsset.DeserializeJson(jsonContent);

                if (!string.IsNullOrEmpty(command.UEVersion))
                {
                    var engineVersion = ParseEngineVersion(command.UEVersion);
                    asset.SetEngineVersion(engineVersion);
                }

                token.ThrowIfCancellationRequested();

                var outputDir = Path.GetDirectoryName(command.OutputAssetPath);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var tempOutputPath = command.OutputAssetPath + ".tmp";

                asset.Write(tempOutputPath);

                token.ThrowIfCancellationRequested();

                if (File.Exists(command.OutputAssetPath))
                {
                    File.Delete(command.OutputAssetPath);
                }
                File.Move(tempOutputPath, command.OutputAssetPath);

                var producedFiles = new List<string> { command.OutputAssetPath };

                var uexpPath = Path.ChangeExtension(tempOutputPath, ".uexp");
                var ubulkPath = Path.ChangeExtension(tempOutputPath, ".ubulk");

                if (File.Exists(uexpPath))
                {
                    var finalUexpPath = Path.ChangeExtension(command.OutputAssetPath, ".uexp");
                    if (File.Exists(finalUexpPath))
                    {
                        File.Delete(finalUexpPath);
                    }
                    File.Move(uexpPath, finalUexpPath);
                    producedFiles.Add(finalUexpPath);
                }

                if (File.Exists(ubulkPath))
                {
                    var finalUbulkPath = Path.ChangeExtension(command.OutputAssetPath, ".ubulk");
                    if (File.Exists(finalUbulkPath))
                    {
                        File.Delete(finalUbulkPath);
                    }
                    File.Move(ubulkPath, finalUbulkPath);
                    producedFiles.Add(finalUbulkPath);
                }

                var detectedVersion = DetermineUEVersionString(asset);

                var warnings = new List<string>();
                if (!string.IsNullOrEmpty(command.CompressionFormat))
                {
                    warnings.Add($"CompressionFormat '{command.CompressionFormat}' was specified but is not yet supported by ARIS UAssetAPI integration.");
                }
                if (command.CompressionLevel.HasValue)
                {
                    warnings.Add($"CompressionLevel '{command.CompressionLevel.Value}' was specified but is not yet supported by ARIS UAssetAPI integration.");
                }

                var result = new UAssetBackendResult
                {
                    OutputPath = command.OutputAssetPath,
                    ProducedFilePaths = producedFiles.ToArray(),
                    Warnings = warnings,
                    DetectedUEVersion = detectedVersion,
                    UsedSchemaVersion = command.SchemaVersion ?? "uassetapi-native"
                };

                _logger.LogDebug("Asset serialization complete: {OutputPath} ({Size} bytes)",
                    command.OutputAssetPath, new FileInfo(command.OutputAssetPath).Length);

                return result;
            }, token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError("Asset serialization timed out after {TimeoutSeconds} seconds: {Path}",
                timeoutSeconds, command.InputJsonPath);

            throw new ToolExecutionError(
                "uassetapi",
                -1,
                $"Asset serialization timed out after {timeoutSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Asset serialization cancelled: {Path}", command.InputJsonPath);
            throw;
        }
        catch (Exception ex) when (ex is not ArisException)
        {
            _logger.LogError(ex, "Failed to serialize asset: {Path}", command.InputJsonPath);

            throw new ToolExecutionError(
                "uassetapi",
                -1,
                $"Failed to serialize asset from '{Path.GetFileName(command.InputJsonPath)}': {ex.Message}");
        }
    }

    public async Task<UAssetBackendResult> DeserializeAsync(
        UAssetDeserializeCommand command,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deserializing asset with UAssetAPI: {InputPath} → {OutputPath}",
            command.InputAssetPath, command.OutputJsonPath);

        // Determine timeout and create linked cancellation token
        var timeoutSeconds = command.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var token = linkedCts.Token;

        try
        {
            return await Task.Run(() =>
            {
                // Validate input file exists
                if (!File.Exists(command.InputAssetPath))
                {
                    throw new FileNotFoundException($"Input asset file not found: {command.InputAssetPath}");
                }

                token.ThrowIfCancellationRequested();

                // Parse engine version (prefer explicit, fallback to auto-detect)
                var engineVersion = ParseEngineVersion(command.UEVersion);

                // Load asset with UAssetAPI
                var asset = new global::UAssetAPI.UAsset(command.InputAssetPath, engineVersion);

                token.ThrowIfCancellationRequested();

                // Serialize to JSON using UAssetAPI's built-in method
                // NOTE: UAssetAPI's SerializeJson() always includes all asset data (including bulk data).
                // There is no native option to exclude bulk data, so the IncludeBulkData command option
                // cannot be enforced at this layer. A warning is added to the result if the user
                // requested IncludeBulkData = false.
                var jsonContent = asset.SerializeJson(isFormatted: true);

                token.ThrowIfCancellationRequested();

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(command.OutputJsonPath);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Write JSON to output file
                File.WriteAllText(command.OutputJsonPath, jsonContent);

                // Detect produced files (main JSON + any sidecars if applicable)
                var producedFiles = new List<string> { command.OutputJsonPath };

                // Detect UE version from loaded asset
                var detectedVersion = DetermineUEVersionString(asset);

                // Build warnings list
                var warnings = new List<string>();
                if (!command.IncludeBulkData)
                {
                    warnings.Add("IncludeBulkData was set to false, but UAssetAPI's SerializeJson() always includes all asset data. Bulk data is present in the output JSON.");
                }

                // Build result
                var result = new UAssetBackendResult
                {
                    OutputPath = command.OutputJsonPath,
                    ProducedFilePaths = producedFiles.ToArray(),
                    Warnings = warnings,
                    DetectedUEVersion = detectedVersion,
                    UsedSchemaVersion = command.SchemaVersion ?? "uassetapi-native"
                };

                _logger.LogDebug("Asset deserialization complete: {OutputPath} ({Size} bytes)",
                    command.OutputJsonPath, new FileInfo(command.OutputJsonPath).Length);

                return result;
            }, token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError("Asset deserialization timed out after {TimeoutSeconds} seconds: {Path}",
                timeoutSeconds, command.InputAssetPath);

            throw new ToolExecutionError(
                "uassetapi",
                -1,
                $"Asset deserialization timed out after {timeoutSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Asset deserialization cancelled: {Path}", command.InputAssetPath);
            throw;
        }
        catch (Exception ex) when (ex is not ArisException)
        {
            _logger.LogError(ex, "Failed to deserialize asset: {Path}", command.InputAssetPath);

            throw new ToolExecutionError(
                "uassetapi",
                -1,
                $"Failed to deserialize asset '{Path.GetFileName(command.InputAssetPath)}': {ex.Message}");
        }
    }

    public Task<UAssetInspection> InspectAsync(
        UAssetInspectCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Inspecting asset with UAssetAPI: {Path}", command.InputAssetPath);

        try
        {
            var asset = new global::UAssetAPI.UAsset(command.InputAssetPath, EngineVersion.UNKNOWN);

            var ueVersionString = DetermineUEVersionString(asset);

            var summary = new UAssetSummary
            {
                UEVersion = ueVersionString,
                LicenseeVersion = asset.FileVersionLicenseeUE,
                CustomVersionCount = asset.CustomVersionContainer?.Count ?? 0,
                NameCount = asset.GetNameMapIndexList().Count,
                ExportCount = asset.Exports.Count,
                ImportCount = asset.Imports.Count
            };

            string[]? exports = null;
            string[]? imports = null;
            string[]? names = null;

            if (command.Fields.Contains("exports"))
            {
                exports = asset.Exports
                    .Select((export, index) => $"[{index}] {export.ObjectName?.ToString() ?? "(null)"}")
                    .ToArray();
            }

            if (command.Fields.Contains("imports"))
            {
                imports = asset.Imports
                    .Select((import, index) => $"[{index}] {import.ObjectName?.ToString() ?? "(null)"}")
                    .ToArray();
            }

            if (command.Fields.Contains("names"))
            {
                var nameList = asset.GetNameMapIndexList();
                names = nameList
                    .Select((name, index) => $"[{index}] {name?.ToString() ?? "(null)"}")
                    .ToArray();
            }

            var inspection = new UAssetInspection
            {
                InputPath = command.InputAssetPath,
                Summary = summary,
                Exports = exports,
                Imports = imports,
                Names = names
            };

            _logger.LogDebug(
                "Asset inspection complete: {ExportCount} exports, {ImportCount} imports, {NameCount} names",
                summary.ExportCount, summary.ImportCount, summary.NameCount);

            return Task.FromResult(inspection);
        }
        catch (Exception ex) when (ex is not ArisException)
        {
            _logger.LogError(ex, "Failed to inspect asset: {Path}", command.InputAssetPath);

            throw new ToolExecutionError(
                "uassetapi",
                -1,
                $"Failed to inspect asset '{Path.GetFileName(command.InputAssetPath)}': {ex.Message}");
        }
    }

    private static EngineVersion ParseEngineVersion(string? ueVersion)
    {
        if (string.IsNullOrEmpty(ueVersion))
        {
            return EngineVersion.UNKNOWN;
        }

        return ueVersion switch
        {
            "4.27" => EngineVersion.VER_UE4_27,
            "5.0" => EngineVersion.VER_UE5_0,
            "5.1" => EngineVersion.VER_UE5_1,
            "5.2" => EngineVersion.VER_UE5_2,
            "5.3" => EngineVersion.VER_UE5_3,
            "5.4" => EngineVersion.VER_UE5_4,
            "5.5" => EngineVersion.VER_UE5_5,
            _ => EngineVersion.UNKNOWN
        };
    }

    private static string DetermineUEVersionString(global::UAssetAPI.UAsset asset)
    {
        if (asset.ObjectVersionUE5 != ObjectVersionUE5.UNKNOWN)
        {
            var ue5Version = asset.ObjectVersionUE5.ToString();
            if (ue5Version.StartsWith("VER_UE5_"))
            {
                return "5." + ue5Version.Substring(8);
            }
            return "5.x";
        }

        if (asset.ObjectVersion != ObjectVersion.UNKNOWN)
        {
            var engineVersion = asset.GetEngineVersion();
            return engineVersion.ToString().Replace("VER_UE", "").Replace("_", ".");
        }

        return "Unknown";
    }
}
