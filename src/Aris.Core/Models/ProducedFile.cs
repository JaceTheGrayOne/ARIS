namespace Aris.Core.Models;

/// <summary>
/// Metadata for a file produced by an operation.
/// </summary>
public class ProducedFile
{
    /// <summary>
    /// Absolute path to the produced file.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// SHA-256 hash of the file contents.
    /// </summary>
    public string? Sha256 { get; init; }

    /// <summary>
    /// File type or role (e.g., "pak", "utoc", "ucas", "uasset", "uexp").
    /// </summary>
    public string? FileType { get; init; }

    public ProducedFile()
    {
    }

    public ProducedFile(string path, long sizeBytes, string? sha256 = null, string? fileType = null)
    {
        Path = path;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        FileType = fileType;
    }
}
