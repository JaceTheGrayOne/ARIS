namespace Aris.Contracts.Retoc;

/// <summary>
/// Response containing Retoc help text as Markdown.
/// </summary>
public sealed class RetocHelpResponse
{
    /// <summary>
    /// Retoc help output wrapped in Markdown code fence.
    /// </summary>
    public required string Markdown { get; init; }
}
