namespace Aris.Core.Errors;

/// <summary>
/// Thrown when JSON to asset serialization fails.
/// </summary>
public class SerializationError : ArisException
{
    /// <summary>
    /// Path to the input JSON file that failed to serialize.
    /// </summary>
    public string? InputPath { get; init; }

    /// <summary>
    /// Schema version used during serialization.
    /// </summary>
    public string? SchemaVersion { get; init; }

    public SerializationError(string message)
        : base("SERIALIZATION_ERROR", message)
    {
    }

    public SerializationError(string message, Exception innerException)
        : base("SERIALIZATION_ERROR", message, innerException)
    {
    }
}
