using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Validates DllEjectCommand inputs before execution.
/// </summary>
public static class DllEjectCommandValidator
{
    /// <summary>
    /// Validates and resolves the target process for DLL ejection.
    /// </summary>
    /// <param name="command">Ejection command to validate.</param>
    /// <param name="options">DLL injector configuration options.</param>
    /// <param name="processResolver">Process resolver for target validation.</param>
    /// <returns>Validated process ID.</returns>
    /// <exception cref="ValidationError">When validation fails.</exception>
    public static int ValidateAndResolveTarget(
        DllEjectCommand command,
        DllInjectorOptions options,
        IProcessResolver processResolver)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (processResolver is null)
        {
            throw new ArgumentNullException(nameof(processResolver));
        }

        var pid = processResolver.ResolveAndValidateTarget(
            command.ProcessId,
            command.ProcessName,
            options);

        if (string.IsNullOrWhiteSpace(command.ModuleName))
        {
            throw new ValidationError("ModuleName is required for DLL ejection.")
            {
                RemediationHint = "Specify the module name to eject, for example 'aris_payload.dll'."
            };
        }

        if (command.TimeoutSeconds.HasValue && command.TimeoutSeconds.Value <= 0)
        {
            throw new ValidationError("TimeoutSeconds must be greater than zero for DLL ejection operations.")
            {
                RemediationHint = "Remove the timeout override or specify a positive value."
            };
        }

        return pid;
    }
}
