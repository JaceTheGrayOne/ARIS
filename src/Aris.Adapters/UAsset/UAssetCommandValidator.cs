using Aris.Core.Errors;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.UAsset;

/// <summary>
/// Validates UAsset command inputs before execution.
/// </summary>
public static class UAssetCommandValidator
{
    public static void ValidateSerializeCommand(UAssetSerializeCommand command, UAssetOptions options)
    {
        if (string.IsNullOrWhiteSpace(command.InputJsonPath))
        {
            throw new ValidationError("InputJsonPath is required", nameof(command.InputJsonPath));
        }

        if (string.IsNullOrWhiteSpace(command.OutputAssetPath))
        {
            throw new ValidationError("OutputAssetPath is required", nameof(command.OutputAssetPath));
        }

        if (!Path.IsPathFullyQualified(command.InputJsonPath))
        {
            throw new ValidationError($"InputJsonPath must be absolute: {command.InputJsonPath}", nameof(command.InputJsonPath));
        }

        if (!Path.IsPathFullyQualified(command.OutputAssetPath))
        {
            throw new ValidationError($"OutputAssetPath must be absolute: {command.OutputAssetPath}", nameof(command.OutputAssetPath));
        }

        var inputPath = Path.GetFullPath(command.InputJsonPath);
        var outputPath = Path.GetFullPath(command.OutputAssetPath);

        if (!File.Exists(inputPath))
        {
            throw new ValidationError($"Input JSON file not found: {inputPath}", nameof(command.InputJsonPath));
        }

        var inputFileInfo = new FileInfo(inputPath);
        if (inputFileInfo.Length > options.MaxAssetSizeBytes)
        {
            throw new ValidationError(
                $"Input JSON file exceeds maximum size ({options.MaxAssetSizeBytes} bytes): {inputFileInfo.Length} bytes",
                nameof(command.InputJsonPath))
            {
                RemediationHint = "Increase MaxAssetSizeBytes in UAssetOptions or reduce the size of the input file"
            };
        }

        var outputDir = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDir))
        {
            throw new ValidationError($"Invalid OutputAssetPath directory: {command.OutputAssetPath}", nameof(command.OutputAssetPath));
        }
    }

    public static void ValidateDeserializeCommand(UAssetDeserializeCommand command, UAssetOptions options)
    {
        if (string.IsNullOrWhiteSpace(command.InputAssetPath))
        {
            throw new ValidationError("InputAssetPath is required", nameof(command.InputAssetPath));
        }

        if (string.IsNullOrWhiteSpace(command.OutputJsonPath))
        {
            throw new ValidationError("OutputJsonPath is required", nameof(command.OutputJsonPath));
        }

        if (!Path.IsPathFullyQualified(command.InputAssetPath))
        {
            throw new ValidationError($"InputAssetPath must be absolute: {command.InputAssetPath}", nameof(command.InputAssetPath));
        }

        if (!Path.IsPathFullyQualified(command.OutputJsonPath))
        {
            throw new ValidationError($"OutputJsonPath must be absolute: {command.OutputJsonPath}", nameof(command.OutputJsonPath));
        }

        var inputPath = Path.GetFullPath(command.InputAssetPath);
        var outputPath = Path.GetFullPath(command.OutputJsonPath);

        if (!File.Exists(inputPath))
        {
            throw new ValidationError($"Input asset file not found: {inputPath}", nameof(command.InputAssetPath));
        }

        var inputFileInfo = new FileInfo(inputPath);
        if (inputFileInfo.Length > options.MaxAssetSizeBytes)
        {
            throw new ValidationError(
                $"Input asset file exceeds maximum size ({options.MaxAssetSizeBytes} bytes): {inputFileInfo.Length} bytes",
                nameof(command.InputAssetPath))
            {
                RemediationHint = "Increase MaxAssetSizeBytes in UAssetOptions or reduce the size of the input file"
            };
        }

        var outputDir = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDir))
        {
            throw new ValidationError($"Invalid OutputJsonPath directory: {command.OutputJsonPath}", nameof(command.OutputJsonPath));
        }
    }

    public static void ValidateInspectCommand(UAssetInspectCommand command, UAssetOptions options)
    {
        if (string.IsNullOrWhiteSpace(command.InputAssetPath))
        {
            throw new ValidationError("InputAssetPath is required", nameof(command.InputAssetPath));
        }

        if (!Path.IsPathFullyQualified(command.InputAssetPath))
        {
            throw new ValidationError($"InputAssetPath must be absolute: {command.InputAssetPath}", nameof(command.InputAssetPath));
        }

        var inputPath = Path.GetFullPath(command.InputAssetPath);

        if (!File.Exists(inputPath))
        {
            throw new ValidationError($"Input asset file not found: {inputPath}", nameof(command.InputAssetPath));
        }

        var inputFileInfo = new FileInfo(inputPath);
        if (inputFileInfo.Length > options.MaxAssetSizeBytes)
        {
            throw new ValidationError(
                $"Input asset file exceeds maximum size ({options.MaxAssetSizeBytes} bytes): {inputFileInfo.Length} bytes",
                nameof(command.InputAssetPath))
            {
                RemediationHint = "Increase MaxAssetSizeBytes in UAssetOptions or reduce the size of the input file"
            };
        }
    }
}
