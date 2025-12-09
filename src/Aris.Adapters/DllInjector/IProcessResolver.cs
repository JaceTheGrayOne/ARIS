using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Resolves and validates target processes for DLL injection/ejection operations.
/// </summary>
public interface IProcessResolver
{
    /// <summary>
    /// Resolves and validates the target process for DLL injection/ejection.
    /// Applies allowlist/denylist and architecture checks.
    /// </summary>
    /// <param name="processId">Target process ID (optional if processName provided).</param>
    /// <param name="processName">Target process name (optional if processId provided).</param>
    /// <param name="options">DLL injector configuration options.</param>
    /// <returns>Validated process ID.</returns>
    /// <exception cref="Core.Errors.ValidationError">When process cannot be resolved or fails validation.</exception>
    int ResolveAndValidateTarget(
        int? processId,
        string? processName,
        DllInjectorOptions options);
}
