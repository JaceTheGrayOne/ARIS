namespace Aris.Contracts.UAsset;

/// <summary>
/// Summary information for an inspected asset.
/// </summary>
public sealed record UAssetSummaryDto(
    /// <summary>
    /// Unreal Engine version.
    /// </summary>
    string? UEVersion,
    /// <summary>
    /// Licensee version.
    /// </summary>
    int LicenseeVersion,
    /// <summary>
    /// Custom version container count.
    /// </summary>
    int CustomVersionCount,
    /// <summary>
    /// Number of name table entries.
    /// </summary>
    int NameCount,
    /// <summary>
    /// Number of export table entries.
    /// </summary>
    int ExportCount,
    /// <summary>
    /// Number of import table entries.
    /// </summary>
    int ImportCount
);
