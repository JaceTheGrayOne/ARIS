using Aris.Core.Errors;
using Aris.Core.UwpDumper;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.UwpDumper;

/// <summary>
/// Validates UwpDumpCommand inputs before execution.
/// </summary>
public static class UwpDumpCommandValidator
{
    public static void ValidateDumpCommand(UwpDumpCommand command, UwpDumperOptions options)
    {
        if (string.IsNullOrWhiteSpace(command.PackageFamilyName))
        {
            throw new ValidationError("PackageFamilyName is required", nameof(command.PackageFamilyName));
        }

        if (string.IsNullOrWhiteSpace(command.OutputPath))
        {
            throw new ValidationError("OutputPath is required", nameof(command.OutputPath));
        }

        if (!Path.IsPathFullyQualified(command.OutputPath))
        {
            throw new ValidationError($"OutputPath must be absolute: {command.OutputPath}", nameof(command.OutputPath));
        }

        var modeName = command.Mode.ToString();
        if (!options.AllowedModes.Contains(modeName))
        {
            throw new ValidationError(
                $"Mode '{modeName}' is not allowed. Allowed modes: {string.Join(", ", options.AllowedModes)}",
                nameof(command.Mode))
            {
                RemediationHint = "Update UwpDumperOptions.AllowedModes to include this mode, or use a different mode"
            };
        }

        if (command.TimeoutSeconds.HasValue && command.TimeoutSeconds.Value <= 0)
        {
            throw new ValidationError($"TimeoutSeconds must be greater than 0, got {command.TimeoutSeconds.Value}", nameof(command.TimeoutSeconds));
        }
    }
}
