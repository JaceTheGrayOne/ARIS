namespace Aris.Contracts.UwpDumper;

/// <summary>
/// Request for UWPDumper dump operation.
/// </summary>
public sealed record UwpDumpRequest(
    /// <summary>
    /// Package Family Name (PFN) of the target UWP application.
    /// </summary>
    string PackageFamilyName,
    /// <summary>
    /// Application ID or App User Model ID (optional).
    /// </summary>
    string? ApplicationId,
    /// <summary>
    /// Absolute path to the output directory or archive.
    /// </summary>
    string OutputPath,
    /// <summary>
    /// Dump operation mode (e.g., "FullDump", "MetadataOnly", "ValidateOnly").
    /// </summary>
    string Mode,
    /// <summary>
    /// Include symbol files in the dump (if available).
    /// </summary>
    bool IncludeSymbols
);
