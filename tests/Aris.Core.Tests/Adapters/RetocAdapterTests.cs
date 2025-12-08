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

namespace Aris.Core.Tests.Adapters;

public class RetocAdapterTests : IDisposable
{
    private readonly FakeProcessRunner _fakeProcessRunner;
    private readonly FakeDependencyValidator _fakeDependencyValidator;
    private readonly RetocOptions _options;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly string _tempWorkspacePath;
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

        // Create a temporary workspace directory for testing
        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), "aris-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspacePath);

        _workspaceOptions = new WorkspaceOptions
        {
            DefaultWorkspacePath = _tempWorkspacePath
        };

        _adapter = new RetocAdapter(
            _fakeProcessRunner,
            _fakeDependencyValidator,
            new NullLogger<RetocAdapter>(),
            Options.Create(_options),
            Options.Create(_workspaceOptions));
    }

    public void Dispose()
    {
        // Clean up temp workspace
        if (Directory.Exists(_tempWorkspacePath))
        {
            try
            {
                Directory.Delete(_tempWorkspacePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
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
            Mode = RetocMode.Repack,
            GameVersion = "1.0",
            UEVersion = "5.3"
        };

        await _adapter.ConvertAsync(command);

        Assert.NotNull(_fakeProcessRunner.LastExecutablePath);
        Assert.Contains("retoc.exe", _fakeProcessRunner.LastExecutablePath);
        Assert.Contains("repack", _fakeProcessRunner.LastArguments);
        Assert.Contains("--input", _fakeProcessRunner.LastArguments);
        Assert.Contains("C:\\input\\test.pak", _fakeProcessRunner.LastArguments);
        Assert.Contains("--output", _fakeProcessRunner.LastArguments);
        Assert.Contains("C:\\output\\test.pak", _fakeProcessRunner.LastArguments);
        Assert.Contains("--game-version \"1.0\"", _fakeProcessRunner.LastArguments);
        Assert.Contains("--ue-version \"5.3\"", _fakeProcessRunner.LastArguments);
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
            Mode = RetocMode.Repack
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
            Mode = RetocMode.Repack,
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
            Mode = RetocMode.Repack
        };

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _adapter.ConvertAsync(command, default, progress);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.Step == "staging");
        Assert.Contains(progressEvents, e => e.Step == "converting");
        Assert.True(progressEvents.Count >= 2, $"Expected at least 2 progress events, got {progressEvents.Count}");
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

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _adapter.ConvertAsync(command, default, progress);

        Assert.Contains(progressEvents, e => e.Step == "decrypting");
    }

    [Fact]
    public async Task ConvertAsync_UsesConfiguredTimeout()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack
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
            Mode = RetocMode.Repack,
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
            Mode = RetocMode.Repack
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
    [InlineData(RetocMode.Repack, "pak")]
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
    public async Task ConvertAsync_WritesOperationLogFile()
    {
        var operationId = "test-op-" + Guid.NewGuid().ToString("N");
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            OperationId = operationId,
            GameVersion = "1.0",
            UEVersion = "5.3"
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

        await _adapter.ConvertAsync(command);

        // Assert log file exists
        var expectedLogPath = Path.Combine(_tempWorkspacePath, "logs", $"retoc-{operationId}.log");
        Assert.True(File.Exists(expectedLogPath), $"Expected log file at {expectedLogPath}");

        // Assert log file contains expected content
        var logContent = File.ReadAllText(expectedLogPath);
        Assert.Contains(operationId, logContent);
        Assert.Contains("Exit Code: 0", logContent);
        Assert.Contains("Mode: Repack", logContent);
        Assert.Contains("Game Version: 1.0", logContent);
        Assert.Contains("UE Version: 5.3", logContent);
        Assert.Contains("Conversion successful", logContent);
    }

    [Fact]
    public async Task ConvertAsync_LogFileContainsFailureDetails()
    {
        var operationId = "test-fail-" + Guid.NewGuid().ToString("N");
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Validate,
            OperationId = operationId
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 1,
            StdOut = "Validation output",
            StdErr = "Error: validation failed",
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        // ConvertAsync will throw ToolExecutionError for non-zero exit code
        await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.ConvertAsync(command));

        // But log file should still be written
        var expectedLogPath = Path.Combine(_tempWorkspacePath, "logs", $"retoc-{operationId}.log");
        Assert.True(File.Exists(expectedLogPath), "Log file should exist even for failed operations");

        var logContent = File.ReadAllText(expectedLogPath);
        Assert.Contains("Exit Code: 1", logContent);
        Assert.Contains("Error: validation failed", logContent);
    }

    [Fact]
    public async Task ConvertAsync_LogFileRespectsMaxLogBytes()
    {
        var operationId = "test-truncate-" + Guid.NewGuid().ToString("N");
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            OperationId = operationId
        };

        // Create large output that exceeds MaxLogBytes
        var largeOutput = new string('A', 10 * 1024 * 1024); // 10 MB of 'A'

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = largeOutput,
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(5)
        };

        await _adapter.ConvertAsync(command);

        var expectedLogPath = Path.Combine(_tempWorkspacePath, "logs", $"retoc-{operationId}.log");
        var logContent = File.ReadAllText(expectedLogPath);
        var logSizeBytes = System.Text.Encoding.UTF8.GetByteCount(logContent);

        // Log should be significantly smaller than the original 10 MB output
        // (accounting for headers and other content, but should be under MaxLogBytes + some overhead)
        Assert.True(logSizeBytes < _options.MaxLogBytes + 10000,
            $"Log file size ({logSizeBytes} bytes) should be less than MaxLogBytes ({_options.MaxLogBytes}) plus small overhead");
        Assert.Contains("[truncated]", logContent);
    }
}
