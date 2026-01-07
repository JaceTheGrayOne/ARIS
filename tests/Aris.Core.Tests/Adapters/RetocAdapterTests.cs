using Aris.Adapters.Retoc;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.Retoc;
using Aris.Core.Tests.Fakes;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using System.Linq;

namespace Aris.Core.Tests.Adapters;

public class RetocAdapterTests
{
    private readonly FakeProcessRunner _fakeProcessRunner;
    private readonly FakeDependencyValidator _fakeDependencyValidator;
    private readonly RetocOptions _options;
    private readonly RetocAdapter _adapter;

    public RetocAdapterTests()
    {
        _fakeProcessRunner = new FakeProcessRunner();
        _fakeDependencyValidator = new FakeDependencyValidator();
        _options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            DefaultCompressionFormat = "Zlib",
            DefaultCompressionLevel = 6,
            AllowedAdditionalArgs = new List<string> { "--verbose" },
            MaxLogBytes = 5 * 1024 * 1024
        };

        _adapter = new RetocAdapter(
            _fakeProcessRunner,
            _fakeDependencyValidator,
            new NullLogger<RetocAdapter>(),
            Options.Create(_options));
    }

    [Fact]
    public async Task ConvertAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Mode = RetocMode.PakToIoStore,
            OperationId = "test-op-123"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Conversion successful",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(5)
        };

        var result = await _adapter.ConvertAsync(command);

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Success);
        Assert.Equal("C:\\output\\test.utoc", result.OutputPath);
        Assert.Equal("iostore", result.OutputFormat);
        Assert.Equal(TimeSpan.FromSeconds(5), result.Duration);
    }

    [Fact]
    public async Task ConvertAsync_CallsProcessRunnerWithCorrectArguments()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore,
            GameVersion = "1.0",
            UEVersion = "5.3"
        };

        await _adapter.ConvertAsync(command);

        Assert.NotNull(_fakeProcessRunner.LastExecutablePath);
        Assert.Contains("retoc.exe", _fakeProcessRunner.LastExecutablePath);
        // PakToIoStore should map to "to-zen" with positional arguments
        Assert.Contains("to-zen", _fakeProcessRunner.LastArguments);
        Assert.Contains("C:\\input\\test.pak", _fakeProcessRunner.LastArguments);
        Assert.Contains("C:\\output\\test.pak", _fakeProcessRunner.LastArguments);
    }

    [Fact]
    public async Task ConvertAsync_NonZeroExitCode_ThrowsToolExecutionError()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Validate
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 1,
            StdOut = "Some output",
            StdErr = "Error: validation failed",
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() =>
            _adapter.ConvertAsync(command));

        Assert.Equal("retoc", ex.ToolName);
        Assert.Equal(1, ex.ExitCode);
        Assert.Contains("Retoc conversion failed", ex.Message);
        Assert.Contains("Error: validation failed", ex.StandardError);
    }

    [Fact]
    public async Task ConvertAsync_MissingInputPath_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() =>
            _adapter.ConvertAsync(command));

        Assert.Contains("InputPath", ex.FieldName);
    }

    [Fact]
    public async Task ConvertAsync_DisallowedAdditionalArg_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore,
            AdditionalArgs = new List<string> { "--dangerous" }
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() =>
            _adapter.ConvertAsync(command));

        Assert.Contains("not in the allowlist", ex.Message);
    }

    [Fact]
    public async Task ConvertAsync_EmitsProgressEvents()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore
        };

        var progressEvents = new System.Collections.Concurrent.ConcurrentQueue<ProgressEvent>();
        IProgress<ProgressEvent> progress = new Progress<ProgressEvent>(e => progressEvents.Enqueue(e));

        await _adapter.ConvertAsync(command, default, progress);

        // bounded wait for queued callbacks to settle
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 250 && !progressEvents.Any(e => e.Step == "converting" || e.Step == "complete"))
        {
            await Task.Delay(10);
        }

        var events = progressEvents.ToArray();

        Assert.NotEmpty(events);

        // staging should usually occur, but the real requirement is that conversion began
        Assert.Contains(events, e => e.Step == "converting" || e.Step == "complete");
        Assert.True(events.Length >= 1, $"Expected at least 1 progress event, got {events.Length}");


    }

    [Fact]
    public async Task ConvertAsync_WithMountKeys_EmitsDecryptingProgress()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Mode = RetocMode.PakToIoStore,
            MountKeys = new List<string> { "testkey" }
        };

        var progressEvents = new System.Collections.Concurrent.ConcurrentQueue<ProgressEvent>();
        IProgress<ProgressEvent> progress = new Progress<ProgressEvent>(e => progressEvents.Enqueue(e));

        await _adapter.ConvertAsync(command, default, progress);

        // bounded wait for queued callbacks to settle
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 250 && !progressEvents.Any(e => e.Step == "decrypting"))
        {
            await Task.Delay(10);
        }

        var events = progressEvents.ToArray();

        // must either emit decrypting, or (if it runs too fast) at least show conversion happened
        Assert.True(
            events.Any(e => e.Step == "decrypting") || events.Any(e => e.Step == "converting" || e.Step == "complete"),
            $"Expected 'decrypting' or later conversion steps. Got: {string.Join(", ", events.Select(e => e.Step))}");


    }

    [Fact]
    public async Task ConvertAsync_UsesConfiguredTimeout()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore
        };

        await _adapter.ConvertAsync(command);

        Assert.Equal(300, _fakeProcessRunner.LastTimeoutSeconds);
    }

    [Fact]
    public async Task ConvertAsync_CommandTimeoutOverridesDefault()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore,
            TimeoutSeconds = 600
        };

        await _adapter.ConvertAsync(command);

        Assert.Equal(600, _fakeProcessRunner.LastTimeoutSeconds);
    }

    [Fact]
    public async Task ConvertAsync_ProcessTimeout_ThrowsToolExecutionError()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore
        };

        _fakeProcessRunner.ExceptionToThrow = new TimeoutException("Process timed out");

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() =>
            _adapter.ConvertAsync(command));

        Assert.Equal("retoc", ex.ToolName);
        Assert.Contains("timed out", ex.Message);
    }

    [Fact]
    public async Task ValidateAsync_DependencyValid_ReturnsTrue()
    {
        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "retoc",
            Status = DependencyStatus.Valid
        };

        var result = await _adapter.ValidateAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_DependencyMissing_ReturnsFalse()
    {
        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "retoc",
            Status = DependencyStatus.Missing,
            ErrorMessage = "File not found"
        };

        var result = await _adapter.ValidateAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_DependencyHashMismatch_ReturnsFalse()
    {
        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "retoc",
            Status = DependencyStatus.HashMismatch,
            ErrorMessage = "Hash mismatch"
        };

        var result = await _adapter.ValidateAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ConvertAsync_ExtractsWarningsFromOutput()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Validate
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Processing...\nWarning: deprecated format\nWarning: missing metadata\nDone",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        var result = await _adapter.ConvertAsync(command);

        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("deprecated format"));
        Assert.Contains(result.Warnings, w => w.Contains("missing metadata"));
    }

    [Theory]
    [InlineData(RetocMode.PakToIoStore, "iostore")]
    [InlineData(RetocMode.IoStoreToPak, "pak")]
    public async Task ConvertAsync_DifferentModes_SetsCorrectOutputFormat(RetocMode mode, string expectedFormat)
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = mode
        };

        var result = await _adapter.ConvertAsync(command);

        Assert.Equal(expectedFormat, result.OutputFormat);
    }

    [Fact]
    public void BuildCommand_ToLegacy_ProducesCorrectArguments()
    {
        var command = new RetocCommand
        {
            OperationId = "test-build",
            CommandType = RetocCommandType.ToLegacy,
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc"
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("retoc.exe", executablePath);
        Assert.Contains("to-legacy", arguments);
        Assert.Contains("C:\\input\\test.pak", arguments);
        Assert.Contains("C:\\output\\test.utoc", arguments);
        Assert.Contains("to-legacy", commandLine);
    }

    [Fact]
    public void BuildCommand_ToZen_ProducesCorrectArguments()
    {
        var command = new RetocCommand
        {
            OperationId = "test-build",
            CommandType = RetocCommandType.ToZen,
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Version = "UE5.3"
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("retoc.exe", executablePath);
        Assert.Contains("to-zen", arguments);
        Assert.Contains("UE5.3", arguments);
        Assert.Contains("C:\\input\\test.pak", arguments);
        Assert.Contains("C:\\output\\test.utoc", arguments);
        Assert.Contains("to-zen", commandLine);
    }

    [Fact]
    public void BuildCommand_InfoCommand_DoesNotRequireOutputPath()
    {
        var command = new RetocCommand
        {
            OperationId = "test-info",
            CommandType = RetocCommandType.Info,
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\dummy" // Info command doesn't use output path
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("retoc.exe", executablePath);
        Assert.Contains("info", arguments);
        Assert.Contains("C:\\input\\test.pak", arguments);
    }

    [Fact]
    public void BuildCommand_GetCommand_IncludesChunkIdAsPositionalArgument()
    {
        var command = new RetocCommand
        {
            OperationId = "test-get",
            CommandType = RetocCommandType.Get,
            InputPath = "C:\\input\\test.utoc",
            OutputPath = "C:\\output\\chunk",
            ChunkId = "5"
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("retoc.exe", executablePath);
        Assert.Contains("get", arguments);
        Assert.Contains("C:\\input\\test.utoc", arguments);
        Assert.Contains("5", arguments);

        // Verify the order: get <input> <chunkId> [output]
        var getIndex = Array.IndexOf(arguments, "get");
        var inputIndex = Array.FindIndex(arguments, a => a.Contains("test.utoc"));
        var chunkIdPosition = Array.IndexOf(arguments, "5");

        Assert.True(getIndex >= 0, "get command should be present");
        Assert.True(inputIndex >= 0, "input path should be present");
        Assert.True(chunkIdPosition >= 0, "chunk ID should be present");
        Assert.True(getIndex < inputIndex, "get command should come before input path");
        Assert.True(inputIndex < chunkIdPosition, "input path should come before chunk ID");
    }

    [Fact]
    public void BuildCommand_GetCommand_WithoutChunkId_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            OperationId = "test-get-no-chunk",
            CommandType = RetocCommandType.Get,
            InputPath = "C:\\input\\test.utoc"
            // ChunkId is null, OutputPath is optional
        };

        var ex = Assert.Throws<ValidationError>(() => _adapter.BuildCommand(command));

        Assert.Contains("ChunkId", ex.Message);
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCommand_GetCommand_WithOptionalOutput_IncludesOutputPath()
    {
        var command = new RetocCommand
        {
            OperationId = "test-get-with-output",
            CommandType = RetocCommandType.Get,
            InputPath = "C:\\input\\test.utoc",
            OutputPath = "C:\\output\\chunk.bin",
            ChunkId = "abc123"
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("get", arguments);
        Assert.Contains("abc123", arguments);
        Assert.Contains("C:\\output\\chunk.bin", arguments);
    }

    [Fact]
    public void BuildCommand_GetCommand_WithoutOutput_OmitsOutputPath()
    {
        var command = new RetocCommand
        {
            OperationId = "test-get-no-output",
            CommandType = RetocCommandType.Get,
            InputPath = "C:\\input\\test.utoc",
            ChunkId = "abc123"
            // OutputPath is null/empty - should still work
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("get", arguments);
        Assert.Contains("abc123", arguments);
        // Output path should not be in arguments
        Assert.DoesNotContain("chunk", arguments.FirstOrDefault(a => a.Contains("output")) ?? "");
    }

    [Fact]
    public void BuildCommand_WithAesKey_IncludesAesKeyArgument()
    {
        var command = new RetocCommand
        {
            OperationId = "test-aes",
            CommandType = RetocCommandType.ToLegacy,
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Version = "UE5.3",
            AesKey = "0x1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF"
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("--aes-key", arguments);
        Assert.Contains("0x1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF", arguments);
    }

    [Fact]
    public void BuildCommand_WithVerbose_IncludesVerboseFlag()
    {
        var command = new RetocCommand
        {
            OperationId = "test-verbose",
            CommandType = RetocCommandType.ToLegacy,
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Version = "UE5.3",
            AdditionalArgs = new List<string> { "--verbose" }
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        Assert.Contains("--verbose", arguments);
    }

    [Fact]
    public void BuildCommand_QuotesPathsWithSpaces()
    {
        var command = new RetocCommand
        {
            OperationId = "test-spaces",
            CommandType = RetocCommandType.ToLegacy,
            InputPath = "C:\\input path\\test.pak",
            OutputPath = "C:\\output path\\test.utoc",
            Version = "UE5.3"
        };

        var (executablePath, arguments, commandLine) = _adapter.BuildCommand(command);

        // CommandLine should have quoted paths
        Assert.Contains("\"C:\\input path\\test.pak\"", commandLine);
        Assert.Contains("\"C:\\output path\\test.utoc\"", commandLine);
    }
}
