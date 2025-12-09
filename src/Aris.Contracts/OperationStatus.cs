namespace Aris.Contracts;

/// <summary>
/// Simple operation outcome status.
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// Operation is queued or in progress.
    /// </summary>
    Pending,

    /// <summary>
    /// Operation completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Operation failed.
    /// </summary>
    Failed
}
