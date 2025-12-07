using System.Text;
using Aris.Core.Errors;
using Aris.Core.Retoc;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Pure command builder for Retoc CLI invocations.
/// No IO, no logging - just validation and argument construction.
/// </summary>
public static class RetocCommandBuilder
{
    /// <summary>
    /// Builds the command-line arguments for a Retoc operation.
    /// </summary>
    /// <param name="command">The Retoc command to build.</param>
    /// <param name="options">Retoc configuration options.</param>
    /// <param name="retocExePath">Full path to retoc.exe.</param>
    /// <returns>Tuple of (executablePath, arguments).</returns>
    /// <exception cref="ValidationError">Thrown if command validation fails.</exception>
    public static (string ExecutablePath, string Arguments) Build(
        RetocCommand command,
        RetocOptions options,
        string retocExePath)
    {
        ValidateCommand(command);
        ValidateAdditionalArgs(command.AdditionalArgs, options.AllowedAdditionalArgs);

        var args = new StringBuilder();

        // Mode-specific subcommand/flags
        AppendModeArgs(args, command.Mode);

        // Input/output paths
        args.Append($"--input \"{command.InputPath}\" ");
        args.Append($"--output \"{command.OutputPath}\" ");

        // Game/UE version
        if (!string.IsNullOrEmpty(command.GameVersion))
        {
            args.Append($"--game-version \"{command.GameVersion}\" ");
        }

        if (!string.IsNullOrEmpty(command.UEVersion))
        {
            args.Append($"--ue-version \"{command.UEVersion}\" ");
        }

        // Compression options
        var compressionFormat = command.CompressionFormat ?? options.DefaultCompressionFormat;
        var compressionLevel = command.CompressionLevel ?? options.DefaultCompressionLevel;

        args.Append($"--compression \"{compressionFormat}\" ");
        args.Append($"--compression-level {compressionLevel} ");

        if (command.CompressionBlockSize.HasValue)
        {
            args.Append($"--block-size {command.CompressionBlockSize.Value} ");
        }

        // Mount keys
        // TODO: Implement key injection via KeyStore and redact in logs
        foreach (var keyRef in command.MountKeys)
        {
            args.Append($"--aes-key \"{keyRef}\" ");
        }

        // Filters
        foreach (var include in command.IncludeFilters)
        {
            ValidateFilterPattern(include);
            args.Append($"--include \"{include}\" ");
        }

        foreach (var exclude in command.ExcludeFilters)
        {
            ValidateFilterPattern(exclude);
            args.Append($"--exclude \"{exclude}\" ");
        }

        // Additional args (already validated)
        foreach (var arg in command.AdditionalArgs)
        {
            args.Append($"{arg} ");
        }

        return (retocExePath, args.ToString().TrimEnd());
    }

    private static void ValidateCommand(RetocCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.InputPath))
        {
            throw new ValidationError("InputPath is required", nameof(command.InputPath));
        }

        if (string.IsNullOrWhiteSpace(command.OutputPath))
        {
            throw new ValidationError("OutputPath is required", nameof(command.OutputPath));
        }

        if (!Path.IsPathFullyQualified(command.InputPath))
        {
            throw new ValidationError(
                "InputPath must be an absolute path",
                nameof(command.InputPath),
                command.InputPath);
        }

        if (!Path.IsPathFullyQualified(command.OutputPath))
        {
            throw new ValidationError(
                "OutputPath must be an absolute path",
                nameof(command.OutputPath),
                command.OutputPath);
        }
    }

    private static void ValidateAdditionalArgs(
        IReadOnlyList<string> additionalArgs,
        List<string> allowedArgs)
    {
        foreach (var arg in additionalArgs)
        {
            if (!allowedArgs.Contains(arg))
            {
                throw new ValidationError(
                    $"Additional argument '{arg}' is not in the allowlist",
                    nameof(additionalArgs),
                    arg)
                {
                    RemediationHint = "Only pre-approved arguments are allowed. Check RetocOptions.AllowedAdditionalArgs configuration."
                };
            }
        }
    }

    private static void ValidateFilterPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ValidationError("Filter pattern cannot be empty");
        }

        // Basic safety check for path traversal
        if (pattern.Contains(".."))
        {
            throw new ValidationError(
                $"Filter pattern contains path traversal sequence: {pattern}",
                "FilterPattern",
                pattern);
        }
    }

    private static void AppendModeArgs(StringBuilder args, RetocMode mode)
    {
        switch (mode)
        {
            case RetocMode.PakToIoStore:
                args.Append("convert --to-iostore ");
                break;
            case RetocMode.IoStoreToPak:
                args.Append("convert --to-pak ");
                break;
            case RetocMode.Repack:
                args.Append("repack ");
                break;
            case RetocMode.Validate:
                args.Append("validate ");
                break;
            default:
                throw new ValidationError($"Unknown RetocMode: {mode}", nameof(mode), mode.ToString());
        }
    }
}
