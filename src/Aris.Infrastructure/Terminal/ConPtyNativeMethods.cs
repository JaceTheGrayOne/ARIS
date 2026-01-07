using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Aris.Infrastructure.Terminal;

/// <summary>
/// P/Invoke declarations for Windows ConPTY (Pseudo Console) API.
/// Used for attaching processes to a pseudo-terminal for proper TTY support.
/// </summary>
internal static class ConPtyNativeMethods
{
    #region CreatePseudoConsole

    /// <summary>
    /// Creates a pseudo console.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int CreatePseudoConsole(
        COORD size,
        SafeFileHandle hInput,
        SafeFileHandle hOutput,
        uint dwFlags,
        out IntPtr phPC);

    /// <summary>
    /// Closes a pseudo console.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern void ClosePseudoConsole(IntPtr hPC);

    /// <summary>
    /// Resizes a pseudo console.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

    #endregion

    #region Process Thread Attributes

    /// <summary>
    /// Initializes the specified list of attributes for process and thread creation.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InitializeProcThreadAttributeList(
        IntPtr lpAttributeList,
        int dwAttributeCount,
        int dwFlags,
        ref IntPtr lpSize);

    /// <summary>
    /// Updates the specified attribute in a list of attributes for process and thread creation.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UpdateProcThreadAttribute(
        IntPtr lpAttributeList,
        uint dwFlags,
        IntPtr Attribute,
        IntPtr lpValue,
        IntPtr cbSize,
        IntPtr lpPreviousValue,
        IntPtr lpReturnSize);

    /// <summary>
    /// Deletes the specified list of attributes for process and thread creation.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    #endregion

    #region Process Creation

    /// <summary>
    /// Creates a new process and its primary thread.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateProcess(
        string? lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    #endregion

    #region Pipe Creation

    /// <summary>
    /// Creates an anonymous pipe.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreatePipe(
        out SafeFileHandle hReadPipe,
        out SafeFileHandle hWritePipe,
        IntPtr lpPipeAttributes,
        uint nSize);

    #endregion

    #region Handle and Process Operations

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Waits until the specified object is in the signaled state or the time-out interval elapses.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    /// <summary>
    /// Retrieves the termination status of the specified process.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    /// <summary>
    /// Terminates the specified process and all of its threads.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    #endregion

    #region Console / TTY Probe Functions

    /// <summary>
    /// Retrieves the current input mode of a console's input buffer or output mode of a console screen buffer.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    /// <summary>
    /// Retrieves the file type of the specified file.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetFileType(IntPtr hFile);

    /// <summary>
    /// Retrieves information about the specified console screen buffer.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetConsoleScreenBufferInfo(
        IntPtr hConsoleOutput,
        out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    /// <summary>
    /// Retrieves a handle to the specified standard device.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    #endregion

    #region Constants

    /// <summary>
    /// The process is to be run with EXTENDED_STARTUPINFO_PRESENT.
    /// Required for ConPTY.
    /// </summary>
    public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

    /// <summary>
    /// Create process with Unicode environment.
    /// </summary>
    public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    /// <summary>
    /// STARTF_USESTDHANDLES - DO NOT USE with ConPTY.
    /// ConPTY provides the console, not redirected handles.
    /// </summary>
    public const int STARTF_USESTDHANDLES = 0x00000100;

    /// <summary>
    /// Attribute for setting pseudoconsole on STARTUPINFOEX.
    /// </summary>
    public static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = (IntPtr)0x00020016;

    /// <summary>
    /// Infinite wait timeout.
    /// </summary>
    public const uint INFINITE = 0xFFFFFFFF;

    /// <summary>
    /// Wait succeeded.
    /// </summary>
    public const uint WAIT_OBJECT_0 = 0x00000000;

    /// <summary>
    /// Process is still active.
    /// </summary>
    public const uint STILL_ACTIVE = 259;

    /// <summary>
    /// Standard input handle.
    /// </summary>
    public const int STD_INPUT_HANDLE = -10;

    /// <summary>
    /// Standard output handle.
    /// </summary>
    public const int STD_OUTPUT_HANDLE = -11;

    /// <summary>
    /// Standard error handle.
    /// </summary>
    public const int STD_ERROR_HANDLE = -12;

    /// <summary>
    /// File type: Character device (console).
    /// </summary>
    public const uint FILE_TYPE_CHAR = 0x0002;

    /// <summary>
    /// File type: Pipe.
    /// </summary>
    public const uint FILE_TYPE_PIPE = 0x0003;

    /// <summary>
    /// File type: Unknown.
    /// </summary>
    public const uint FILE_TYPE_UNKNOWN = 0x0000;

    #endregion

    #region Structures

    /// <summary>
    /// Console coordinate.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X;
        public short Y;

        public COORD(short x, short y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Console screen buffer info.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public ushort wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    /// <summary>
    /// Small rectangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    /// <summary>
    /// Startup information for a process.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    /// <summary>
    /// Extended startup information with attribute list.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    /// <summary>
    /// Process information returned by CreateProcess.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    #endregion
}
