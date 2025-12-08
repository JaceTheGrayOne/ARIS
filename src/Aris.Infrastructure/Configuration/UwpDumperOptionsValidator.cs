using Microsoft.Extensions.Options;

namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Validates UwpDumperOptions at startup to ensure safe and sensible configuration.
/// </summary>
public class UwpDumperOptionsValidator : IValidateOptions<UwpDumperOptions>
{
    private const int MaxReasonableLogBytes = 100 * 1024 * 1024; // 100 MB
    private static readonly string[] ValidModeNames = { "FullDump", "MetadataOnly", "ValidateOnly" };

    public ValidateOptionsResult Validate(string? name, UwpDumperOptions options)
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

        if (options.AllowedModes == null || options.AllowedModes.Length == 0)
        {
            errors.Add("AllowedModes must contain at least one mode");
        }
        else
        {
            foreach (var mode in options.AllowedModes)
            {
                if (string.IsNullOrWhiteSpace(mode))
                {
                    errors.Add("AllowedModes contains empty or whitespace-only entry");
                    continue;
                }

                if (!ValidModeNames.Contains(mode))
                {
                    errors.Add($"AllowedModes contains invalid mode '{mode}'. Valid modes: {string.Join(", ", ValidModeNames)}");
                }
            }
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return ValidateOptionsResult.Fail($"UwpDumperOptions validation failed: {errorMessage}");
        }

        return ValidateOptionsResult.Success;
    }
}
