using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Aris.Infrastructure.Terminal;

/// <summary>
/// Process wrapper using Windows ConPTY (Pseudo Console).
/// Provides proper TTY environment for applications that require terminal support.
/// </summary>
public sealed class ConPtyProcess : IConPtyProcess
{
    private readonly ILogger<ConPtyProcess> _logger;

    // Pipe handles for ConPTY I/O
    private SafeFileHandle? _pipeReadHandle;  // Read output from ConPTY
    private SafeFileHandle? _pipeWriteHandle; // Write input to ConPTY

    // Pseudo-console handle
    private IntPtr _pseudoConsoleHandle;

    // Attribute list for STARTUPINFOEX
    private IntPtr _attributeList;

    // Process information
    private ConPtyNativeMethods.PROCESS_INFORMATION _processInfo;

    private bool _disposed;
    private bool _hasStarted;
    private bool _hasExited;

    public ConPtyProcess(ILogger<ConPtyProcess> logger)
    {
        _logger = logger;
    }

    public int ProcessId => _hasStarted ? _processInfo.dwProcessId : -1;

    public bool HasStarted => _hasStarted;

    public bool HasExited => _hasExited;

    public async Task StartAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        short terminalWidth = 120,
        short terminalHeight = 30)
    {
        if (_hasStarted)
            throw new InvalidOperationException("Process has already been started.");

        _logger.LogDebug(
            "Starting ConPTY process: Executable={Executable}, WorkingDir={WorkingDir}, Size={Width}x{Height}",
            executable, workingDirectory, terminalWidth, terminalHeight);

        try
        {
            // Step 1: Create pipes for ConPTY I/O
            // We need two pairs: one for ConPTY to read from (our write), one for ConPTY to write to (our read)
            if (!ConPtyNativeMethods.CreatePipe(out var pipeReadForConPty, out _pipeWriteHandle, IntPtr.Zero, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create input pipe");
            }

            if (!ConPtyNativeMethods.CreatePipe(out _pipeReadHandle, out var pipeWriteForConPty, IntPtr.Zero, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create output pipe");
            }

            // Step 2: Create pseudo-console
            var consoleSize = new ConPtyNativeMethods.COORD(terminalWidth, terminalHeight);
            int hr = ConPtyNativeMethods.CreatePseudoConsole(
                consoleSize,
                pipeReadForConPty,
                pipeWriteForConPty,
                0,
                out _pseudoConsoleHandle);

            if (hr != 0)
            {
                throw new Win32Exception(hr, "CreatePseudoConsole failed");
            }

            // Step 3: Close pipe ends that ConPTY now owns
            // ConPTY has duplicated these handles internally
            pipeReadForConPty.Dispose();
            pipeWriteForConPty.Dispose();

            // Step 4: Initialize attribute list
            // First call to get required size
            IntPtr attributeListSize = IntPtr.Zero;
            ConPtyNativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeListSize);

            // Allocate and initialize
            _attributeList = Marshal.AllocHGlobal(attributeListSize);
            if (!ConPtyNativeMethods.InitializeProcThreadAttributeList(_attributeList, 1, 0, ref attributeListSize))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeProcThreadAttributeList failed");
            }

            // Step 5: Set PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE
            // CRITICAL: This is what makes ConPTY work
            if (!ConPtyNativeMethods.UpdateProcThreadAttribute(
                _attributeList,
                0,
                ConPtyNativeMethods.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                _pseudoConsoleHandle,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateProcThreadAttribute failed");
            }

            // Step 6: Prepare STARTUPINFOEX
            // CRITICAL: Do NOT set STARTF_USESTDHANDLES - ConPTY provides the console
            var startupInfo = new ConPtyNativeMethods.STARTUPINFOEX
            {
                StartupInfo = new ConPtyNativeMethods.STARTUPINFO
                {
                    cb = Marshal.SizeOf<ConPtyNativeMethods.STARTUPINFOEX>()
                    // dwFlags is left at 0 - NO STARTF_USESTDHANDLES
                    // hStdInput, hStdOutput, hStdError are left at IntPtr.Zero
                },
                lpAttributeList = _attributeList
            };

            // Step 7: Build command line
            string commandLine = string.IsNullOrEmpty(arguments)
                ? $"\"{executable}\""
                : $"\"{executable}\" {arguments}";

            // Step 8: Create process with EXTENDED_STARTUPINFO_PRESENT
            // dwCreationFlags: EXTENDED_STARTUPINFO_PRESENT is REQUIRED
            // CREATE_UNICODE_ENVIRONMENT for proper Unicode support
            // Do NOT add CREATE_NO_WINDOW - ConPTY is the console
            uint creationFlags =
                ConPtyNativeMethods.EXTENDED_STARTUPINFO_PRESENT |
                ConPtyNativeMethods.CREATE_UNICODE_ENVIRONMENT;

            if (!ConPtyNativeMethods.CreateProcess(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false, // bInheritHandles = false
                creationFlags,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out _processInfo))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcess failed");
            }

            _hasStarted = true;

            _logger.LogInformation(
                "ConPTY process started: PID={Pid}, CommandLine={CommandLine}",
                _processInfo.dwProcessId,
                commandLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start ConPTY process");
            Cleanup();
            throw;
        }

        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<byte[]> ReadOutputAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_hasStarted)
            throw new InvalidOperationException("Process has not been started.");

        if (_pipeReadHandle == null || _pipeReadHandle.IsInvalid)
            throw new InvalidOperationException("Output pipe is not available.");

        // CreatePipe doesn't create overlapped handles, so we must use synchronous I/O.
        // Wrap synchronous reads in background tasks to avoid blocking the async enumeration.
        using var outputStream = new FileStream(
            _pipeReadHandle,
            FileAccess.Read,
            bufferSize: 4096,
            isAsync: false);  // MUST be false - CreatePipe handles don't support overlapped I/O

        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead;
            try
            {
                // Run synchronous read on thread pool to avoid blocking
                bytesRead = await Task.Run(() =>
                {
                    try
                    {
                        return outputStream.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException) when (IsProcessExited())
                    {
                        return 0; // Treat as EOF when process exits
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Output read cancelled");
                break;
            }
            catch (IOException ex) when (IsProcessExited())
            {
                _logger.LogDebug(ex, "Pipe closed - process exited");
                break;
            }

            if (bytesRead == 0)
            {
                _logger.LogDebug("Pipe closed (EOF)");
                break;
            }

            // Return a copy of the data
            var data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);
            yield return data;
        }
    }

    public async Task WriteInputAsync(byte[] data, CancellationToken cancellationToken)
    {
        if (!_hasStarted)
            throw new InvalidOperationException("Process has not been started.");

        if (_pipeWriteHandle == null || _pipeWriteHandle.IsInvalid)
            throw new InvalidOperationException("Input pipe is not available.");

        // CreatePipe doesn't create overlapped handles, so we must use synchronous I/O.
        using var inputStream = new FileStream(
            _pipeWriteHandle,
            FileAccess.Write,
            bufferSize: 4096,
            isAsync: false);  // MUST be false - CreatePipe handles don't support overlapped I/O

        await Task.Run(() =>
        {
            inputStream.Write(data, 0, data.Length);
            inputStream.Flush();
        }, cancellationToken);
    }

    public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (!_hasStarted)
            throw new InvalidOperationException("Process has not been started.");

        // Wait asynchronously for the process to exit
        await Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                uint result = ConPtyNativeMethods.WaitForSingleObject(
                    _processInfo.hProcess,
                    100); // 100ms timeout for checking cancellation

                if (result == ConPtyNativeMethods.WAIT_OBJECT_0)
                {
                    break; // Process exited
                }
            }
        }, cancellationToken);

        // Get exit code
        if (!ConPtyNativeMethods.GetExitCodeProcess(_processInfo.hProcess, out uint exitCode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetExitCodeProcess failed");
        }

        _hasExited = true;
        _logger.LogDebug("Process exited with code {ExitCode}", exitCode);

        return (int)exitCode;
    }

    public void Kill()
    {
        if (!_hasStarted || _hasExited)
            return;

        _logger.LogWarning("Killing ConPTY process PID={Pid}", _processInfo.dwProcessId);

        if (_processInfo.hProcess != IntPtr.Zero)
        {
            ConPtyNativeMethods.TerminateProcess(_processInfo.hProcess, 1);
            _hasExited = true;
        }
    }

    public void Resize(short width, short height)
    {
        if (_pseudoConsoleHandle == IntPtr.Zero)
            return;

        var newSize = new ConPtyNativeMethods.COORD(width, height);
        int hr = ConPtyNativeMethods.ResizePseudoConsole(_pseudoConsoleHandle, newSize);

        if (hr != 0)
        {
            _logger.LogWarning("ResizePseudoConsole failed with HRESULT {HResult}", hr);
        }
    }

    private bool IsProcessExited()
    {
        if (_processInfo.hProcess == IntPtr.Zero)
            return true;

        if (!ConPtyNativeMethods.GetExitCodeProcess(_processInfo.hProcess, out uint exitCode))
            return true;

        return exitCode != ConPtyNativeMethods.STILL_ACTIVE;
    }

    private void Cleanup()
    {
        // Cleanup order is important!

        // 1. Terminate process if still running
        if (_processInfo.hProcess != IntPtr.Zero && !_hasExited)
        {
            if (ConPtyNativeMethods.GetExitCodeProcess(_processInfo.hProcess, out uint exitCode) &&
                exitCode == ConPtyNativeMethods.STILL_ACTIVE)
            {
                ConPtyNativeMethods.TerminateProcess(_processInfo.hProcess, 1);
            }
        }

        // 2. Close process and thread handles
        if (_processInfo.hProcess != IntPtr.Zero)
        {
            ConPtyNativeMethods.CloseHandle(_processInfo.hProcess);
            _processInfo.hProcess = IntPtr.Zero;
        }

        if (_processInfo.hThread != IntPtr.Zero)
        {
            ConPtyNativeMethods.CloseHandle(_processInfo.hThread);
            _processInfo.hThread = IntPtr.Zero;
        }

        // 3. Delete attribute list
        if (_attributeList != IntPtr.Zero)
        {
            ConPtyNativeMethods.DeleteProcThreadAttributeList(_attributeList);
            Marshal.FreeHGlobal(_attributeList);
            _attributeList = IntPtr.Zero;
        }

        // 4. Close pseudo-console
        if (_pseudoConsoleHandle != IntPtr.Zero)
        {
            ConPtyNativeMethods.ClosePseudoConsole(_pseudoConsoleHandle);
            _pseudoConsoleHandle = IntPtr.Zero;
        }

        // 5. Close pipe handles
        _pipeReadHandle?.Dispose();
        _pipeReadHandle = null;

        _pipeWriteHandle?.Dispose();
        _pipeWriteHandle = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Cleanup();
    }
}
