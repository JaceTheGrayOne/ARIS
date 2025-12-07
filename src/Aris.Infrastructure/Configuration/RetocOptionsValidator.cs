using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aris.Infrastructure.Configuration;

/// <summary>
/// Validates RetocOptions at startup to ensure safe and sensible configuration.
/// </summary>
public class RetocOptionsValidator : IValidateOptions<RetocOptions>
{
    private const int MaxReasonableLogBytes = 100 * 1024 * 1024; // 100 MB
    private static readonly char[] UnsafePatterns = { ';', '`', '\n', '\r' };
    private static readonly string[] UnsafeSequences = { "&&", "||", "|", ">", "<", "$(" };

    public ValidateOptionsResult Validate(string? name, RetocOptions options)
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

        foreach (var arg in options.AllowedAdditionalArgs)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                errors.Add("AllowedAdditionalArgs contains empty or whitespace-only entry");
                continue;
            }

            if (arg.IndexOfAny(UnsafePatterns) >= 0)
            {
                errors.Add($"AllowedAdditionalArgs contains unsafe characters in '{arg}' (semicolons, backticks, newlines not allowed)");
            }

            if (UnsafeSequences.Any(seq => arg.Contains(seq)))
            {
                errors.Add($"AllowedAdditionalArgs contains unsafe sequence in '{arg}' (&&, ||, pipes, redirects not allowed)");
            }

            var quoteCount = arg.Count(c => c == '"');
            if (quoteCount % 2 != 0)
            {
                errors.Add($"AllowedAdditionalArgs contains unbalanced quotes in '{arg}'");
            }
        }

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            return ValidateOptionsResult.Fail($"RetocOptions validation failed: {errorMessage}");
        }

        return ValidateOptionsResult.Success;
    }
}
