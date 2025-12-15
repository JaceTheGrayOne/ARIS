namespace Aris.Core.Retoc;

/// <summary>
/// Container header version values for --override-container-header-version flag.
/// Maps directly to retoc's EIoContainerHeaderVersion enum.
/// See: https://github.com/trumank/retoc
/// </summary>
public enum RetocContainerHeaderVersion
{
    /// <summary>
    /// Pre-initial container header version.
    /// </summary>
    PreInitial,

    /// <summary>
    /// Initial container header version.
    /// </summary>
    Initial,

    /// <summary>
    /// Localized packages support.
    /// </summary>
    LocalizedPackages,

    /// <summary>
    /// Optional segment packages support.
    /// </summary>
    OptionalSegmentPackages,

    /// <summary>
    /// No export info.
    /// </summary>
    NoExportInfo,

    /// <summary>
    /// Soft package references.
    /// </summary>
    SoftPackageReferences,

    /// <summary>
    /// Soft package references with offset.
    /// </summary>
    SoftPackageReferencesOffset
}
