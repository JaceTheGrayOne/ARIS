namespace Aris.Core.UwpDumper;

/// <summary>
/// Supported UWPDumper operation modes.
/// </summary>
public enum UwpDumpMode
{
    /// <summary>
    /// Perform a complete dump of the UWP package, including all binaries, resources, and metadata.
    /// </summary>
    FullDump,

    /// <summary>
    /// Extract only metadata, headers, and SDK-like artifacts without dumping full package contents.
    /// </summary>
    MetadataOnly,

    /// <summary>
    /// Validate package structure and accessibility without extracting or dumping contents.
    /// </summary>
    ValidateOnly
}
