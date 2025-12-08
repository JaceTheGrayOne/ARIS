namespace Aris.Core.Errors;

/// <summary>
/// Thrown when asset to JSON deserialization fails.
/// </summary>
public class DeserializationError : ArisException
{
    /// <summary>
    /// Path to the input asset file that failed to deserialize.
    /// </summary>
    public string? InputPath { get; init; }

    /// <summary>
    /// Unreal Engine version of the asset.
    /// </summary>
    public string? UEVersion { get; init; }

    public DeserializationError(string message)
        : base("DESERIALIZATION_ERROR", message)
    {
    }

    public DeserializationError(string message, Exception innerException)
        : base("DESERIALIZATION_ERROR", message, innerException)
    {
    }
}
