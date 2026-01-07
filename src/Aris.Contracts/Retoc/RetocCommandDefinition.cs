namespace Aris.Contracts.Retoc;

/// <summary>
/// Defines a single Retoc command and its required/optional fields.
/// Used to dynamically render the Advanced Mode command builder UI.
/// </summary>
public sealed class RetocCommandDefinition
{
    /// <summary>
    /// Command type matching RetocCommandType enum (e.g., "ToLegacy", "ToZen").
    /// </summary>
    public required string CommandType { get; init; }

    /// <summary>
    /// Display name for UI (e.g., "Unpack (Zen → Legacy)").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description of what the command does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// List of required field names.
    /// </summary>
    public required string[] RequiredFields { get; init; }

    /// <summary>
    /// List of optional field names.
    /// </summary>
    public required string[] OptionalFields { get; init; }

    /// <summary>
    /// Per-field UI hints for this command (keyed by field name).
    /// Includes PathKind ("file" or "folder") and Extensions for path fields.
    /// </summary>
    public Dictionary<string, RetocFieldUiHint>? FieldUiHints { get; init; }
}
