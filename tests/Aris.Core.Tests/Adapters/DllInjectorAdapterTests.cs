using Aris.Adapters.DllInjector;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.Tests.Fakes;
using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.Adapters;

public class DllInjectorAdapterTests : IDisposable
{
    private readonly FakeDllInjectionService _fakeInjectionService;
    private readonly FakeProcessResolver _fakeProcessResolver;
    private readonly DllInjectorOptions _options;
    private readonly string _tempTestDir;
    private readonly string _tempPayloadPath;
    private readonly DllInjectorAdapter _adapter;

    public DllInjectorAdapterTests()
    {
        _fakeInjectionService = new FakeDllInjectionService();
        _fakeProcessResolver = new FakeProcessResolver();

        _options = new DllInjectorOptions
        {
            DefaultTimeoutSeconds = 60,
            RequireElevation = true,
            AllowedTargets = Array.Empty<string>(),
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe", "services.exe" },
            AllowedMethods = new[] { "CreateRemoteThread", "ApcQueue", "ManualMap" },
            MaxLogBytes = 4 * 1024,
            KeepTempOnFailure = false
        };

        _tempTestDir = Path.Combine(Path.GetTempPath(), "aris-dllinjector-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);

        var payloadsDir = Path.Combine(_tempTestDir, "input", "payloads");
        Directory.CreateDirectory(payloadsDir);
        _tempPayloadPath = Path.Combine(payloadsDir, "test_payload.dll");
        File.WriteAllText(_tempPayloadPath, "fake dll content for testing");

        _fakeProcessResolver.ProcessIdToReturn = 1234;

        _adapter = new DllInjectorAdapter(
            _fakeInjectionService,
            _fakeProcessResolver,
            new NullLogger<DllInjectorAdapter>(),
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
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_NativeInjector_ReturnsTrue()
    {
        // Native injector is always available on Windows
        var result = await _adapter.ValidateAsync();

        Assert.True(result);
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

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = true,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = new IntPtr(0x12340000),
            Duration = TimeSpan.FromMilliseconds(150),
            ErrorMessage = null,
            Win32ErrorCode = null,
            RequiresElevation = false
        };

        var result = await _adapter.InjectAsync(command);

        Assert.Equal("test-inject-123", result.OperationId);
        Assert.Equal(1234, result.ProcessId);
        Assert.NotEmpty(result.ProcessName);
        Assert.Equal(_tempPayloadPath, result.DllPath);
        Assert.Equal(DllInjectionMethod.CreateRemoteThread, result.Method);
        Assert.False(result.ElevationUsed);
        Assert.NotNull(result.Warnings);
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

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = true,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = new IntPtr(0x12340000),
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            Win32ErrorCode = null,
            RequiresElevation = false
        };

        var progressEvents = new System.Collections.Concurrent.ConcurrentQueue<ProgressEvent>();
        IProgress<ProgressEvent> progress = new Progress<ProgressEvent>(e => progressEvents.Enqueue(e));

        await _adapter.InjectAsync(command, progress: progress);

        // Bounded wait for queued callbacks to settle
        // There are 5 progress events: resolving, validating, injecting, verifying, finalizing
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 250 && progressEvents.Count < 5)
        {
            await Task.Delay(10);
        }

        var events = progressEvents.ToArray();

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Step == "resolving");
        Assert.Contains(events, e => e.Step == "validating");
        Assert.Contains(events, e => e.Step == "injecting");
        Assert.Contains(events, e => e.Step == "verifying");
        Assert.Contains(events, e => e.Step == "finalizing");
    }

    [Fact]
    public async Task EjectAsync_NotImplemented_ThrowsNotImplementedException()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            OperationId = "test-eject-456"
        };

        await Assert.ThrowsAsync<NotImplementedException>(() => _adapter.EjectAsync(command));
    }

    [Fact]
    public async Task InjectAsync_InjectionFailsRequiresElevation_ThrowsElevationRequiredError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = false,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = IntPtr.Zero,
            Duration = TimeSpan.FromMilliseconds(50),
            ErrorMessage = "Access denied",
            Win32ErrorCode = 5, // ERROR_ACCESS_DENIED
            RequiresElevation = true
        };

        var ex = await Assert.ThrowsAsync<ElevationRequiredError>(() => _adapter.InjectAsync(command));

        Assert.NotNull(ex.OperationId);
        Assert.Contains("administrator", ex.RemediationHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InjectAsync_InjectionFailsOtherReason_ThrowsToolExecutionError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = false,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = IntPtr.Zero,
            Duration = TimeSpan.FromMilliseconds(50),
            ErrorMessage = "LoadLibraryW returned NULL",
            Win32ErrorCode = null,
            RequiresElevation = false
        };

        var ex = await Assert.ThrowsAsync<ToolExecutionError>(() => _adapter.InjectAsync(command));

        Assert.Equal("dllinjector", ex.ToolName);
        Assert.Contains("LoadLibraryW", ex.Message);
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

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = true,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = new IntPtr(0x12340000),
            Duration = TimeSpan.FromMilliseconds(150),
            ErrorMessage = null,
            Win32ErrorCode = null,
            RequiresElevation = false
        };

        await _adapter.InjectAsync(command);

        // Log file is now written to system temp path
        var logPath = Path.Combine(Path.GetTempPath(), "aris", "logs", "dllinjector-test-log-write.log");
        Assert.True(File.Exists(logPath), "Log file should be created");

        var logContent = File.ReadAllText(logPath);
        Assert.Contains("test-log-write", logContent);
        Assert.Contains("Success: True", logContent);
        Assert.Contains("Duration:", logContent);
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
        // Injection service should not be called when process resolution fails
        Assert.Null(_fakeInjectionService.LastDllPath);
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

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = true,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = new IntPtr(0x12340000),
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            Win32ErrorCode = null,
            RequiresElevation = false
        };

        await _adapter.InjectAsync(command);

        // Working directory is now created in system temp path
        var expectedWorkDir = Path.Combine(Path.GetTempPath(), "aris", "temp", "inject-test-workdir");
        Assert.True(Directory.Exists(expectedWorkDir), "Working directory should be created");
    }

    [Fact]
    public async Task InjectAsync_PassesCorrectParametersToInjectionService()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            OperationId = "test-params"
        };

        _fakeInjectionService.ResultToReturn = new DllInjectionResult
        {
            Success = true,
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            LoadedModuleAddress = new IntPtr(0x12340000),
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            Win32ErrorCode = null,
            RequiresElevation = false
        };

        await _adapter.InjectAsync(command);

        Assert.Equal(1234, _fakeInjectionService.LastProcessId);
        Assert.Equal(_tempPayloadPath, _fakeInjectionService.LastDllPath);
    }
}
