namespace Aris.Core.UAsset;

/// <summary>
/// Command to inspect asset metadata without full deserialization.
/// </summary>
public class UAssetInspectCommand
{
    /// <summary>
    /// Absolute path to the input asset file (*.uasset).
    /// </summary>
    public string InputAssetPath { get; init; } = string.Empty;

    /// <summary>
    /// Optional list of fields to include in the inspection (e.g., "exports", "imports", "names").
    /// If empty, only summary information is returned.
    /// </summary>
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Operation identifier for logging.
    /// </summary>
    public string OperationId { get; init; } = Guid.NewGuid().ToString("N");
}
