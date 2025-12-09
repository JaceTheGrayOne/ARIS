using Aris.Adapters.DllInjector;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.DllInjector;

public class DllEjectCommandValidatorTests
{
    private readonly FakeProcessResolver _fakeResolver;

    public DllEjectCommandValidatorTests()
    {
        _fakeResolver = new FakeProcessResolver();
    }

    [Fact]
    public void ValidateAndResolveTarget_ValidCommand_ReturnsResolvedPid()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllEjectCommandValidator.ValidateAndResolveTarget(
            command, options, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_NullCommand_ThrowsArgumentNullException()
    {
        var options = CreateValidOptions();

        Assert.Throws<ArgumentNullException>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                null!, options, _fakeResolver));
    }

    [Fact]
    public void ValidateAndResolveTarget_NullOptions_ThrowsArgumentNullException()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        Assert.Throws<ArgumentNullException>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, null!, _fakeResolver));
    }

    [Fact]
    public void ValidateAndResolveTarget_NullProcessResolver_ThrowsArgumentNullException()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        var options = CreateValidOptions();

        Assert.Throws<ArgumentNullException>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, options, null!));
    }

    [Fact]
    public void ValidateAndResolveTarget_EmptyModuleName_ThrowsValidationError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = ""
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, options, _fakeResolver));

        Assert.Contains("ModuleName is required", ex.Message);
        Assert.Contains("module name to eject", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_WhitespaceModuleName_ThrowsValidationError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "   "
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, options, _fakeResolver));

        Assert.Contains("ModuleName is required", ex.Message);
    }

    [Fact]
    public void ValidateAndResolveTarget_ValidModuleName_Succeeds()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "aris_payload.dll"
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllEjectCommandValidator.ValidateAndResolveTarget(
            command, options, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_ZeroTimeout_ThrowsValidationError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            TimeoutSeconds = 0
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, options, _fakeResolver));

        Assert.Contains("TimeoutSeconds must be greater than zero", ex.Message);
        Assert.Contains("positive value", ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_NegativeTimeout_ThrowsValidationError()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            TimeoutSeconds = -10
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var ex = Assert.Throws<ValidationError>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, options, _fakeResolver));

        Assert.Contains("TimeoutSeconds must be greater than zero", ex.Message);
    }

    [Fact]
    public void ValidateAndResolveTarget_PositiveTimeout_Succeeds()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            TimeoutSeconds = 60
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllEjectCommandValidator.ValidateAndResolveTarget(
            command, options, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_NullTimeout_Succeeds()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll",
            TimeoutSeconds = null
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(1234);

        var result = DllEjectCommandValidator.ValidateAndResolveTarget(
            command, options, _fakeResolver);

        Assert.Equal(1234, result);
    }

    [Fact]
    public void ValidateAndResolveTarget_ProcessResolverThrows_BubblesException()
    {
        var command = new DllEjectCommand
        {
            ProcessId = 1234,
            ModuleName = "payload.dll"
        };

        var options = CreateValidOptions();
        var expectedError = new ValidationError("Process denied by policy")
        {
            RemediationHint = "Choose a different process"
        };
        _fakeResolver.SetError(expectedError);

        var ex = Assert.Throws<ValidationError>(() =>
            DllEjectCommandValidator.ValidateAndResolveTarget(
                command, options, _fakeResolver));

        Assert.Equal(expectedError.Message, ex.Message);
        Assert.Equal(expectedError.RemediationHint, ex.RemediationHint);
    }

    [Fact]
    public void ValidateAndResolveTarget_ByProcessName_Succeeds()
    {
        var command = new DllEjectCommand
        {
            ProcessName = "testapp.exe",
            ModuleName = "payload.dll"
        };

        var options = CreateValidOptions();
        _fakeResolver.SetResult(5678);

        var result = DllEjectCommandValidator.ValidateAndResolveTarget(
            command, options, _fakeResolver);

        Assert.Equal(5678, result);
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
