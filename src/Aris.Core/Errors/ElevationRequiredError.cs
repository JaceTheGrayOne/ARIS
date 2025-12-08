namespace Aris.Core.Errors;

/// <summary>
/// Thrown when an operation requires elevation (UAC) and it is not granted or available.
/// </summary>
public class ElevationRequiredError : ArisException
{
    /// <summary>
    /// Operation identifier for tracking and diagnostics.
    /// </summary>
    public string? OperationId { get; init; }

    public ElevationRequiredError(string message)
        : base("ELEVATION_REQUIRED", message)
    {
    }

    public ElevationRequiredError(string message, Exception innerException)
        : base("ELEVATION_REQUIRED", message, innerException)
    {
    }
}
