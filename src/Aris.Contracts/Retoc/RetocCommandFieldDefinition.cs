namespace Aris.Contracts.Retoc;

/// <summary>
/// Defines a single field in a Retoc command schema.
/// Used to render dynamic UI controls in Advanced Mode.
/// </summary>
public sealed class RetocCommandFieldDefinition
{
    /// <summary>
    /// Field name matching the DTO property (e.g., "InputPath", "ChunkIndex").
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// UI label for the field.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Field type: "Path", "Integer", "Enum", "String", "Boolean".
    /// </summary>
    public required string FieldType { get; init; }

    /// <summary>
    /// Help text displayed below the field.
    /// </summary>
    public string? HelpText { get; init; }

    /// <summary>
    /// For Enum fields: allowed values.
    /// </summary>
    public string[]? EnumValues { get; init; }

    /// <summary>
    /// For Integer fields: minimum value.
    /// </summary>
    public int? MinValue { get; init; }

    /// <summary>
    /// For Integer fields: maximum value.
    /// </summary>
    public int? MaxValue { get; init; }

    /// <summary>
    /// For Path fields: "file" or "folder" to indicate picker type.
    /// </summary>
    public string? PathKind { get; init; }

    /// <summary>
    /// For Path fields with PathKind="file": allowed extensions (e.g., [".utoc"]).
    /// </summary>
    public string[]? Extensions { get; init; }
}
