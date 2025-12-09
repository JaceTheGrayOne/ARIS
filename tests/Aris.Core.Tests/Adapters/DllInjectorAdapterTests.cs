using Aris.Adapters.DllInjector;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.Tests.Fakes;
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.Adapters;

public class DllInjectorAdapterTests : IDisposable
{
    private readonly FakeProcessRunner _fakeProcessRunner;
    private readonly FakeProcessResolver _fakeProcessResolver;
    private readonly FakeDependencyValidator _fakeDependencyValidator;
    private readonly DllInjectorOptions _options;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly string _tempWorkspacePath;
    private readonly string _tempPayloadPath;
    private readonly DllInjectorAdapter _adapter;

    public DllInjectorAdapterTests()
    {
        _fakeProcessRunner = new FakeProcessRunner();
        _fakeProcessResolver = new FakeProcessResolver();
        _fakeDependencyValidator = new FakeDependencyValidator();

        _options = new DllInjectorOptions
        {
            DefaultTimeoutSeconds = 60,
            RequireElevation = true,
            AllowedTargets = Array.Empty<string>(),
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe", "services.exe" },
            AllowedMethods = new[] { "CreateRemoteThread", "ApcQueue", "ManualMap" },
            MaxLogBytes = 4 * 1024, // 4 KB for easier truncation testing
            KeepTempOnFailure = false
        };

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), "aris-dllinjector-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspacePath);

        var payloadsDir = Path.Combine(_tempWorkspacePath, "input", "payloads");
        Directory.CreateDirectory(payloadsDir);
        _tempPayloadPath = Path.Combine(payloadsDir, "test_payload.dll");
        File.WriteAllText(_tempPayloadPath, "fake dll content for testing");

