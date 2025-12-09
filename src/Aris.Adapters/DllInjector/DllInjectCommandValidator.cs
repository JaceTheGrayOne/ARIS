using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Validates DllInjectCommand inputs before execution.
/// </summary>
public static class DllInjectCommandValidator
{
    /// <summary>
    /// Validates and resolves the target process for DLL injection.
    /// </summary>
    /// <param name="command">Injection command to validate.</param>
    /// <param name="options">DLL injector configuration options.</param>
    /// <param name="workspaceRoot">Workspace root path for payload validation.</param>
    /// <param name="processResolver">Process resolver for target validation.</param>
    /// <returns>Validated process ID.</returns>
    /// <exception cref="ValidationError">When validation fails.</exception>
    public static int ValidateAndResolveTarget(
        DllInjectCommand command,
        DllInjectorOptions options,
        string? workspaceRoot,
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

        ValidatePayloadPath(command.DllPath, workspaceRoot);
        ValidateMethod(command.Method, options);
        ValidateArguments(command.Arguments);

        if (command.TimeoutSeconds.HasValue && command.TimeoutSeconds.Value <= 0)
        {
            throw new ValidationError("TimeoutSeconds must be greater than zero for DLL injection operations.")
            {
                RemediationHint = "Remove the timeout override or specify a positive value."
            };
        }

        return pid;
    }

    private static void ValidatePayloadPath(string dllPath, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            throw new ValidationError("DllPath is required for injection.")
            {
                RemediationHint = "Specify the absolute path to the payload DLL."
            };
        }

        if (!Path.IsPathRooted(dllPath))
        {
            throw new ValidationError($"DllPath must be an absolute path: {dllPath}")
            {
                RemediationHint = "Provide a fully-qualified path to the payload DLL."
            };
        }

        var normalizedPath = Path.GetFullPath(dllPath);

        if (!File.Exists(normalizedPath))
        {
            throw new ValidationError($"Payload DLL not found: {normalizedPath}")
            {
                RemediationHint = "Ensure the DLL file exists at the specified path."
            };
        }

        if (!normalizedPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationError($"Payload must be a .dll file: {normalizedPath}")
            {
                RemediationHint = "Use a valid DLL file for injection."
            };
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var normalizedWorkspace = Path.GetFullPath(workspaceRoot);
            var inputPayloadsPath = Path.Combine(normalizedWorkspace, "input", "payloads");
            var dependenciesPayloadsPath = Path.Combine(normalizedWorkspace, "dependencies", "payloads");

            var isInInputPayloads = normalizedPath.StartsWith(inputPayloadsPath, StringComparison.OrdinalIgnoreCase);
            var isInDependenciesPayloads = normalizedPath.StartsWith(dependenciesPayloadsPath, StringComparison.OrdinalIgnoreCase);

            if (!isInInputPayloads && !isInDependenciesPayloads)
            {
                throw new ValidationError($"Payload DLL must be under workspace payloads directory: {normalizedPath}")
                {
                    RemediationHint = $"Place payload DLLs in '{inputPayloadsPath}' or '{dependenciesPayloadsPath}'."
                };
            }
        }
    }

    private static void ValidateMethod(DllInjectionMethod method, DllInjectorOptions options)
    {
        var methodName = method.ToString();

        if (!options.AllowedMethods.Contains(methodName, StringComparer.Ordinal))
        {
            throw new ValidationError($"Injection method '{methodName}' is not allowed by configuration.")
            {
                RemediationHint = "Enable this method in DllInjector:AllowedMethods or choose an allowed method."
            };
        }
    }

    private static void ValidateArguments(IReadOnlyList<string> arguments)
    {
        if (arguments == null || arguments.Count == 0)
        {
            return;
        }

        foreach (var arg in arguments)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                throw new ValidationError("Arguments must not contain empty or whitespace-only entries.")
                {
                    RemediationHint = "Remove the empty argument entry or provide a meaningful value."
                };
            }
        }
    }
}
