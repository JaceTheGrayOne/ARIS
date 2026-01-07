namespace Aris.Contracts.Retoc;

/// <summary>
/// Response containing the built Retoc command for preview.
/// This is the single source of truth for command preview.
/// </summary>
public sealed class RetocBuildCommandResponse
{
    /// <summary>
    /// Full path to the retoc executable.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Ordered list of command-line arguments.
    /// </summary>
    public required string[] Arguments { get; init; }

    /// <summary>
    /// Human-readable command line string for UI preview.
    /// </summary>
    public required string CommandLine { get; init; }
}
