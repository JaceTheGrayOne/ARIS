namespace Aris.Contracts.Retoc;

/// <summary>
/// Schema describing all supported Retoc commands and fields.
/// Used to render the Advanced Mode UI dynamically.
/// </summary>
public sealed class RetocCommandSchemaResponse
{
    /// <summary>
    /// Definitions for all supported Retoc commands.
    /// </summary>
    public required RetocCommandDefinition[] Commands { get; init; }

    /// <summary>
    /// Global options available for all commands.
    /// </summary>
    public required RetocCommandFieldDefinition[] GlobalOptions { get; init; }

    /// <summary>
    /// Allowlisted boolean flags (e.g., "--verbose", "--no-warnings").
    /// </summary>
    public required string[] AllowlistedFlags { get; init; }
}
