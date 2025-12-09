using System.Collections.Generic;

namespace Aris.Contracts;

/// <summary>
/// Basic backend metadata and environment info.
/// </summary>
public sealed record InfoResponse(
    /// <summary>
    /// Backend version string (assembly version).
    /// </summary>
    string Version,
    /// <summary>
    /// Base URL the backend believes it is running on (scheme + host).
    /// </summary>
    string BackendBaseUrl,
    /// <summary>
    /// Optional IPC auth token (null until Phase 5 auth wiring is implemented).
    /// </summary>
    string? IpcToken,
    /// <summary>
    /// Known tool IDs and their versions (from tools manifest, if available).
    /// </summary>
    IDictionary<string, string> ToolVersions
);
