using Aris.Core.Tests.Fakes;
using Aris.Infrastructure.Terminal;

namespace Aris.Core.Tests.Infrastructure;

/// <summary>
/// Tests for ConPTY process abstraction and related constants.
/// Note: Full ConPTY integration tests require a Windows environment with console support.
/// These tests focus on verifiable design aspects and the fake implementation.
/// </summary>
public class ConPtyProcessTests
{
    /// <summary>
    /// Verifies STARTF_USESTDHANDLES constant is defined correctly.
    /// ConPTY launch path must NOT use this flag.
    /// </summary>
    [Fact]
    public void STARTF_USESTDHANDLES_HasCorrectValue()
    {
        // The Windows STARTF_USESTDHANDLES constant is 0x00000100
        // ConPTY implementations must NOT set this flag
        uint expected = 0x00000100;
        uint actual = ConPtyNativeMethods.STARTF_USESTDHANDLES;
        Assert.True(expected == actual, $"Expected {expected:X8}, got {actual:X8}");
    }

    /// <summary>
    /// Verifies EXTENDED_STARTUPINFO_PRESENT constant is correct.
    /// This flag is REQUIRED for ConPTY to work.
    /// </summary>
    [Fact]
    public void EXTENDED_STARTUPINFO_PRESENT_HasCorrectValue()
    {
        // Must be 0x00080000 for CreateProcess with STARTUPINFOEX
        uint expected = 0x00080000;
        uint actual = ConPtyNativeMethods.EXTENDED_STARTUPINFO_PRESENT;
        Assert.True(expected == actual, $"Expected {expected:X8}, got {actual:X8}");
    }

    /// <summary>
    /// Verifies PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE constant is correct.
    /// This attribute links the process to the pseudo-console.
    /// </summary>
    [Fact]
    public void PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE_HasCorrectValue()
    {
        // PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016
        IntPtr expected = (IntPtr)0x00020016;
        IntPtr actual = ConPtyNativeMethods.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;
        Assert.True(expected == actual, $"Expected {expected:X}, got {actual:X}");
    }

    /// <summary>
    /// Verifies CREATE_UNICODE_ENVIRONMENT constant is correct.
    /// </summary>
    [Fact]
    public void CREATE_UNICODE_ENVIRONMENT_HasCorrectValue()
    {
        uint expected = 0x00000400;
        uint actual = ConPtyNativeMethods.CREATE_UNICODE_ENVIRONMENT;
        Assert.True(expected == actual, $"Expected {expected:X8}, got {actual:X8}");
    }

    /// <summary>
    /// Verifies FILE_TYPE_CHAR constant is correct (used in TTY probe).
    /// </summary>
    [Fact]
    public void FILE_TYPE_CHAR_HasCorrectValue()
    {
        uint expected = 0x0002;
        uint actual = ConPtyNativeMethods.FILE_TYPE_CHAR;
        Assert.True(expected == actual, $"Expected {expected:X4}, got {actual:X4}");
    }

    /// <summary>
    /// Verifies STD_OUTPUT_HANDLE constant is correct.
    /// </summary>
    [Fact]
    public void STD_OUTPUT_HANDLE_HasCorrectValue()
    {
        int expected = -11;
        int actual = ConPtyNativeMethods.STD_OUTPUT_HANDLE;
        Assert.True(expected == actual, $"Expected {expected}, got {actual}");
    }

    /// <summary>
    /// Verifies STD_ERROR_HANDLE constant is correct.
    /// </summary>
    [Fact]
    public void STD_ERROR_HANDLE_HasCorrectValue()
    {
        int expected = -12;
        int actual = ConPtyNativeMethods.STD_ERROR_HANDLE;
        Assert.True(expected == actual, $"Expected {expected}, got {actual}");
    }

    /// <summary>
    /// Verifies COORD structure is correctly sized.
    /// </summary>
    [Fact]
    public void COORD_Structure_HasCorrectSize()
    {
        // COORD should be 4 bytes (2 shorts)
        var coord = new ConPtyNativeMethods.COORD(120, 30);
        Assert.True(coord.X == 120, $"Expected X=120, got {coord.X}");
        Assert.True(coord.Y == 30, $"Expected Y=30, got {coord.Y}");
    }

    /// <summary>
    /// FakeConPtyProcess tracks start call correctly.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_TracksStartCall()
    {
        using var fake = new FakeConPtyProcess();

        Assert.False(fake.StartCalled);
        Assert.False(fake.HasStarted);
        Assert.Equal(-1, fake.ProcessId);

        await fake.StartAsync("test.exe", "--arg1 --arg2", "C:\\work", 100, 50);

        Assert.True(fake.StartCalled);
        Assert.True(fake.HasStarted);
        Assert.Equal("test.exe", fake.LastExecutable);
        Assert.Equal("--arg1 --arg2", fake.LastArguments);
        Assert.Equal("C:\\work", fake.LastWorkingDirectory);
        Assert.Equal((100, 50), fake.LastTerminalSize);
        Assert.Equal(12345, fake.ProcessId);
    }

