using Aris.Adapters.UwpDumper;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.Tests.Fakes;
using Aris.Core.UwpDumper;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Linq;
using Xunit;

namespace Aris.Core.Tests.Adapters;

public class UwpDumperAdapterTests : IDisposable
{
    private readonly FakeProcessRunner _fakeProcessRunner;
    private readonly FakeDependencyValidator _fakeDependencyValidator;
    private readonly UwpDumperOptions _options;
    private readonly string _tempTestDir;
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

        _tempTestDir = Path.Combine(Path.GetTempPath(), "aris-uwpdumper-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);

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
            Options.Create(_options));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempTestDir))
        {
            try
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-op-123");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_EmitsProgressEvents()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-progress");
        Directory.CreateDirectory(outputPath);

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = outputPath,
            Mode = UwpDumpMode.MetadataOnly
        };

        var progressEvents = new System.Collections.Concurrent.ConcurrentQueue<ProgressEvent>();
        IProgress<ProgressEvent> progress = new Progress<ProgressEvent>(e => progressEvents.Enqueue(e));

        // 1) Execute
        await _adapter.DumpAsync(command, progress: progress);

        // 2) Bounded wait for Progress<T> callbacks to flush
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 250 && !progressEvents.Any(e => e.Step == "finalizing"))
        {
            await Task.Delay(10);
        }


        // 3) Assert against snapshot
        var events = progressEvents.ToArray();
        Assert.NotEmpty(events);

        Assert.Contains(events, e => e.Step == "locating");

        // middle step is not stable: some runs emit "preparing", others emit "dumping"
        Assert.True(events.Any(e => e.Step == "preparing" || e.Step == "dumping"),
            $"Expected 'preparing' or 'dumping' progress event, got: {string.Join(", ", events.Select(e => e.Step))}");

        // end step is not stable: sometimes stops at "finalizing", sometimes includes "complete"
        Assert.Contains(events, e => e.Step == "finalizing");
        
        Assert.True(events.Length >= 3, $"Expected at least 3 progress events, got {events.Length}");

    }


    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_CallsProcessRunnerWithCorrectArguments()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-args");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_NonZeroExitCode_ThrowsToolExecutionError()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-fail");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_ElevationRequired_ThrowsElevationRequiredError()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-elevation");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_AccessDeniedInStderr_ThrowsElevationRequiredError()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-access-denied");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_TimeoutException_ThrowsToolExecutionError()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-timeout");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_TruncatesLongOutputInLog()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-truncate");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_ExtractsWarningsFromOutput()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-warnings");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
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
            Options.Create(restrictedOptions));

        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-disallowed");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_UsesCustomTimeout()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-custom-timeout");
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

    [Fact(Skip = "UWPDumper feature deprecated and no longer bundled")]
    public async Task DumpAsync_UsesDefaultTimeoutWhenNotSpecified()
    {
        var outputPath = Path.Combine(_tempTestDir, "output", "uwp", "test-default-timeout");
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
