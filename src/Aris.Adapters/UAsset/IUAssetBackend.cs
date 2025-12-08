using Aris.Core.UAsset;

namespace Aris.Adapters.UAsset;

/// <summary>
/// Abstraction for the actual UAssetAPI integration.
/// This allows us to stub the real UAssetAPI work while testing the service orchestration.
/// </summary>
public interface IUAssetBackend
{
    /// <summary>
    /// Performs the actual JSON to asset serialization using UAssetAPI.
    /// </summary>
    /// <param name="command">The serialization command.</param>
    /// <param name="stagingDirectory">Temporary staging directory for intermediate files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing produced files and metadata.</returns>
    Task<UAssetBackendResult> SerializeAsync(
        UAssetSerializeCommand command,
        string stagingDirectory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Performs the actual asset to JSON deserialization using UAssetAPI.
    /// </summary>
    /// <param name="command">The deserialization command.</param>
    /// <param name="stagingDirectory">Temporary staging directory for intermediate files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing produced files and metadata.</returns>
    Task<UAssetBackendResult> DeserializeAsync(
        UAssetDeserializeCommand command,
        string stagingDirectory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Performs asset metadata inspection using UAssetAPI.
    /// </summary>
    /// <param name="command">The inspection command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inspection result with asset metadata.</returns>
    Task<UAssetInspection> InspectAsync(
        UAssetInspectCommand command,
        CancellationToken cancellationToken);
}

/// <summary>
/// Result from a backend serialization or deserialization operation.
/// </summary>
public class UAssetBackendResult
{
    public string OutputPath { get; init; } = string.Empty;
    public string[] ProducedFilePaths { get; init; } = Array.Empty<string>();
    public List<string> Warnings { get; init; } = new();
    public string? DetectedUEVersion { get; init; }
    public string? UsedSchemaVersion { get; init; }
}
