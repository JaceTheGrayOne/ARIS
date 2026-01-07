namespace Aris.Contracts.Retoc;

/// <summary>
/// UI hints for a specific field in a Retoc command.
/// Used to configure path picker behavior in the frontend.
/// </summary>
public sealed class RetocFieldUiHint
{
    /// <summary>
    /// For Path fields: "file" or "folder" to indicate picker type.
    /// </summary>
    public string? PathKind { get; init; }

    /// <summary>
    /// For Path fields with PathKind="file": allowed extensions (e.g., [".utoc"]).
    /// </summary>
    public string[]? Extensions { get; init; }
}
