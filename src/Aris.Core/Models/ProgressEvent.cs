namespace Aris.Core.Models;

/// <summary>
/// Represents a step-level progress event for long-running operations.
/// </summary>
public class ProgressEvent
{
    /// <summary>
    /// Identifier for the current step (e.g., "staging", "decrypting", "converting").
    /// </summary>
    public string Step { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable message describing the current activity.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional progress percentage (0-100).
    /// </summary>
    public double? Percent { get; init; }

    /// <summary>
    /// Optional additional detail (e.g., file name, byte count).
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Timestamp when this event was generated.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public ProgressEvent()
    {
    }

    public ProgressEvent(string step, string message, double? percent = null, string? detail = null)
    {
        Step = step;
        Message = message;
        Percent = percent;
        Detail = detail;
    }
}