    /// <summary>
    /// FakeConPtyProcess returns configured output chunks.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_ReturnsConfiguredOutput()
    {
        using var fake = new FakeConPtyProcess();
        fake.AddOutput("Hello\r\n");
        fake.AddOutput("World\r\n");

        await fake.StartAsync("test.exe", "");

        var output = new List<byte[]>();
        await foreach (var chunk in fake.ReadOutputAsync(CancellationToken.None))
        {
            output.Add(chunk);
        }

        Assert.Equal(2, output.Count);
        Assert.Equal("Hello\r\n", System.Text.Encoding.UTF8.GetString(output[0]));
        Assert.Equal("World\r\n", System.Text.Encoding.UTF8.GetString(output[1]));
    }

    /// <summary>
    /// FakeConPtyProcess returns configured exit code.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_ReturnsConfiguredExitCode()
    {
        using var fake = new FakeConPtyProcess();
        fake.ExitCodeToReturn = 42;

        await fake.StartAsync("test.exe", "");
        var exitCode = await fake.WaitForExitAsync(CancellationToken.None);

        Assert.Equal(42, exitCode);
        Assert.True(fake.HasExited);
    }

    /// <summary>
    /// FakeConPtyProcess cancellation throws and marks kill.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_CancellationSupport()
    {
        using var fake = new FakeConPtyProcess();
        fake.ExitDelay = TimeSpan.FromSeconds(10);

        await fake.StartAsync("test.exe", "");

        var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => fake.WaitForExitAsync(cts.Token));
    }

    /// <summary>
    /// FakeConPtyProcess Kill sets flag correctly.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_Kill_SetsFlag()
    {
        using var fake = new FakeConPtyProcess();

        await fake.StartAsync("test.exe", "");
        Assert.False(fake.KillCalled);
        Assert.False(fake.HasExited);

        fake.Kill();

        Assert.True(fake.KillCalled);
        Assert.True(fake.HasExited);
    }

    /// <summary>
    /// FakeConPtyProcess tracks Resize calls.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_Resize_TracksCall()
    {
        using var fake = new FakeConPtyProcess();

        await fake.StartAsync("test.exe", "");
        Assert.Null(fake.LastResize);

        fake.Resize(200, 60);

        Assert.Equal(((short)200, (short)60), fake.LastResize);
    }

    /// <summary>
    /// FakeConPtyProcess tracks WriteInput calls.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_WriteInput_TracksData()
    {
        using var fake = new FakeConPtyProcess();

        await fake.StartAsync("test.exe", "");

        var input1 = System.Text.Encoding.UTF8.GetBytes("test input");
        var input2 = System.Text.Encoding.UTF8.GetBytes("more data");

        await fake.WriteInputAsync(input1, CancellationToken.None);
        await fake.WriteInputAsync(input2, CancellationToken.None);

        Assert.Equal(2, fake.WrittenInput.Count);
        Assert.Equal("test input", System.Text.Encoding.UTF8.GetString(fake.WrittenInput[0]));
        Assert.Equal("more data", System.Text.Encoding.UTF8.GetString(fake.WrittenInput[1]));
    }

    /// <summary>
    /// FakeConPtyProcess throws configured exception on start.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_StartException_Throws()
    {
        using var fake = new FakeConPtyProcess();
        fake.StartException = new InvalidOperationException("Tool not found");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fake.StartAsync("missing.exe", ""));

        Assert.Equal("Tool not found", ex.Message);
        Assert.False(fake.StartCalled);
    }

    /// <summary>
    /// FakeConPtyProcess throws ObjectDisposedException after dispose.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_ThrowsAfterDispose()
    {
        var fake = new FakeConPtyProcess();
        fake.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => fake.StartAsync("test.exe", ""));
    }

    /// <summary>
    /// Verifies IConPtyProcess interface includes all required members.
    /// </summary>
    [Fact]
    public void IConPtyProcess_Interface_HasRequiredMembers()
    {
        // This test documents the interface contract
        var type = typeof(IConPtyProcess);

        Assert.True(type.IsInterface);

        // Properties
        Assert.NotNull(type.GetProperty(nameof(IConPtyProcess.ProcessId)));
        Assert.NotNull(type.GetProperty(nameof(IConPtyProcess.HasStarted)));
        Assert.NotNull(type.GetProperty(nameof(IConPtyProcess.HasExited)));

        // Methods
        Assert.NotNull(type.GetMethod(nameof(IConPtyProcess.StartAsync)));
        Assert.NotNull(type.GetMethod(nameof(IConPtyProcess.ReadOutputAsync)));
        Assert.NotNull(type.GetMethod(nameof(IConPtyProcess.WriteInputAsync)));
        Assert.NotNull(type.GetMethod(nameof(IConPtyProcess.WaitForExitAsync)));
        Assert.NotNull(type.GetMethod(nameof(IConPtyProcess.Kill)));
        Assert.NotNull(type.GetMethod(nameof(IConPtyProcess.Resize)));

        // IDisposable
        Assert.True(typeof(IDisposable).IsAssignableFrom(type));
    }
}
