using System.Threading;

namespace Aris.Hosting.Infrastructure;

/// <summary>
/// Mutable backend health state used by startup initialization and HTTP endpoints.
/// </summary>
public sealed class BackendHealthState
{
    private int _dependenciesReady;
    private string _status = "Starting";
    private string? _message;

    public string Status
    {
        get => _status;
        set => _status = value ?? "Unknown";
    }

    public bool DependenciesReady
    {
        get => Interlocked.CompareExchange(ref _dependenciesReady, 0, 0) == 1;
        set => Interlocked.Exchange(ref _dependenciesReady, value ? 1 : 0);
    }

    public string? Message
    {
        get => _message;
        set => _message = value;
    }

    public void MarkReady(string? message = null)
    {
        Status = "Ready";
        DependenciesReady = true;
        Message = message ?? "All dependencies initialized.";
    }

    public void MarkError(string message)
    {
        Status = "Error";
        DependenciesReady = false;
        Message = message;
    }
}
