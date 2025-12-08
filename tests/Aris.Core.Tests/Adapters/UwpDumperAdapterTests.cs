using Aris.Adapters.UwpDumper;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.Tests.Fakes;
using Aris.Core.UwpDumper;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.Adapters;

public class UwpDumperAdapterTests : IDisposable
{
    private readonly FakeProcessRunner _fakeProcessRunner;
    private readonly FakeDependencyValidator _fakeDependencyValidator;
    private readonly UwpDumperOptions _options;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly string _tempWorkspacePath;
    private readonly UwpDumperAdapter _adapter;

    public UwpDumperAdapterTests()
    {
        _fakeProcessRunner = new FakeProcessRunner();
        _fakeDependencyValidator = new FakeDependencyValidator();
        _options = new UwpDumperOptions
        {
            DefaultTimeoutSeconds = 300,
            RequireElevation = true,
            AllowedModes = new[] { "FullDump", "MetadataOnly", "ValidateOnly" },
            MaxLogBytes = 5 * 1024 * 1024,
            KeepTempOnFailure = false
        };

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), "aris-uwpdumper-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspacePath);

        _workspaceOptions = new WorkspaceOptions
        {
            DefaultWorkspacePath = _tempWorkspacePath
        };

        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "uwpdumper",
            Status = DependencyStatus.Valid,
            ExpectedPath = "C:\\fake\\uwpdumper.exe",
            ExpectedHash = "fakehash",
            ActualHash = "fakehash"
        };

        _adapter = new UwpDumperAdapter(
            _fakeProcessRunner,
            _fakeDependencyValidator,
            new NullLogger<UwpDumperAdapter>(),
            Options.Create(_options),
            Options.Create(_workspaceOptions));
    }

    public void Dispose()
    {
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
    public async Task ValidateAsync_DependencyValid_ReturnsTrue()
    {
        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "uwpdumper",
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
            ToolId = "uwpdumper",
            Status = DependencyStatus.Missing,
            ErrorMessage = "Tool not found"
        };

        var result = await _adapter.ValidateAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task DumpAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-op-123");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump,
            OperationId = "test-op-123"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Dump successful\nPackage dumped to output",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(10),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(10)
        };

        var result = await _adapter.DumpAsync(command);

        Assert.Equal("test-op-123", result.OperationId);
        Assert.Equal("Microsoft.MinecraftUWP_8wekyb3d8bbwe", result.PackageFamilyName);
        Assert.Equal(outputPath, result.OutputPath);
        Assert.Equal(TimeSpan.FromSeconds(10), result.Duration);
        Assert.NotNull(result.Artifacts);
        Assert.NotNull(result.Warnings);
    }

    [Fact]
    public async Task DumpAsync_EmitsProgressEvents()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-progress");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.MetadataOnly
        };

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _adapter.DumpAsync(command, progress: progress);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.Step == "locating");
        Assert.Contains(progressEvents, e => e.Step == "preparing");
        Assert.Contains(progressEvents, e => e.Step == "finalizing");
        Assert.Contains(progressEvents, e => e.Step == "complete");
        Assert.True(progressEvents.Count >= 4, $"Expected at least 4 progress events, got {progressEvents.Count}");
    }

    [Fact]
    public async Task DumpAsync_CreatesOperationLog()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-log");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            ApplicationId = "App",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump,
            IncludeSymbols = true,
            OperationId = "test-log-123"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Log output",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(5)
        };

        await _adapter.DumpAsync(command);

        var logPath = Path.Combine(_tempWorkspacePath, "logs", "uwpdumper-test-log-123.log");
        Assert.True(File.Exists(logPath), "Operation log file should exist");

        var logContent = File.ReadAllText(logPath);
        Assert.Contains("test-log-123", logContent);
        Assert.Contains("Microsoft.MinecraftUWP_8wekyb3d8bbwe", logContent);
        Assert.Contains("App", logContent);
        Assert.Contains("FullDump", logContent);
        Assert.Contains("Include Symbols: True", logContent);
        Assert.Contains("Exit Code: 0", logContent);
    }

    [Fact]
    public async Task DumpAsync_CallsProcessRunnerWithCorrectArguments()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-args");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            ApplicationId = "App123",
            OutputPath = outputPath,
            Mode = UwpDumpMode.MetadataOnly,
            IncludeSymbols = true
        };

        await _adapter.DumpAsync(command);

        Assert.NotNull(_fakeProcessRunner.LastExecutablePath);
        Assert.Contains("uwpdumper.exe", _fakeProcessRunner.LastExecutablePath);
        Assert.Contains("--pfn", _fakeProcessRunner.LastArguments);
        Assert.Contains("Microsoft.MinecraftUWP_8wekyb3d8bbwe", _fakeProcessRunner.LastArguments);
        Assert.Contains("--appid", _fakeProcessRunner.LastArguments);
        Assert.Contains("App123", _fakeProcessRunner.LastArguments);
        Assert.Contains("--output", _fakeProcessRunner.LastArguments);
        Assert.Contains(outputPath, _fakeProcessRunner.LastArguments);
        Assert.Contains("--mode", _fakeProcessRunner.LastArguments);
        Assert.Contains("metadata", _fakeProcessRunner.LastArguments);
        Assert.Contains("--symbols", _fakeProcessRunner.LastArguments);
    }

    [Fact]
    public async Task DumpAsync_NonZeroExitCode_ThrowsToolExecutionError()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-fail");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.ValidateOnly
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 1,
            StdOut = "Some output",
            StdErr = "Error: package not found",
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() =>
            _adapter.DumpAsync(command));

        Assert.Equal("uwpdumper", ex.ToolName);
        Assert.Equal(1, ex.ExitCode);
        Assert.Contains("dump operation failed", ex.Message);
        Assert.Contains("Error: package not found", ex.StandardError);
    }

    [Fact]
    public async Task DumpAsync_ElevationRequired_ThrowsElevationRequiredError()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-elevation");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump,
            OperationId = "test-elevation-123"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 5,
            StdOut = string.Empty,
            StdErr = "Access denied. Administrator privileges required.",
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ElevationRequiredError>(() =>
            _adapter.DumpAsync(command));

        Assert.Equal("test-elevation-123", ex.OperationId);
        Assert.Contains("elevation", ex.Message.ToLowerInvariant());
        Assert.NotNull(ex.RemediationHint);
        Assert.Contains("administrator", ex.RemediationHint.ToLowerInvariant());
    }

    [Fact]
    public async Task DumpAsync_AccessDeniedInStderr_ThrowsElevationRequiredError()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-access-denied");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 1,
            StdOut = string.Empty,
            StdErr = "Error: access denied to package",
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ElevationRequiredError>(() =>
            _adapter.DumpAsync(command));

        Assert.Contains("elevation", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task DumpAsync_TimeoutException_ThrowsToolExecutionError()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-timeout");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump,
            TimeoutSeconds = 60
        };

        _fakeProcessRunner.ExceptionToThrow = new TimeoutException("Process timed out");

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() =>
            _adapter.DumpAsync(command));

        Assert.Equal("uwpdumper", ex.ToolName);
        Assert.Contains("timed out", ex.Message);
        Assert.NotNull(ex.RemediationHint);
        Assert.Contains("timeout", ex.RemediationHint.ToLowerInvariant());
    }

    [Fact]
    public async Task DumpAsync_TruncatesLongOutputInLog()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-truncate");
        Directory.CreateDirectory(outputPath);

        var longOutput = new string('x', 10 * 1024 * 1024); // 10 MB of output

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump,
            OperationId = "test-truncate-123"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = longOutput,
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(5)
        };

        var result = await _adapter.DumpAsync(command);

        Assert.NotNull(result.LogExcerpt);
        Assert.True(result.LogExcerpt.Length < longOutput.Length);
        Assert.Contains("[truncated]", result.LogExcerpt);
    }

    [Fact]
    public async Task DumpAsync_ExtractsWarningsFromOutput()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-warnings");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Processing package...\nWarning: Symbol files not found\nWarning: Metadata incomplete\nDump completed",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(5),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(5)
        };

        var result = await _adapter.DumpAsync(command);

        Assert.NotNull(result.Warnings);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains(result.Warnings, w => w.Contains("Symbol files not found"));
        Assert.Contains(result.Warnings, w => w.Contains("Metadata incomplete"));
    }

    [Fact]
    public async Task DumpAsync_InvalidCommand_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "",
            OutputPath = "relative/path",
            Mode = UwpDumpMode.FullDump
        };

        await Assert.ThrowsAsync<ValidationError>(() =>
            _adapter.DumpAsync(command));
    }

    [Fact]
    public async Task DumpAsync_DisallowedMode_ThrowsValidationError()
    {
        var restrictedOptions = new UwpDumperOptions
        {
            AllowedModes = new[] { "MetadataOnly" }
        };

        var restrictedAdapter = new UwpDumperAdapter(
            _fakeProcessRunner,
            _fakeDependencyValidator,
            new NullLogger<UwpDumperAdapter>(),
            Options.Create(restrictedOptions),
            Options.Create(_workspaceOptions));

        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-disallowed");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() =>
            restrictedAdapter.DumpAsync(command));

        Assert.Contains("not allowed", ex.Message);
        Assert.Contains("FullDump", ex.Message);
    }

    [Fact]
    public async Task DumpAsync_UsesCustomTimeout()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-custom-timeout");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump,
            TimeoutSeconds = 600
        };

        await _adapter.DumpAsync(command);

        Assert.Equal(600, _fakeProcessRunner.LastTimeoutSeconds);
    }

    [Fact]
    public async Task DumpAsync_UsesDefaultTimeoutWhenNotSpecified()
    {
        var outputPath = Path.Combine(_tempWorkspacePath, "output", "uwp", "test-default-timeout");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.FullDump
        };

        await _adapter.DumpAsync(command);

        Assert.Equal(_options.DefaultTimeoutSeconds, _fakeProcessRunner.LastTimeoutSeconds);
    }
}
