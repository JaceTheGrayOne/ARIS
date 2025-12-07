namespace Aris.Core.Retoc;

/// <summary>
/// Supported Retoc conversion modes.
/// </summary>
public enum RetocMode
{
    /// <summary>
    /// Convert from PAK format to IoStore format.
    /// </summary>
    PakToIoStore,

    /// <summary>
    /// Convert from IoStore format to PAK format.
    /// </summary>
    IoStoreToPak,

    /// <summary>
    /// Repack an existing package (same format, with filters or compression changes).
    /// </summary>
    Repack,

    /// <summary>
    /// Validate package integrity without conversion.
    /// </summary>
    Validate
}
