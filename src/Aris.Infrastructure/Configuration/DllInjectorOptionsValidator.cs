using Aris.Core.DllInjector;
using Microsoft.Extensions.Options;

namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Validates DllInjectorOptions at startup to ensure safe and sensible configuration.
/// </summary>
public class DllInjectorOptionsValidator : IValidateOptions<DllInjectorOptions>
{
    private const int MaxReasonableLogBytes = 100 * 1024 * 1024; // 100 MB
    private static readonly string[] ValidMethodNames = Enum.GetNames(typeof(DllInjectionMethod));
    private static readonly string[] RequiredDeniedTargets = { "csrss.exe", "smss.exe", "lsass.exe", "services.exe" };

    public ValidateOptionsResult Validate(string? name, DllInjectorOptions options)
    {
        var errors = new List<string>();

        if (options.DefaultTimeoutSeconds <= 0)
        {
            errors.Add($"DefaultTimeoutSeconds must be greater than 0, got {options.DefaultTimeoutSeconds}");
        }

        if (options.MaxLogBytes <= 0)
        {
            errors.Add($"MaxLogBytes must be greater than 0, got {options.MaxLogBytes}");
        }
        else if (options.MaxLogBytes > MaxReasonableLogBytes)
        {
            errors.Add($"MaxLogBytes must not exceed {MaxReasonableLogBytes} bytes, got {options.MaxLogBytes}");
        }

        if (options.AllowedMethods == null || options.AllowedMethods.Length == 0)
        {
            errors.Add("AllowedMethods must contain at least one method");
        }
        else
        {
            foreach (var method in options.AllowedMethods)
            {
                if (string.IsNullOrWhiteSpace(method))
                {
                    errors.Add("AllowedMethods contains empty or whitespace-only entry");
                    continue;
                }

                if (!ValidMethodNames.Contains(method))
                {
                    errors.Add($"AllowedMethods contains invalid method '{method}'. Valid methods: {string.Join(", ", ValidMethodNames)}");
                }
            }
        }

        if (options.DeniedTargets == null || options.DeniedTargets.Length == 0)
        {
            errors.Add("DeniedTargets must not be empty (must include critical system processes)");
        }
        else
        {
            foreach (var required in RequiredDeniedTargets)
            {
                if (!options.DeniedTargets.Contains(required, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"DeniedTargets must include critical process '{required}'");
                }
            }
        }

        if (options.AllowedTargets != null)
        {
            foreach (var target in options.AllowedTargets)
            {
                if (string.IsNullOrWhiteSpace(target))
                {
                    errors.Add("AllowedTargets contains empty or whitespace-only entry");
                }
            }
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return ValidateOptionsResult.Fail($"DllInjectorOptions validation failed: {errorMessage}");
        }

        return ValidateOptionsResult.Success;
    }
}
