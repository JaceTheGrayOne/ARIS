namespace Aris.Infrastructure.Tools;

/// <summary>
/// Status of a tool dependency validation.
/// </summary>
public enum DependencyStatus
{
    /// <summary>
    /// Tool is present and hash matches manifest.
    /// </summary>
    Valid,

    /// <summary>
    /// Tool file is missing.
    /// </summary>
    Missing,

    /// <summary>
    /// Tool exists but hash does not match manifest.
    /// </summary>
    HashMismatch,

    /// <summary>
    /// Tool validation was not performed or is unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Result of validating a single tool dependency.
/// </summary>
public class ToolValidationResult
{
    public string ToolId { get; init; } = string.Empty;
    public DependencyStatus Status { get; init; }
    public string? ExpectedPath { get; init; }
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsValid => Status == DependencyStatus.Valid;
}

/// <summary>
/// Result of validating all tool dependencies.
/// </summary>
public class DependencyValidationResult
{
    public IReadOnlyList<ToolValidationResult> ToolResults { get; init; } = Array.Empty<ToolValidationResult>();
    public bool AllValid => ToolResults.All(r => r.IsValid);
    public int ValidCount => ToolResults.Count(r => r.IsValid);
    public int TotalCount => ToolResults.Count;
}
