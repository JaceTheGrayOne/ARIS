using Aris.Adapters.DllInjector;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.DllInjector;

public class ProcessResolverTests
{
    private readonly IProcessResolver _resolver = new ProcessResolver();

    [Fact]
    public void ResolveAndValidateTarget_BothNull_ThrowsValidationError()
    {
        var options = CreateValidOptions();

        var ex = Assert.Throws<ValidationError>(() =>
            _resolver.ResolveAndValidateTarget(null, null, options));

        Assert.Contains("Either ProcessId or ProcessName must be provided", ex.Message);
        Assert.Contains("Specify a target process", ex.RemediationHint);
    }

    [Fact]
    public void ResolveAndValidateTarget_BothEmpty_ThrowsValidationError()
    {
        var options = CreateValidOptions();

        var ex = Assert.Throws<ValidationError>(() =>
            _resolver.ResolveAndValidateTarget(null, "   ", options));

        Assert.Contains("Either ProcessId or ProcessName must be provided", ex.Message);
    }

    [Fact]
    public void ResolveAndValidateTarget_BothProvided_ThrowsValidationError()
    {
        var options = CreateValidOptions();

        var ex = Assert.Throws<ValidationError>(() =>
            _resolver.ResolveAndValidateTarget(1234, "notepad.exe", options));

        Assert.Contains("Provide either ProcessId or ProcessName, but not both", ex.Message);
        Assert.Contains("Use ProcessId for precise targeting", ex.RemediationHint);
    }

    [Fact]
    public void ResolveAndValidateTarget_InvalidProcessId_ThrowsValidationError()
    {
        var options = CreateValidOptions();

        var ex = Assert.Throws<ValidationError>(() =>
            _resolver.ResolveAndValidateTarget(999999, null, options));

        Assert.Contains("No running process with ID 999999 was found", ex.Message);
        Assert.Contains("Verify the process is running", ex.RemediationHint);
    }

    [Fact]
    public void ResolveAndValidateTarget_InvalidProcessName_ThrowsValidationError()
    {
        var options = CreateValidOptions();

        var ex = Assert.Throws<ValidationError>(() =>
            _resolver.ResolveAndValidateTarget(null, "NonExistentProcess12345.exe", options));

        Assert.Contains("No running process with name", ex.Message);
        Assert.Contains("Verify the process name is correct", ex.RemediationHint);
    }

    [Fact]
    public void ResolveAndValidateTarget_CurrentProcessId_WithDenylist_Succeeds()
    {
        var options = CreateValidOptions();
        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;

        var result = _resolver.ResolveAndValidateTarget(currentPid, null, options);

        Assert.Equal(currentPid, result);
    }

    [Fact]
    public void ResolveAndValidateTarget_CurrentProcessName_WithDenylist_IsDeterministic()
    {
        var options = CreateValidOptions();
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var currentPid = currentProcess.Id;

        var name = currentProcess.ProcessName;

        // Name-based resolution is strict: if multiple processes share the name, it must throw.
        // If the name is unique, it should resolve to the current process.
        var matches = System.Diagnostics.Process.GetProcessesByName(name);

        if (matches.Length > 1)
        {
            var ex = Assert.Throws<ValidationError>(() =>
                _resolver.ResolveAndValidateTarget(null, name, options));

            Assert.Contains("resolved to", ex.Message);
            Assert.Contains("running processes", ex.Message);
        }
        else
        {
            var result = _resolver.ResolveAndValidateTarget(null, name, options);
            Assert.Equal(currentPid, result);
        }
    }



    [Fact]
    public void ResolveAndValidateTarget_CurrentProcess_IsX64OrX86_Returns()
    {
        var options = new DllInjectorOptions
        {
            AllowedTargets = Array.Empty<string>(),
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe", "services.exe" }
        };

        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;

        var result = _resolver.ResolveAndValidateTarget(currentPid, null, options);

        Assert.Equal(currentPid, result);
    }

    private DllInjectorOptions CreateValidOptions()
    {
        return new DllInjectorOptions
        {
            AllowedTargets = Array.Empty<string>(),
            DeniedTargets = new[]
            {
                "csrss.exe",
                "smss.exe",
                "wininit.exe",
                "services.exe",
                "lsass.exe",
                "svchost.exe",
                "winlogon.exe"
            }
        };
    }
}
