namespace Aris.Contracts.UAsset;

/// <summary>
/// Request for UAsset inspect operation (metadata inspection without full deserialization).
/// </summary>
public sealed record UAssetInspectRequest(
    /// <summary>
    /// Absolute path to the input asset file (*.uasset).
    /// </summary>
    string InputAssetPath,
    /// <summary>
    /// Optional list of fields to include (e.g., "Summary", "Exports", "Imports", "Names").
    /// If null or empty, only summary information is returned.
    /// </summary>
    IReadOnlyList<string>? Fields
);
