using Aris.Core.Models;
using Aris.Core.UAsset;

namespace Aris.Adapters.UAsset;

/// <summary>
/// Service for UAsset serialization, deserialization, and inspection operations.
/// </summary>
public interface IUAssetService
{
    /// <summary>
    /// Serializes JSON to a binary Unreal asset.
    /// </summary>
    /// <param name="command">The serialization command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result of the serialization operation.</returns>
    Task<UAssetResult> SerializeAsync(
        UAssetSerializeCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);

    /// <summary>
    /// Deserializes a binary Unreal asset to JSON.
    /// </summary>
    /// <param name="command">The deserialization command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result of the deserialization operation.</returns>
    Task<UAssetResult> DeserializeAsync(
        UAssetDeserializeCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);

    /// <summary>
    /// Inspects asset metadata without full deserialization.
    /// </summary>
    /// <param name="command">The inspection command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Inspection result containing asset metadata.</returns>
    Task<UAssetInspection> InspectAsync(
        UAssetInspectCommand command,
        CancellationToken cancellationToken = default,
        IProgress<ProgressEvent>? progress = null);
}
