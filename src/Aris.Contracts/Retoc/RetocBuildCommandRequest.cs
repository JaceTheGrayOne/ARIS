namespace Aris.Contracts.Retoc;

/// <summary>
/// Request to build a Retoc command for preview or execution.
/// All Retoc functionality is exposed via structured fields only.
/// </summary>
public sealed class RetocBuildCommandRequest
{
    /// <summary>
    /// The specific Retoc command to execute.
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
    /// Container header version override (e.g., "Initial", "LocalizedPackages", "OptimizedNames").
    /// </summary>
    public string? ContainerHeaderVersion { get; init; }

    /// <summary>
    /// TOC version override (e.g., "DirectoryIndex", "PartitionSize", "PerfectHash", "PerfectHashWithOverflow").
    /// </summary>
    public string? TocVersion { get; init; }

    /// <summary>
    /// Chunk ID for the Get command (required by Get, unused by others).
    /// </summary>
    public string? ChunkId { get; init; }

    /// <summary>
    /// Enable verbose output (supported by to-legacy, to-zen, unpack).
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Timeout in seconds for command execution.
    /// </summary>
    public int? TimeoutSeconds { get; init; }
}
