namespace Aris.Contracts.Retoc;

/// <summary>
/// HTTP-transport version of a produced file record.
/// </summary>
public sealed record ProducedFileDto(
    /// <summary>
    /// Path to the produced file.
    /// </summary>
    string Path,
    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    long SizeBytes,
    /// <summary>
    /// SHA-256 hash of the file, if computed.
    /// </summary>
    string? Sha256,
    /// <summary>
    /// File type or format hint.
    /// </summary>
    string? FileType
);
