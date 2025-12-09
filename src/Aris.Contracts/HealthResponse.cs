namespace Aris.Contracts;

/// <summary>
/// Basic health status for the backend.
/// </summary>
public sealed record HealthResponse(
    /// <summary>
    /// Overall status: "Ready", "Starting", or "Error".
    /// </summary>
    string Status,
    /// <summary>
    /// True if all critical dependencies (tools, workspace) are considered ready.
    /// </summary>
    bool DependenciesReady,
    /// <summary>
    /// Current workspace root path, if configured.
    /// </summary>
    string? CurrentWorkspace,
    /// <summary>
    /// Human-readable message describing current status.
    /// </summary>
    string? Message
);
