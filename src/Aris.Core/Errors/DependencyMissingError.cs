namespace Aris.Core.Errors;

/// <summary>
/// Thrown when a required tool binary or dependency is missing or corrupt.
/// </summary>
public class DependencyMissingError : ArisException
{
    /// <summary>
    /// Identifier of the missing dependency (e.g., "retoc", "uassetapi").
    /// </summary>
    public string DependencyId { get; }

    /// <summary>
    /// Expected file path where the dependency should exist.
    /// </summary>
    public string? ExpectedPath { get; init; }

    public DependencyMissingError(string dependencyId, string message)
        : base("DEPENDENCY_MISSING", message)
    {
        DependencyId = dependencyId;
        RemediationHint = "Try restarting ARIS to re-extract dependencies, or reinstall if the issue persists.";
    }

    public DependencyMissingError(string dependencyId, string message, string expectedPath)
        : base("DEPENDENCY_MISSING", message)
    {
        DependencyId = dependencyId;
        ExpectedPath = expectedPath;
        RemediationHint = "Try restarting ARIS to re-extract dependencies, or reinstall if the issue persists.";
    }
}
