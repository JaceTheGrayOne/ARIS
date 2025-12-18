using Aris.Core.UAsset;

namespace Aris.Adapters.UAsset;

/// <summary>
/// Stub implementation of IUAssetBackend for testing purposes.
/// Returns plausible data without calling real UAssetAPI.
/// Used by tests to validate UAssetService orchestration independently of UAssetAPI.
/// </summary>
public class StubUAssetBackend : IUAssetBackend
{
    public Task<UAssetBackendResult> SerializeAsync(
        UAssetSerializeCommand command,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        var outputPath = command.OutputAssetPath;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, "Stub .uasset content");

        var result = new UAssetBackendResult
        {
            OutputPath = outputPath,
            ProducedFilePaths = new[] { outputPath },
            Warnings = new List<string>(),
            DetectedUEVersion = command.UEVersion ?? "5.3",
            UsedSchemaVersion = command.SchemaVersion ?? "1.0"
        };

        return Task.FromResult(result);
    }

    public Task<UAssetBackendResult> DeserializeAsync(
        UAssetDeserializeCommand command,
        string stagingDirectory,
        CancellationToken cancellationToken)
    {
        var outputPath = command.OutputJsonPath;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, "{ \"stub\": \"json content\" }");

        var result = new UAssetBackendResult
        {
            OutputPath = outputPath,
            ProducedFilePaths = new[] { outputPath },
            Warnings = new List<string>(),
            DetectedUEVersion = command.UEVersion ?? "5.3",
            UsedSchemaVersion = command.SchemaVersion ?? "1.0"
        };

        return Task.FromResult(result);
    }

    public Task<UAssetInspection> InspectAsync(
        UAssetInspectCommand command,
        CancellationToken cancellationToken)
    {
        var inspection = new UAssetInspection
        {
            InputPath = command.InputAssetPath,
            Summary = new UAssetSummary
            {
                UEVersion = "5.3",
                LicenseeVersion = 0,
                CustomVersionCount = 10,
                NameCount = 100,
                ExportCount = 50,
                ImportCount = 75
            },
            Exports = command.Fields.Contains("exports") ? new[] { "Export1", "Export2" } : null,
            Imports = command.Fields.Contains("imports") ? new[] { "Import1", "Import2" } : null,
            Names = command.Fields.Contains("names") ? new[] { "Name1", "Name2" } : null
        };

        return Task.FromResult(inspection);
    }
}
