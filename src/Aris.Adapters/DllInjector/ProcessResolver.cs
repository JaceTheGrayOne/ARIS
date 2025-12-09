using System.Diagnostics;
using System.Runtime.InteropServices;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.DllInjector;

/// <summary>
/// Resolves and validates target processes for DLL injection/ejection operations.
/// </summary>
public class ProcessResolver : IProcessResolver
{
    public int ResolveAndValidateTarget(
        int? processId,
        string? processName,
        DllInjectorOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var hasPid = processId.HasValue;
        var hasName = !string.IsNullOrWhiteSpace(processName);

        if (!hasPid && !hasName)
        {
            throw new ValidationError("Either ProcessId or ProcessName must be provided.")
            {
                RemediationHint = "Specify a target process using ProcessId (e.g., 1234) or ProcessName (e.g., 'Game.exe')."
            };
        }

        if (hasPid && hasName)
        {
            throw new ValidationError("Provide either ProcessId or ProcessName, but not both.")
            {
                RemediationHint = "Use ProcessId for precise targeting or ProcessName for name-based resolution."
            };
        }

        Process targetProcess;

        if (hasPid)
        {
            try
            {
                targetProcess = Process.GetProcessById(processId!.Value);
            }
            catch (ArgumentException)
            {
                throw new ValidationError($"No running process with ID {processId} was found.")
                {
                    RemediationHint = "Verify the process is running and use a valid process ID."
                };
            }
        }
        else
        {
            targetProcess = ResolveByName(processName!);
        }

        using (targetProcess)
        {
            if (targetProcess.HasExited)
            {
                throw new ValidationError($"Process {targetProcess.Id} ({targetProcess.ProcessName}) has already exited.")
                {
                    RemediationHint = "Target a running process."
                };
            }

            if (!IsProcessX64(targetProcess))
            {
                throw new ValidationError($"Process {targetProcess.Id} ({targetProcess.ProcessName}) is not 64-bit.")
                {
                    RemediationHint = "Only 64-bit processes are supported for injection. Ensure the target process is compiled for x64."
                };
            }

            var executableName = GetExecutableName(targetProcess);

            if (IsProcessDenied(executableName, options))
            {
                throw new ValidationError($"Process '{executableName}' is denied by policy.")
                {
                    RemediationHint = "This process is in the DeniedTargets list. Choose a different target or adjust DllInjector:DeniedTargets configuration."
                };
            }

            if (!IsProcessAllowed(executableName, options))
            {
                throw new ValidationError($"Process '{executableName}' is not in the allowed targets list.")
                {
                    RemediationHint = "This process is not in the AllowedTargets list. Add it to DllInjector:AllowedTargets or clear the allowlist to allow all non-denied processes."
                };
            }

            return targetProcess.Id;
        }
    }

    private static Process ResolveByName(string processName)
    {
        var normalizedName = processName.Trim();

        if (normalizedName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = normalizedName.Substring(0, normalizedName.Length - 4);
        }

        var matches = Process.GetProcessesByName(normalizedName);

        if (matches.Length == 0)
        {
            throw new ValidationError($"No running process with name '{processName}' was found.")
            {
                RemediationHint = "Verify the process name is correct and the process is running."
            };
        }

        if (matches.Length > 1)
        {
            foreach (var p in matches.Skip(1))
            {
                p.Dispose();
            }

            matches[0].Dispose();

            throw new ValidationError($"ProcessName '{processName}' resolved to {matches.Length} running processes.")
            {
                RemediationHint = "Multiple processes match this name. Use ProcessId instead for precise targeting."
            };
        }

        return matches[0];
    }

    private static bool IsProcessX64(Process process)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        if (!Environment.Is64BitOperatingSystem)
        {
            return false;
        }

        try
        {
            if (!IsWow64Process(process.Handle, out bool isWow64))
            {
                return true;
            }

            return !isWow64;
        }
        catch
        {
            return false;
        }
    }

    private static string GetExecutableName(Process process)
    {
        try
        {
            var moduleName = process.MainModule?.ModuleName;
            if (!string.IsNullOrEmpty(moduleName))
            {
                return moduleName;
            }
        }
        catch
        {
        }

        return process.ProcessName + ".exe";
    }

    private static bool IsProcessDenied(string executableName, DllInjectorOptions options)
    {
        if (options.DeniedTargets == null || options.DeniedTargets.Length == 0)
        {
            return false;
        }

        foreach (var pattern in options.DeniedTargets)
        {
            if (MatchesPattern(executableName, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProcessAllowed(string executableName, DllInjectorOptions options)
    {
        if (options.AllowedTargets == null || options.AllowedTargets.Length == 0)
        {
            return true;
        }

        foreach (var pattern in options.AllowedTargets)
        {
            if (MatchesPattern(executableName, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPattern(string executableName, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var normalizedExe = executableName.ToLowerInvariant();
        var normalizedPattern = pattern.ToLowerInvariant();

        if (normalizedPattern == "*" || normalizedPattern == "*.*")
        {
            return true;
        }

        if (!normalizedPattern.Contains('*'))
        {
            return normalizedExe.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedPattern.StartsWith("*") && normalizedPattern.EndsWith("*"))
        {
            var middle = normalizedPattern.Substring(1, normalizedPattern.Length - 2);
            return normalizedExe.Contains(middle);
        }

        if (normalizedPattern.StartsWith("*"))
        {
            var suffix = normalizedPattern.Substring(1);
            return normalizedExe.EndsWith(suffix);
        }

        if (normalizedPattern.EndsWith("*"))
        {
            var prefix = normalizedPattern.Substring(0, normalizedPattern.Length - 1);
            return normalizedExe.StartsWith(prefix);
        }

        return false;
    }

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process([In] IntPtr processHandle, [Out] out bool wow64Process);
}
