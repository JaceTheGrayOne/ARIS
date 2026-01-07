namespace Aris.Contracts.Retoc;

/// <summary>
/// Request to stream a Retoc execution via WebSocket with ConPTY terminal output.
/// </summary>
public sealed class RetocStreamRequest
{
    /// <summary>
    /// The specific Retoc command to execute (e.g., "ToZen", "ToLegacy", "Unpack").
    /// </summary>
    public required string CommandType { get; init; }

    /// <summary>
    /// Input path (file or directory depending on command).
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Output path (file or directory depending on command).
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Engine version (e.g., "UE5_6"). Used by ToZen command.
    /// </summary>
    public string? EngineVersion { get; init; }

    /// <summary>
    /// AES encryption key for encrypted containers.
    /// </summary>
    public string? AesKey { get; init; }

    /// <summary>
    /// Container header version override.
    /// </summary>
    public string? ContainerHeaderVersion { get; init; }

    /// <summary>
    /// TOC version override.
    /// </summary>
    public string? TocVersion { get; init; }

    /// <summary>
    /// Chunk ID for the Get command.
    /// </summary>
    public string? ChunkId { get; init; }

    /// <summary>
    /// Enable verbose output.
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Timeout in seconds for command execution.
    /// </summary>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Enable TTY probe mode for diagnostics (instead of running Retoc).
    /// </summary>
    public bool TtyProbe { get; init; }
}
