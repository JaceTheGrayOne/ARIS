namespace Aris.Contracts.UAsset;

/// <summary>
/// Result of asset inspection containing metadata without full deserialization.
/// </summary>
public sealed record UAssetInspectionDto(
    /// <summary>
    /// Operation identifier.
    /// </summary>
    string OperationId,
    /// <summary>
    /// Path to the inspected asset.
    /// </summary>
    string AssetPath,
    /// <summary>
    /// Asset summary information.
    /// </summary>
    UAssetSummaryDto Summary,
    /// <summary>
    /// Export table (optional, included if requested).
    /// </summary>
    IReadOnlyList<string>? Exports,
    /// <summary>
    /// Import table (optional, included if requested).
    /// </summary>
    IReadOnlyList<string>? Imports,
    /// <summary>
    /// Name table (optional, included if requested).
    /// </summary>
    IReadOnlyList<string>? Names,
    /// <summary>
    /// Excerpt from the operation log.
    /// </summary>
    string? LogExcerpt
);
