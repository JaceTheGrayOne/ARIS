using System.Text;
using Aris.Core.Errors;
using Aris.Core.Retoc;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Pure command builder for Retoc CLI invocations.
/// No IO, no logging - just validation and argument construction.
/// Builds commands for the real retoc CLI from https://github.com/trumank/retoc
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

        var args = new List<string>();

        // Add global options first
        AppendGlobalOptions(args, command);

        // Add subcommand
        var subcommand = GetSubcommand(command);
        args.Add(subcommand);

        // Add subcommand-specific arguments
        AppendSubcommandArgs(args, command, subcommand);

        // Additional args (already validated)
        args.AddRange(command.AdditionalArgs);

        return (retocExePath, string.Join(" ", args));
    }

    /// <summary>
    /// Builds the command-line arguments for a Retoc operation.
    /// Returns both the argument list and the joined string.
    /// </summary>
    /// <param name="command">The Retoc command to build.</param>
    /// <param name="options">Retoc configuration options.</param>
    /// <param name="retocExePath">Full path to retoc.exe.</param>
    /// <returns>Tuple of (executablePath, argumentList, argumentsString).</returns>
    /// <exception cref="ValidationError">Thrown if command validation fails.</exception>
    public static (string ExecutablePath, List<string> ArgumentList, string ArgumentsString) BuildWithList(
        RetocCommand command,
        RetocOptions options,
        string retocExePath)
    {
        ValidateCommand(command);
        ValidateAdditionalArgs(command.AdditionalArgs, options.AllowedAdditionalArgs);

        var args = new List<string>();

        // Add global options first
        AppendGlobalOptions(args, command);

        // Add subcommand
        var subcommand = GetSubcommand(command);
        args.Add(subcommand);

        // Add subcommand-specific arguments
        AppendSubcommandArgs(args, command, subcommand);

        // Additional args (already validated)
        args.AddRange(command.AdditionalArgs);

        return (retocExePath, args, string.Join(" ", args));
    }

    private static void ValidateCommand(RetocCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.InputPath))
        {
            throw new ValidationError("InputPath is required", nameof(command.InputPath));
        }

        if (!Path.IsPathFullyQualified(command.InputPath))
        {
            throw new ValidationError(
                "InputPath must be an absolute path",
                nameof(command.InputPath),
                command.InputPath);
        }

        // OutputPath validation depends on command type
        // Some commands (get, info, list, verify, etc.) have optional or no output
        var commandsWithOptionalOutput = new[]
        {
            RetocCommandType.Get,
            RetocCommandType.Info,
            RetocCommandType.List,
            RetocCommandType.Verify,
            RetocCommandType.PrintScriptObjects,
            RetocCommandType.DumpTest
        };

        var requiresOutput = !commandsWithOptionalOutput.Contains(command.CommandType);

        if (requiresOutput)
        {
            if (string.IsNullOrWhiteSpace(command.OutputPath))
            {
                throw new ValidationError("OutputPath is required", nameof(command.OutputPath));
            }

            if (!Path.IsPathFullyQualified(command.OutputPath))
            {
                throw new ValidationError(
                    "OutputPath must be an absolute path",
                    nameof(command.OutputPath),
                    command.OutputPath);
            }
        }
        else if (!string.IsNullOrWhiteSpace(command.OutputPath) && command.OutputPath != "-")
        {
            // If output is provided for optional-output commands, validate it
            if (!Path.IsPathFullyQualified(command.OutputPath))
            {
                throw new ValidationError(
                    "OutputPath must be an absolute path",
                    nameof(command.OutputPath),
                    command.OutputPath);
            }
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

    /// <summary>
    /// Appends global options (--aes-key, --override-container-header-version, --override-toc-version).
    /// These must come before the subcommand.
    /// </summary>
    private static void AppendGlobalOptions(List<string> args, RetocCommand command)
    {
        // AES key (if provided)
        var aesKey = command.AesKey ?? (command.MountKeys.Count > 0 ? command.MountKeys[0] : null);
        if (!string.IsNullOrEmpty(aesKey))
        {
            args.Add("--aes-key");
            args.Add(aesKey);
        }

        // Container header version override
        if (command.ContainerHeaderVersion.HasValue)
        {
            args.Add("--override-container-header-version");
            args.Add(command.ContainerHeaderVersion.Value.ToString());
        }

        // TOC version override
        if (command.TocVersion.HasValue)
        {
            args.Add("--override-toc-version");
            args.Add(command.TocVersion.Value.ToString());
        }
    }

    /// <summary>
    /// Determines the Retoc CLI subcommand from the command type or mode.
    /// </summary>
    private static string GetSubcommand(RetocCommand command)
    {
        // If CommandType is explicitly set, use it
        if (command.CommandType != default(RetocCommandType))
        {
            return CommandTypeToString(command.CommandType);
        }

        // Otherwise, infer from Mode (backward compatibility)
        if (command.Mode.HasValue)
        {
            return command.Mode.Value switch
            {
                RetocMode.PakToIoStore => "to-zen",
                RetocMode.IoStoreToPak => "to-legacy",
                RetocMode.Validate => "verify",
                RetocMode.Repack => throw new ValidationError(
                    "RetocMode.Repack is not directly supported by retoc CLI. Use explicit CommandType instead.",
                    nameof(command.Mode),
                    command.Mode.Value.ToString()),
                _ => throw new ValidationError($"Unknown RetocMode: {command.Mode.Value}", nameof(command.Mode), command.Mode.Value.ToString())
            };
        }

        throw new ValidationError("Either CommandType or Mode must be specified", nameof(command.CommandType));
    }

    /// <summary>
    /// Converts RetocCommandType enum to the actual CLI subcommand string.
    /// </summary>
    private static string CommandTypeToString(RetocCommandType commandType)
    {
        return commandType switch
        {
            RetocCommandType.Manifest => "manifest",
            RetocCommandType.Info => "info",
            RetocCommandType.List => "list",
            RetocCommandType.Verify => "verify",
            RetocCommandType.Unpack => "unpack",
            RetocCommandType.UnpackRaw => "unpack-raw",
            RetocCommandType.PackRaw => "pack-raw",
            RetocCommandType.ToLegacy => "to-legacy",
            RetocCommandType.ToZen => "to-zen",
            RetocCommandType.Get => "get",
            RetocCommandType.DumpTest => "dump-test",
            RetocCommandType.GenScriptObjects => "gen-script-objects",
            RetocCommandType.PrintScriptObjects => "print-script-objects",
            _ => throw new ValidationError($"Unknown RetocCommandType: {commandType}", nameof(commandType), commandType.ToString())
        };
    }

    /// <summary>
    /// Appends subcommand-specific arguments (input/output paths, etc.).
    /// </summary>
    private static void AppendSubcommandArgs(List<string> args, RetocCommand command, string subcommand)
    {
        // Most commands take input as a positional argument
        switch (subcommand)
        {
            case "to-zen":
                // Format: to-zen --version <version> <input> <output>
                if (!string.IsNullOrWhiteSpace(command.Version))
                {
                    args.Add("--version");
                    args.Add(command.Version);
                }
                args.Add(QuoteIfNeeded(command.InputPath));
                args.Add(QuoteIfNeeded(command.OutputPath));
                break;

            case "to-legacy":
                // Format: to-legacy <input> <output>
                args.Add(QuoteIfNeeded(command.InputPath));
                args.Add(QuoteIfNeeded(command.OutputPath));
                break;

            case "unpack":
            case "unpack-raw":
                // Format: unpack <input.utoc> <output-dir>
                args.Add(QuoteIfNeeded(command.InputPath));
                args.Add(QuoteIfNeeded(command.OutputPath));
                break;

            case "pack-raw":
                // Format: pack-raw <input-dir> <output-prefix>
                args.Add(QuoteIfNeeded(command.InputPath));
                args.Add(QuoteIfNeeded(command.OutputPath));
                break;

            case "manifest":
            case "info":
            case "list":
            case "verify":
            case "print-script-objects":
                // Format: <command> <input.utoc>
                args.Add(QuoteIfNeeded(command.InputPath));
                break;

            case "gen-script-objects":
                // Format: gen-script-objects <input.jmap> <output-dir>
                args.Add(QuoteIfNeeded(command.InputPath));
                args.Add(QuoteIfNeeded(command.OutputPath));
                break;

            case "get":
                // Format: get <input.utoc> <chunk-id> [output]
                // OUTPUT is optional - if omitted or "-", writes to stdout
                if (string.IsNullOrWhiteSpace(command.ChunkId))
                {
                    throw new ValidationError(
                        "ChunkId is required for Get command",
                        nameof(command.ChunkId));
                }
                args.Add(QuoteIfNeeded(command.InputPath));
                args.Add(command.ChunkId);
                // Add optional output path if provided and not empty/"-"
                if (!string.IsNullOrWhiteSpace(command.OutputPath) && command.OutputPath != "-")
                {
                    args.Add(QuoteIfNeeded(command.OutputPath));
                }
                break;

            case "dump-test":
                // Format: dump-test <input>
                args.Add(QuoteIfNeeded(command.InputPath));
                break;

            default:
                // For unknown commands, just add input and output if they exist
                if (!string.IsNullOrEmpty(command.InputPath))
                {
                    args.Add(QuoteIfNeeded(command.InputPath));
                }
                if (!string.IsNullOrEmpty(command.OutputPath))
                {
                    args.Add(QuoteIfNeeded(command.OutputPath));
                }
                break;
        }
    }

    /// <summary>
    /// Quotes a path if it contains spaces.
    /// </summary>
    private static string QuoteIfNeeded(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return path.Contains(' ') ? $"\"{path}\"" : path;
    }
}
