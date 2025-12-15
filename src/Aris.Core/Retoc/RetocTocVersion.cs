namespace Aris.Core.Retoc;

/// <summary>
/// Table of contents version values for --override-toc-version flag.
/// Maps directly to retoc's EIoStoreTocVersion enum.
/// See: https://github.com/trumank/retoc
/// </summary>
public enum RetocTocVersion
{
    /// <summary>
    /// Invalid TOC version.
    /// </summary>
    Invalid,

    /// <summary>
    /// Initial TOC version.
    /// </summary>
    Initial,

    /// <summary>
    /// Directory index support.
    /// </summary>
    DirectoryIndex,

    /// <summary>
    /// Partition size support.
    /// </summary>
    PartitionSize,

    /// <summary>
    /// Perfect hash support.
    /// </summary>
    PerfectHash,

    /// <summary>
    /// Perfect hash with overflow.
    /// </summary>
    PerfectHashWithOverflow,

    /// <summary>
    /// On-demand metadata.
    /// </summary>
    OnDemandMetaData,

    /// <summary>
    /// Removed on-demand metadata.
    /// </summary>
    RemovedOnDemandMetaData,

    /// <summary>
    /// Replace IO chunk hash with IO hash.
    /// </summary>
    ReplaceIoChunkHashWithIoHash
}
