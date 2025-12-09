using Aris.Adapters.DllInjector;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.DllInjector;

public class DllInjectCommandValidatorTests : IDisposable
{
    private readonly string _tempWorkspaceRoot;
    private readonly string _tempPayloadPath;
    private readonly FakeProcessResolver _fakeResolver;

    public DllInjectCommandValidatorTests()
    {
        _tempWorkspaceRoot = Path.Combine(Path.GetTempPath(), "aris-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspaceRoot);

        var payloadsDir = Path.Combine(_tempWorkspaceRoot, "input", "payloads");
        Directory.CreateDirectory(payloadsDir);

        _tempPayloadPath = Path.Combine(payloadsDir, "test_payload.dll");
        File.WriteAllText(_tempPayloadPath, "fake dll content");

        _fakeResolver = new FakeProcessResolver();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempWorkspaceRoot))
            {
                Directory.Delete(_tempWorkspaceRoot, recursive: true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void ValidateAndResolveTarget_ValidCommand_ReturnsResolvedPid()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllInjectCommandValidator.ValidateAndResolveTarget(
            command, options, _tempWorkspaceRoot, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_NullCommand_ThrowsArgumentNullException()
    {
        var options = CreateValidOptions();

        Assert.Throws<ArgumentNullException>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                null!, options, _tempWorkspaceRoot, _fakeResolver));
    }

    [Fact]
    public void ValidateAndResolveTarget_NullOptions_ThrowsArgumentNullException()
    {
        var command = CreateValidCommand();

        Assert.Throws<ArgumentNullException>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, null!, _tempWorkspaceRoot, _fakeResolver));
    }

    [Fact]
    public void ValidateAndResolveTarget_NullProcessResolver_ThrowsArgumentNullException()
    {
        var command = CreateValidCommand();
        var options = CreateValidOptions();

        Assert.Throws<ArgumentNullException>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, null!));
    }

    [Fact]
    public void ValidateAndResolveTarget_EmptyDllPath_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = "",
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("DllPath is required", ex.Message);
        Assert.Contains("absolute path", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_WhitespaceDllPath_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = "   ",
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("DllPath is required", ex.Message);
    }

    [Fact]
    public void ValidateAndResolveTarget_RelativeDllPath_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = "relative/path/payload.dll",
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("DllPath must be an absolute path", ex.Message);
        Assert.Contains("fully-qualified", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_NonExistentDllPath_ThrowsValidationError()
    {
        var nonExistentPath = Path.Combine(_tempWorkspaceRoot, "input", "payloads", "nonexistent.dll");
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = nonExistentPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("Payload DLL not found", ex.Message);
        Assert.Contains("file exists", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_WrongExtension_ThrowsValidationError()
    {
        var wrongExtPath = Path.Combine(_tempWorkspaceRoot, "input", "payloads", "payload.exe");
        File.WriteAllText(wrongExtPath, "fake");

        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = wrongExtPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("Payload must be a .dll file", ex.Message);
        Assert.Contains("valid DLL", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_DllOutsideWorkspace_ThrowsValidationError()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), "outside.dll");
        File.WriteAllText(outsidePath, "fake");

        try
        {
            var command = new DllInjectCommand
            {
                ProcessId = 1234,
                DllPath = outsidePath,
                Method = DllInjectionMethod.CreateRemoteThread
            };

            var options = CreateValidOptions();
            _fakeResolver.SetResult(1234);

            var ex = Assert.Throws<ValidationError>(() =>
                DllInjectCommandValidator.ValidateAndResolveTarget(
                    command, options, _tempWorkspaceRoot, _fakeResolver));

            Assert.Contains("Payload DLL must be under workspace payloads directory", ex.Message);
            Assert.Contains("Place payload DLLs", ex.RemediationHint);
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }

    [Fact]
    public void ValidateAndResolveTarget_DllInDependenciesPayloads_Succeeds()
    {
        var depsPayloadsDir = Path.Combine(_tempWorkspaceRoot, "dependencies", "payloads");
        Directory.CreateDirectory(depsPayloadsDir);
        var depsPayload = Path.Combine(depsPayloadsDir, "dep_payload.dll");
        File.WriteAllText(depsPayload, "fake");

        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = depsPayload,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllInjectCommandValidator.ValidateAndResolveTarget(
            command, options, _tempWorkspaceRoot, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_DisallowedMethod_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.ManualMap
        };

        var options = new DllInjectorOptions
        {
            AllowedMethods = new[] { "CreateRemoteThread", "ApcQueue" },
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe", "services.exe" }
        };
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("Injection method 'ManualMap' is not allowed", ex.Message);
        Assert.Contains("AllowedMethods", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_AllowedMethod_Succeeds()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllInjectCommandValidator.ValidateAndResolveTarget(
            command, options, _tempWorkspaceRoot, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_EmptyArgument_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            Arguments = new[] { "valid", "" }
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("Arguments must not contain empty or whitespace-only entries", ex.Message);
        Assert.Contains("meaningful value", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_WhitespaceArgument_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            Arguments = new[] { "valid", "   " }
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("Arguments must not contain empty or whitespace-only entries", ex.Message);
    }

    [Fact]
    public void ValidateAndResolveTarget_ValidArguments_Succeeds()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            Arguments = new[] { "arg1", "arg2", "arg3" }
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllInjectCommandValidator.ValidateAndResolveTarget(
            command, options, _tempWorkspaceRoot, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_ZeroTimeout_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            TimeoutSeconds = 0
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("TimeoutSeconds must be greater than zero", ex.Message);
        Assert.Contains("positive value", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_NegativeTimeout_ThrowsValidationError()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            TimeoutSeconds = -10
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllInjectCommandValidator.ValidateAndResolveTarget(
                command, options, _tempWorkspaceRoot, _fakeResolver));

        Assert.Contains("TimeoutSeconds must be greater than zero", ex.Message);
    }

    [Fact]
    public void ValidateAndResolveTarget_PositiveTimeout_Succeeds()
    {
        var command = new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread,
            TimeoutSeconds = 60
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllInjectCommandValidator.ValidateAndResolveTarget(
            command, options, _tempWorkspaceRoot, _fakeResolver);

        Assert.Equal(1234, result);
    }

    private DllInjectCommand CreateValidCommand()
    {
        return new DllInjectCommand
        {
            ProcessId = 1234,
            DllPath = _tempPayloadPath,
            Method = DllInjectionMethod.CreateRemoteThread
        };
    }

    private DllInjectorOptions CreateValidOptions()
    {
        return new DllInjectorOptions
        {
            AllowedMethods = new[] { "CreateRemoteThread", "ApcQueue", "ManualMap" },
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe", "services.exe" }
        };
    }

    private class FakeProcessResolver : IProcessResolver
    {
        private int _result = 1234;
        private ValidationError? _error;

        public void SetResult(int pid)
        {
            _result = pid;
            _error = null;
        }

        public void SetError(ValidationError error)
        {
            _error = error;
        }

        public int ResolveAndValidateTarget(int? processId, string? processName, DllInjectorOptions options)
        {
            if (_error != null)
            {
                throw _error;
            }

            return _result;
        }
    }
}
