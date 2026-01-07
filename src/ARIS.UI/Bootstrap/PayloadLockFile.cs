using System.Text.Json.Serialization;

namespace ARIS.UI.Bootstrap;

/// <summary>
/// Data model for the payload extraction lock file.
/// Used to track whether the payload needs re-extraction.
/// </summary>
public sealed class PayloadLockFile
{
    /// <summary>
    /// SHA-256 hash of the embedded payload resource.
    /// </summary>
    [JsonPropertyName("payloadHash")]
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when extraction occurred.
    /// </summary>
    [JsonPropertyName("extractedAt")]
    public DateTimeOffset ExtractedAt { get; set; }

    /// <summary>
    /// Version from assembly metadata at extraction time.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