        _workspaceOptions = new WorkspaceOptions
        {
            DefaultWorkspacePath = _tempWorkspacePath
        };

        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "dllinjector",
            Status = DependencyStatus.Valid,
            ExpectedPath = "C:\\fake\\dllinjector.exe",
            ExpectedHash = "fakehash",
            ActualHash = "fakehash"
        };

        _fakeProcessResolver.ProcessIdToReturn = 1234;

        _adapter = new DllInjectorAdapter(
            _fakeProcessRunner,
            _fakeProcessResolver,
            _fakeDependencyValidator,
            new NullLogger<DllInjectorAdapter>(),
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
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_DependencyValid_ReturnsTrue()
    {
        _fakeDependencyValidator.ToolResultToReturn = new ToolValidationResult
        {
            ToolId = "dllinjector",
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
            ToolId = "dllinjector",
            Status = DependencyStatus.Missing,
            ErrorMessage = "Tool not found"
        };

        var result = await _adapter.ValidateAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task InjectAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            OperationId = "test-inject-123"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Injection successful\nwarning: module already loaded\nCompleted injection",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(2),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(2)
        };

        var result = await _adapter.InjectAsync(command);

        Assert.Equal("test-inject-123", result.OperationId);
        Assert.Equal(1234, result.ProcessId);
        Assert.NotEmpty(result.ProcessName);
        Assert.Equal(_tempPayloadPath, result.DllPath);
        Assert.Equal(DllInjectionMethod.CreateRemoteThread, result.Method);
        Assert.True(result.ElevationUsed);
        Assert.Equal(TimeSpan.FromSeconds(2), result.Duration);
        Assert.NotNull(result.Warnings);
        Assert.Single(result.Warnings);
        Assert.Contains("warning:", result.Warnings[0]);
    }

    [Fact]
    public async Task InjectAsync_EmitsProgressEvents()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _adapter.InjectAsync(command, progress: progress);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.Step == "resolving");
        Assert.Contains(progressEvents, e => e.Step == "validating");
        Assert.Contains(progressEvents, e => e.Step == "injecting");
    }

    [Fact]
    public async Task EjectAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            OperationId = "test-eject-456"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Module unloaded successfully",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1),
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddSeconds(1)
        };

        var result = await _adapter.EjectAsync(command);

        Assert.Equal("test-eject-456", result.OperationId);
        Assert.Equal(1234, result.ProcessId);
        Assert.NotEmpty(result.ProcessName);
        Assert.Equal("payload.dll", result.ModuleName);
        Assert.Equal(TimeSpan.FromSeconds(1), result.Duration);
        Assert.True(result.IsUnloaded);
    }

    [Fact]
    public async Task EjectAsync_EmitsProgressEvents()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _adapter.EjectAsync(command, progress: progress);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.Step == "resolving");
        Assert.Contains(progressEvents, e => e.Step == "ejecting");
    }

    [Fact]
    public async Task InjectAsync_InvalidCommand_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.ManualMap
        };

        var restrictedOptions = new DllInjectorOptions
        {
            AllowedMethods = new[] { "CreateRemoteThread" },
            DeniedTargets = _options.DeniedTargets,
            MaxLogBytes = _options.MaxLogBytes
        };

        var adapter = new DllInjectorAdapter(
            _fakeProcessRunner,
            _fakeProcessResolver,
            _fakeDependencyValidator,
            new NullLogger<DllInjectorAdapter>(),
            Options.Create(restrictedOptions),
            Options.Create(_workspaceOptions));

        await Assert.ThrowsAsync<ValidationError>(() => adapter.InjectAsync(command));

        Assert.Null(_fakeProcessRunner.LastExecutablePath);
    }

    [Fact]
    public async Task EjectAsync_InvalidCommand_ThrowsValidationError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = ""
        };

        await Assert.ThrowsAsync<ValidationError>(() => _adapter.EjectAsync(command));

        Assert.Null(_fakeProcessRunner.LastExecutablePath);
    }

    [Fact]
    public async Task InjectAsync_NonZeroExitCode_ThrowsToolExecutionError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 13,
            StdOut = "Some output",
            StdErr = "Injection failed: target process incompatible",
            Duration = TimeSpan.FromSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.InjectAsync(command));

        Assert.Equal(13, ex.ExitCode);
        Assert.Contains("Injection failed", ex.StandardError);
        Assert.NotNull(ex.RemediationHint);
    }

    [Fact]
    public async Task EjectAsync_NonZeroExitCode_ThrowsToolExecutionError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 7,
            StdOut = string.Empty,
            StdErr = "Module not found in target process",
            Duration = TimeSpan.FromSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.EjectAsync(command));

        Assert.Equal(7, ex.ExitCode);
        Assert.Contains("Module not found", ex.StandardError);
    }

    [Fact]
    public async Task InjectAsync_ElevationRequired_ExitCode5_ThrowsElevationRequiredError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 5,
            StdOut = string.Empty,
            StdErr = "Access denied",
            Duration = TimeSpan.FromSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ElevationRequiredError>(() => _adapter.InjectAsync(command));

        Assert.NotNull(ex.OperationId);
        Assert.Contains("administrator", ex.RemediationHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InjectAsync_ElevationRequired_AccessDeniedInStderr_ThrowsElevationRequiredError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 1,
            StdOut = string.Empty,
            StdErr = "Error: access is denied. Elevation required.",
            Duration = TimeSpan.FromSeconds(1)
        };

        var ex = await Assert.ThrowsAsync<ElevationRequiredError>(() => _adapter.InjectAsync(command));

        Assert.NotNull(ex.RemediationHint);
    }

    [Fact]
    public async Task InjectAsync_ElevationNotRequired_AccessDenied_ThrowsToolExecutionError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            RequireElevationOverride = false
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 5,
            StdOut = string.Empty,
            StdErr = "Access denied",
            Duration = TimeSpan.FromSeconds(1)
        };

        await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.InjectAsync(command));
    }

    [Fact]
    public async Task InjectAsync_Timeout_ThrowsToolExecutionError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessRunner.ExceptionToThrow = new TimeoutException("Operation timed out");

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.InjectAsync(command));

        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timeout", ex.RemediationHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EjectAsync_Timeout_ThrowsToolExecutionError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        _fakeProcessRunner.ExceptionToThrow = new TimeoutException("Operation timed out");

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.EjectAsync(command));

        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InjectAsync_WritesOperationLogFile()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            OperationId = "test-log-write"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Injection completed successfully",
            StdErr = string.Empty,
            Duration = TimeSpan.FromMilliseconds(1500)
        };

        await _adapter.InjectAsync(command);

        var logPath = Path.Combine(_tempWorkspacePath, "logs", "dllinjector-test-log-write.log");
        Assert.True(File.Exists(logPath), "Log file should be created");

        var logContent = File.ReadAllText(logPath);
        Assert.Contains("test-log-write", logContent);
        Assert.Contains("Exit Code: 0", logContent);
        Assert.Contains("Duration:", logContent);
        Assert.Contains("Injection completed successfully", logContent);
    }

    [Fact]
    public async Task InjectAsync_LogFileRespectsMaxLogBytes()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            OperationId = "test-log-truncate"
        };

        var hugeOutput = new string('X', 10 * 1024); // 10 KB of output
        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = hugeOutput,
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        await _adapter.InjectAsync(command);

        var logPath = Path.Combine(_tempWorkspacePath, "logs", "dllinjector-test-log-truncate.log");
        var logContent = File.ReadAllText(logPath);

        Assert.Contains("[truncated", logContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InjectAsync_ProcessResolverThrows_BubblesValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessName = "DeniedProcess.exe",
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessResolver.ErrorToThrow = new ValidationError("Process denied by policy")
        {
            RemediationHint = "Choose a different process"
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() => _adapter.InjectAsync(command));

        Assert.Contains("denied by policy", ex.Message);
        Assert.Null(_fakeProcessRunner.LastExecutablePath);
    }

    [Fact]
    public async Task InjectAsync_UsesCustomTimeout()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            TimeoutSeconds = 120
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        await _adapter.InjectAsync(command);

        Assert.Equal(120, _fakeProcessRunner.LastTimeoutSeconds);
    }

    [Fact]
    public async Task InjectAsync_UsesDefaultTimeout_WhenNotSpecified()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        await _adapter.InjectAsync(command);

        Assert.Equal(60, _fakeProcessRunner.LastTimeoutSeconds);
    }

    [Fact]
    public async Task EjectAsync_UsesCustomTimeout()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            TimeoutSeconds = 90
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        await _adapter.EjectAsync(command);

        Assert.Equal(90, _fakeProcessRunner.LastTimeoutSeconds);
    }

    [Fact]
    public async Task InjectAsync_CreatesWorkingDirectory()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            OperationId = "test-workdir"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        await _adapter.InjectAsync(command);

        var expectedWorkDir = Path.Combine(_tempWorkspacePath, "temp", "inject-test-workdir");
        Assert.NotNull(_fakeProcessRunner.LastWorkingDirectory);
        Assert.Contains("inject-test-workdir", _fakeProcessRunner.LastWorkingDirectory);
        Assert.True(Directory.Exists(expectedWorkDir), "Working directory should be created");
    }

    [Fact]
    public async Task EjectAsync_CreatesWorkingDirectory()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            OperationId = "test-eject-workdir"
        };

        _fakeProcessRunner.ResultToReturn = new ProcessResult
        {
            ExitCode = 0,
            StdOut = "Success",
            StdErr = string.Empty,
            Duration = TimeSpan.FromSeconds(1)
        };

        await _adapter.EjectAsync(command);

        var expectedWorkDir = Path.Combine(_tempWorkspacePath, "temp", "eject-test-eject-workdir");
        Assert.NotNull(_fakeProcessRunner.LastWorkingDirectory);
        Assert.Contains("eject-test-eject-workdir", _fakeProcessRunner.LastWorkingDirectory);
        Assert.True(Directory.Exists(expectedWorkDir), "Working directory should be created");
    }
}
