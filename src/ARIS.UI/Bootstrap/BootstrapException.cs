namespace ARIS.UI.Bootstrap;

/// <summary>
/// Base exception for bootstrap-related failures.
/// </summary>
public class BootstrapException : Exception
{
    public string ErrorCode { get; }
    public string? RemediationHint { get; }

    public BootstrapException(string errorCode, string message, string? remediationHint = null)
        : base(message)
    {
        ErrorCode = errorCode;
        RemediationHint = remediationHint;
    }

    public BootstrapException(string errorCode, string message, Exception? innerException, string? remediationHint = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        RemediationHint = remediationHint;
    }
}

/// <summary>
/// Thrown when payload extraction fails.
/// </summary>
public sealed class PayloadExtractionException : BootstrapException
{
    private const string DefaultRemediation = "Try deleting the ARIS folder in %LOCALAPPDATA% and restarting the application.";

    public PayloadExtractionException(string message)
        : base("PAYLOAD_EXTRACTION_FAILED", message, DefaultRemediation)
    {
    }

    public PayloadExtractionException(string message, Exception innerException)
        : base("PAYLOAD_EXTRACTION_FAILED", message, innerException, DefaultRemediation)
    {
    }
}

/// <summary>
/// Thrown when the embedded payload resource is not found.
/// </summary>
public sealed class PayloadNotFoundException : BootstrapException
{
    public PayloadNotFoundException()
        : base("PAYLOAD_NOT_FOUND", "The embedded payload resource was not found.",
            "This may be a development build. Try running the publish script.")
    {
    }
}

/// <summary>
/// Thrown when backend process fails to start.
/// </summary>
public sealed class BackendStartException : BootstrapException
{
    private const string DefaultRemediation = "Check the ARIS logs in %LOCALAPPDATA%\\ARIS\\logs for details.";

    public BackendStartException(string message)
        : base("BACKEND_START_FAILED", message, DefaultRemediation)
    {
    }

    public BackendStartException(string message, Exception innerException)
        : base("BACKEND_START_FAILED", message, innerException, DefaultRemediation)
    {
    }
}

/// <summary>
/// Thrown when backend URL announcement times out.
/// </summary>
public sealed class BackendUrlTimeoutException : BootstrapException
{
    public TimeSpan Timeout { get; }

    public BackendUrlTimeoutException(TimeSpan timeout)
        : base("BACKEND_URL_TIMEOUT",
            $"The backend did not announce its URL within {timeout.TotalSeconds:F0} seconds.",
            "Check the ARIS logs in %LOCALAPPDATA%\\ARIS\\logs for startup errors.")
    {
        Timeout = timeout;
    }
}

/// <summary>
/// Thrown when backend readiness polling times out.
/// </summary>
public sealed class BackendReadinessTimeoutException : BootstrapException
{
    public TimeSpan Timeout { get; }
    public string? LastStatus { get; }

    public BackendReadinessTimeoutException(TimeSpan timeout, string? lastStatus = null)
        : base("BACKEND_READINESS_TIMEOUT",
            $"The backend did not become ready within {timeout.TotalSeconds:F0} seconds. Last status: {lastStatus ?? "unknown"}",
            "Check the ARIS logs in %LOCALAPPDATA%\\ARIS\\logs for startup errors.")
    {
        Timeout = timeout;
        LastStatus = lastStatus;
    }
}
