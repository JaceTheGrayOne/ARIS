namespace Aris.Core.UAsset;

/// <summary>
/// Type of UAsset operation.
/// </summary>
public enum UAssetOperation
{
    /// <summary>
    /// Convert JSON to binary asset (*.uasset).
    /// </summary>
    Serialize,

    /// <summary>
    /// Convert binary asset to JSON.
    /// </summary>
    Deserialize,

    /// <summary>
    /// Inspect asset metadata without full deserialization.
    /// </summary>
    Inspect
}
