namespace Aris.Core.UAsset;

/// <summary>
/// Result of asset inspection containing metadata without full deserialization.
/// </summary>
public class UAssetInspection
{
    /// <summary>
    /// Path to the inspected asset.
    /// </summary>
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Asset summary information.
    /// </summary>
    public UAssetSummary Summary { get; init; } = new();

    /// <summary>
    /// Export table (optional, included if requested).
    /// </summary>
    public IReadOnlyList<string>? Exports { get; init; }

    /// <summary>
    /// Import table (optional, included if requested).
    /// </summary>
    public IReadOnlyList<string>? Imports { get; init; }

    /// <summary>
    /// Name table (optional, included if requested).
    /// </summary>
    public IReadOnlyList<string>? Names { get; init; }
}

/// <summary>
/// Summary information for an inspected asset.
/// </summary>
public class UAssetSummary
{
    /// <summary>
    /// Unreal Engine version.
    /// </summary>
    public string? UEVersion { get; init; }

    /// <summary>
    /// Licensee version.
    /// </summary>
    public int LicenseeVersion { get; init; }

    /// <summary>
    /// Custom version container count.
    /// </summary>
    public int CustomVersionCount { get; init; }

    /// <summary>
    /// Number of name table entries.
    /// </summary>
    public int NameCount { get; init; }

    /// <summary>
    /// Number of export table entries.
    /// </summary>
    public int ExportCount { get; init; }

    /// <summary>
    /// Number of import table entries.
    /// </summary>
    public int ImportCount { get; init; }
}
