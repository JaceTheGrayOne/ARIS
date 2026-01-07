using System.Runtime.CompilerServices;
using Aris.Infrastructure.Terminal;

namespace Aris.Core.Tests.Fakes;

/// <summary>
/// Fake ConPTY process for testing without real Windows pseudo-console.
/// </summary>
public sealed class FakeConPtyProcess : IConPtyProcess
{
    private readonly List<byte[]> _outputChunks = new();
    private int _outputIndex;
    private bool _isDisposed;

    /// <summary>
    /// Output chunks to return when ReadOutputAsync is called.
    /// </summary>
    public IList<byte[]> OutputChunksToReturn => _outputChunks;

    /// <summary>
    /// Exit code to return when WaitForExitAsync is called.
    /// </summary>
    public int ExitCodeToReturn { get; set; } = 0;

    /// <summary>
    /// Delay before returning output chunks (simulates slow I/O).
    /// </summary>
    public TimeSpan OutputDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Delay before WaitForExitAsync returns (simulates long-running process).
    /// </summary>
    public TimeSpan ExitDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Exception to throw when StartAsync is called (null for success).
    /// </summary>
    public Exception? StartException { get; set; }

    /// <summary>
    /// The last executable passed to StartAsync.
    /// </summary>
    public string? LastExecutable { get; private set; }

    /// <summary>
    /// The last arguments passed to StartAsync.
    /// </summary>
    public string? LastArguments { get; private set; }

    /// <summary>
    /// The last working directory passed to StartAsync.
    /// </summary>
    public string? LastWorkingDirectory { get; private set; }

    /// <summary>
    /// The last terminal size passed to StartAsync.
    /// </summary>
    public (short Width, short Height) LastTerminalSize { get; private set; }

    /// <summary>
    /// Whether StartAsync was called.
    /// </summary>
    public bool StartCalled { get; private set; }

    /// <summary>
    /// Whether Kill was called.
    /// </summary>
    public bool KillCalled { get; private set; }

    /// <summary>
    /// Data written via WriteInputAsync.
    /// </summary>
    public List<byte[]> WrittenInput { get; } = new();

    /// <summary>
    /// Last resize dimensions.
    /// </summary>
    public (short Width, short Height)? LastResize { get; private set; }

    /// <inheritdoc />
    public int ProcessId => StartCalled ? 12345 : -1;

    /// <inheritdoc />
    public bool HasStarted => StartCalled;

    /// <inheritdoc />
    public bool HasExited { get; private set; }

    /// <inheritdoc />
    public Task StartAsync(
        string executable,
        string arguments,
        string? workingDirectory = null,
        short terminalWidth = 120,
        short terminalHeight = 30)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FakeConPtyProcess));

        if (StartException != null)
            throw StartException;

        StartCalled = true;
        LastExecutable = executable;
        LastArguments = arguments;
        LastWorkingDirectory = workingDirectory;
        LastTerminalSize = (terminalWidth, terminalHeight);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<byte[]> ReadOutputAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FakeConPtyProcess));

        while (_outputIndex < _outputChunks.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (OutputDelay > TimeSpan.Zero)
            {
                await Task.Delay(OutputDelay, cancellationToken);
            }

            yield return _outputChunks[_outputIndex++];
        }
    }

    /// <inheritdoc />
    public Task WriteInputAsync(byte[] data, CancellationToken cancellationToken)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FakeConPtyProcess));

        cancellationToken.ThrowIfCancellationRequested();
        WrittenInput.Add(data);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> WaitForExitAsync(CancellationToken cancellationToken)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FakeConPtyProcess));

        if (ExitDelay > TimeSpan.Zero)
        {
            await Task.Delay(ExitDelay, cancellationToken);
        }

        HasExited = true;
        return ExitCodeToReturn;
    }

    /// <inheritdoc />
    public void Kill()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FakeConPtyProcess));

        KillCalled = true;
        HasExited = true;
    }

    /// <inheritdoc />
    public void Resize(short width, short height)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FakeConPtyProcess));

        LastResize = (width, height);
    }

    /// <summary>
    /// Adds an output chunk to return (convenience method).
    /// </summary>
    public void AddOutput(string text)
    {
        _outputChunks.Add(System.Text.Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Resets the output index to allow re-reading chunks.
    /// </summary>
    public void ResetOutputIndex()
    {
        _outputIndex = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _isDisposed = true;
    }
}
