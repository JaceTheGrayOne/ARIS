using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Aris.Core.DllInjector;
using Aris.Infrastructure.Interop;
using Microsoft.Extensions.Logging;

namespace Aris.Infrastructure.DllInjection;

/// <summary>
/// Native C# implementation of DLL injection using CreateRemoteThread + LoadLibraryW.
/// This class performs DLL injection directly using Windows API calls without relying on external tools.
/// </summary>
public class NativeDllInjectionService : IDllInjectionService
{
    private readonly ILogger<NativeDllInjectionService> _logger;

    public NativeDllInjectionService(ILogger<NativeDllInjectionService> logger)
    {
        _logger = logger;
    }

    public async Task<DllInjectionResult> InjectAsync(
        int processId,
        string dllPath,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new InvalidOperationException("DLL injection is only supported on Windows");
        }

        if (!Path.IsPathFullyQualified(dllPath))
        {
            throw new ArgumentException("DLL path must be absolute", nameof(dllPath));
        }

        if (!File.Exists(dllPath))
        {
            throw new ArgumentException($"DLL file not found: {dllPath}", nameof(dllPath));
        }

        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting native DLL injection: processId={ProcessId}, dll={DllPath}",
            processId,
            Path.GetFileName(dllPath));

        // Perform injection on a background thread to avoid blocking
        return await Task.Run(() => InjectInternal(processId, dllPath, cancellationToken), cancellationToken);
    }

    private DllInjectionResult InjectInternal(int processId, string dllPath, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        IntPtr processHandle = IntPtr.Zero;
        IntPtr allocatedMemory = IntPtr.Zero;
        IntPtr remoteThread = IntPtr.Zero;

        try
        {
            // Step 1: Open the target process
            processHandle = Win32Interop.OpenProcess(
                Win32Interop.ProcessAccessFlags.CreateThread |
                Win32Interop.ProcessAccessFlags.VirtualMemoryOperation |
                Win32Interop.ProcessAccessFlags.VirtualMemoryWrite |
                Win32Interop.ProcessAccessFlags.QueryInformation,
                false,
                processId);

            if (processHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("OpenProcess failed with error code {ErrorCode}", error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to open process {processId}. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = error == Win32Interop.ERROR_ACCESS_DENIED
                };
            }

            _logger.LogDebug("Process {ProcessId} opened successfully", processId);

            cancellationToken.ThrowIfCancellationRequested();

            // Step 2: Get the address of LoadLibraryW in kernel32.dll
            // LoadLibraryW is used instead of LoadLibraryA to support Unicode paths
            IntPtr kernel32Module = Win32Interop.GetModuleHandle("kernel32.dll");
            if (kernel32Module == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("GetModuleHandle(kernel32.dll) failed with error code {ErrorCode}", error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to get kernel32.dll module handle. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = false
                };
            }

            IntPtr loadLibraryAddr = Win32Interop.GetProcAddress(kernel32Module, "LoadLibraryW");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("GetProcAddress(LoadLibraryW) failed with error code {ErrorCode}", error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to get LoadLibraryW address. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = false
                };
            }

            _logger.LogDebug("LoadLibraryW address: 0x{Address:X}", loadLibraryAddr.ToInt64());

            cancellationToken.ThrowIfCancellationRequested();

            // Step 3: Allocate memory in the target process for the DLL path
            byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath + '\0'); // Unicode string with null terminator
            uint dllPathSize = (uint)dllPathBytes.Length;

            allocatedMemory = Win32Interop.VirtualAllocEx(
                processHandle,
                IntPtr.Zero,
                dllPathSize,
                Win32Interop.AllocationType.Commit | Win32Interop.AllocationType.Reserve,
                Win32Interop.MemoryProtection.ReadWrite);

            if (allocatedMemory == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("VirtualAllocEx failed with error code {ErrorCode}", error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to allocate memory in target process. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = error == Win32Interop.ERROR_ACCESS_DENIED
                };
            }

            _logger.LogDebug(
                "Allocated {Size} bytes at address 0x{Address:X} in process {ProcessId}",
                dllPathSize,
                allocatedMemory.ToInt64(),
                processId);

            cancellationToken.ThrowIfCancellationRequested();

            // Step 4: Write the DLL path to the allocated memory
            bool writeSuccess = Win32Interop.WriteProcessMemory(
                processHandle,
                allocatedMemory,
                dllPathBytes,
                dllPathSize,
                out UIntPtr bytesWritten);

            if (!writeSuccess || bytesWritten.ToUInt32() != dllPathSize)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError(
                    "WriteProcessMemory failed. Success={Success}, BytesWritten={BytesWritten}/{Expected}, ErrorCode={ErrorCode}",
                    writeSuccess,
                    bytesWritten,
                    dllPathSize,
                    error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to write DLL path to target process memory. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = false
                };
            }

            _logger.LogDebug("Wrote {BytesWritten} bytes to target process memory", bytesWritten);

            cancellationToken.ThrowIfCancellationRequested();

            // Step 5: Create a remote thread in the target process to call LoadLibraryW
            remoteThread = Win32Interop.CreateRemoteThread(
                processHandle,
                IntPtr.Zero,
                0,
                loadLibraryAddr, // Thread start address = LoadLibraryW
                allocatedMemory, // Thread parameter = DLL path
                0,
                out IntPtr threadId);

            if (remoteThread == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("CreateRemoteThread failed with error code {ErrorCode}", error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to create remote thread in target process. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = error == Win32Interop.ERROR_ACCESS_DENIED
                };
            }

            _logger.LogDebug("Created remote thread with ID {ThreadId}", threadId.ToInt64());

            // Step 6: Wait for the remote thread to complete
            uint waitResult = Win32Interop.WaitForSingleObject(remoteThread, 30000); // 30 second timeout

            if (waitResult == Win32Interop.WAIT_TIMEOUT)
            {
                _logger.LogError("Remote thread timed out after 30 seconds");

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = "Remote thread timed out (30 seconds). The DLL may have hung during initialization.",
                    Win32ErrorCode = null,
                    RequiresElevation = false
                };
            }

            if (waitResult != Win32Interop.WAIT_OBJECT_0)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("WaitForSingleObject failed with result {WaitResult}, error code {ErrorCode}", waitResult, error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to wait for remote thread completion. Wait result: {waitResult}, Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = false
                };
            }

            // Step 7: Get the exit code of the thread (this is the module base address returned by LoadLibraryW)
            bool exitCodeSuccess = Win32Interop.GetExitCodeThread(remoteThread, out uint exitCode);

            if (!exitCodeSuccess)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("GetExitCodeThread failed with error code {ErrorCode}", error);

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = $"Failed to get thread exit code. Error code: {error}",
                    Win32ErrorCode = error,
                    RequiresElevation = false
                };
            }

            // If exit code is 0, LoadLibraryW failed
            if (exitCode == 0)
            {
                _logger.LogError("LoadLibraryW returned NULL (DLL failed to load in target process)");

                return new DllInjectionResult
                {
                    Success = false,
                    ProcessId = processId,
                    DllPath = dllPath,
                    LoadedModuleAddress = IntPtr.Zero,
                    Duration = DateTime.UtcNow - startTime,
                    ErrorMessage = "LoadLibraryW returned NULL. The DLL failed to load in the target process. " +
                                  "Possible causes: DLL architecture mismatch (x86 vs x64), missing dependencies, or DllMain returned FALSE.",
                    Win32ErrorCode = null,
                    RequiresElevation = false
                };
            }

            var moduleAddress = new IntPtr(exitCode);

            _logger.LogInformation(
                "DLL injection successful. Process={ProcessId}, DLL={DllPath}, ModuleBase=0x{ModuleBase:X}",
                processId,
                Path.GetFileName(dllPath),
                exitCode);

            return new DllInjectionResult
            {
                Success = true,
                ProcessId = processId,
                DllPath = dllPath,
                LoadedModuleAddress = moduleAddress,
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = null,
                Win32ErrorCode = null,
                RequiresElevation = false
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("DLL injection was cancelled for process {ProcessId}", processId);

            return new DllInjectionResult
            {
                Success = false,
                ProcessId = processId,
                DllPath = dllPath,
                LoadedModuleAddress = IntPtr.Zero,
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = "Operation was cancelled",
                Win32ErrorCode = null,
                RequiresElevation = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during DLL injection for process {ProcessId}", processId);

            return new DllInjectionResult
            {
                Success = false,
                ProcessId = processId,
                DllPath = dllPath,
                LoadedModuleAddress = IntPtr.Zero,
                Duration = DateTime.UtcNow - startTime,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Win32ErrorCode = null,
                RequiresElevation = false
            };
        }
        finally
        {
            // Clean up resources
            if (allocatedMemory != IntPtr.Zero && processHandle != IntPtr.Zero)
            {
                // Free the allocated memory in the target process
                Win32Interop.VirtualFreeEx(
                    processHandle,
                    allocatedMemory,
                    0,
                    Win32Interop.FreeType.Release);

                _logger.LogDebug("Freed allocated memory at address 0x{Address:X}", allocatedMemory.ToInt64());
            }

            if (remoteThread != IntPtr.Zero)
            {
                Win32Interop.CloseHandle(remoteThread);
                _logger.LogDebug("Closed remote thread handle");
            }

            if (processHandle != IntPtr.Zero)
            {
                Win32Interop.CloseHandle(processHandle);
                _logger.LogDebug("Closed process handle");
            }
        }
    }
}
