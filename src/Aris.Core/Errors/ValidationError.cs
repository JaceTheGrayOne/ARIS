namespace Aris.Core.Errors;

/// <summary>
/// Thrown when command arguments, paths, or configuration fail validation.
/// </summary>
public class ValidationError : ArisException
{
    /// <summary>
    /// Name of the field or parameter that failed validation.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// The invalid value that was provided (may be redacted for sensitive data).
    /// </summary>
    public string? InvalidValue { get; init; }

    public ValidationError(string message)
        : base("VALIDATION_ERROR", message)
    {
    }

    public ValidationError(string message, string fieldName)
        : base("VALIDATION_ERROR", message)
    {
        FieldName = fieldName;
    }

    public ValidationError(string message, string fieldName, string invalidValue)
        : base("VALIDATION_ERROR", message)
    {
        FieldName = fieldName;
        InvalidValue = invalidValue;
    }
}
