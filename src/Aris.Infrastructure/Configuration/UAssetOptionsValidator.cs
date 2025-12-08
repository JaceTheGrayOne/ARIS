using Microsoft.Extensions.Options;

namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Validates UAssetOptions at startup to ensure safe and sensible configuration.
/// </summary>
public class UAssetOptionsValidator : IValidateOptions<UAssetOptions>
{
    private const long MaxReasonableAssetSizeBytes = 10L * 1024 * 1024 * 1024; // 10 GB
    private const int MaxReasonableLogBytes = 100 * 1024 * 1024; // 100 MB

    public ValidateOptionsResult Validate(string? name, UAssetOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DefaultUEVersion))
        {
            errors.Add("DefaultUEVersion must not be empty");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultSchemaVersion))
        {
            errors.Add("DefaultSchemaVersion must not be empty");
        }

        if (options.MaxAssetSizeBytes <= 0)
        {
            errors.Add($"MaxAssetSizeBytes must be greater than 0, got {options.MaxAssetSizeBytes}");
        }
        else if (options.MaxAssetSizeBytes > MaxReasonableAssetSizeBytes)
        {
            errors.Add($"MaxAssetSizeBytes must not exceed {MaxReasonableAssetSizeBytes} bytes, got {options.MaxAssetSizeBytes}");
        }

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

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return ValidateOptionsResult.Fail($"UAssetOptions validation failed: {errorMessage}");
        }

        return ValidateOptionsResult.Success;
    }
}
