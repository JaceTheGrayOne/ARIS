using System.Text.Json.Serialization;

namespace Aris.Contracts.Retoc;

/// <summary>
/// Base type for Retoc stream events (NDJSON discriminated union).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RetocStreamStarted), "started")]
[JsonDerivedType(typeof(RetocStreamOutput), "output")]
[JsonDerivedType(typeof(RetocStreamExited), "exited")]
[JsonDerivedType(typeof(RetocStreamError), "error")]
public abstract record RetocStreamEvent
{
    /// <summary>
    /// Timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Signals stream has started with operation metadata.
/// </summary>
public sealed record RetocStreamStarted(
    /// <summary>
    /// Unique identifier for this operation.
    /// </summary>
    string OperationId,
    /// <summary>
    /// The full command line being executed.
    /// </summary>
    string CommandLine
) : RetocStreamEvent;

/// <summary>
/// Terminal output data (VT/ANSI sequences suitable for xterm).
/// With ConPTY, stdout and stderr are merged into a single VT stream.
/// </summary>
public sealed record RetocStreamOutput(
    /// <summary>
    /// Raw terminal output (UTF-8 with VT sequences).
    /// </summary>
    string Data
) : RetocStreamEvent;

/// <summary>
/// Signals the process has exited.
/// </summary>
public sealed record RetocStreamExited(
    /// <summary>
    /// Process exit code.
    /// </summary>
    int ExitCode,
    /// <summary>
    /// Total execution duration.
    /// </summary>
    TimeSpan Duration
) : RetocStreamEvent;

/// <summary>
/// Error occurred during streaming.
/// </summary>
public sealed record RetocStreamError(
    /// <summary>
    /// Error code identifying the type of error.
    /// </summary>
    string Code,
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    string Message,
    /// <summary>
    /// Optional hint for how to fix or work around the error.
    /// </summary>
    string? RemediationHint = null
) : RetocStreamEvent;
