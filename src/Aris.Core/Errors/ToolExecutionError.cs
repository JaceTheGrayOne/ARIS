namespace Aris.Core.Errors;

/// <summary>
/// Thrown when an external tool process exits with a non-zero exit code or fails to execute.
/// </summary>
public class ToolExecutionError : ArisException
{
    /// <summary>
    /// Name or identifier of the tool that failed.
    /// </summary>
    public string ToolName { get; }

    /// <summary>
    /// Exit code returned by the process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Command line that was executed (with sensitive data redacted).
    /// </summary>
    public string? CommandLine { get; init; }

    /// <summary>
    /// Captured standard output (truncated).
    /// </summary>
    public string? StandardOutput { get; init; }

    /// <summary>
    /// Captured standard error (truncated).
    /// </summary>
    public string? StandardError { get; init; }

    public ToolExecutionError(string toolName, int exitCode, string message)
        : base("TOOL_EXECUTION_ERROR", message)
    {
        ToolName = toolName;
        ExitCode = exitCode;
    }

    public ToolExecutionError(string toolName, int exitCode, string message, Exception innerException)
        : base("TOOL_EXECUTION_ERROR", message, innerException)
    {
        ToolName = toolName;
        ExitCode = exitCode;
    }
}
