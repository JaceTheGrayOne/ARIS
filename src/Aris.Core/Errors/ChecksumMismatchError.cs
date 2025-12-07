namespace Aris.Core.Errors;

/// <summary>
/// Thrown when a file's computed hash does not match the expected hash.
/// </summary>
public class ChecksumMismatchError : ArisException
{
    /// <summary>
    /// Path to the file with mismatched checksum.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Expected hash value.
    /// </summary>
    public string ExpectedHash { get; init; }

    /// <summary>
    /// Actual computed hash value.
    /// </summary>
    public string ActualHash { get; init; }

    /// <summary>
    /// Hash algorithm used (e.g., "SHA256").
    /// </summary>
    public string Algorithm { get; init; }

    public ChecksumMismatchError(string filePath, string expectedHash, string actualHash, string algorithm = "SHA256")
        : base("CHECKSUM_MISMATCH", $"Checksum mismatch for {Path.GetFileName(filePath)}: expected {expectedHash[..8]}..., got {actualHash[..8]}...")
    {
        FilePath = filePath;
        ExpectedHash = expectedHash;
        ActualHash = actualHash;
        Algorithm = algorithm;
        RemediationHint = "The file may be corrupt or tampered with. Try reinstalling ARIS.";
    }
}
