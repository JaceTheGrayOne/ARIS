using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.DllInjector;

public class DllInjectorOptionsValidatorTests
{
    private readonly DllInjectorOptionsValidator _validator = new DllInjectorOptionsValidator();

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var options = new DllInjectorOptions
        {
            DefaultTimeoutSeconds = 60,
            RequireElevation = true,
            AllowedTargets = Array.Empty<string>(),
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe", "services.exe", "wininit.exe" },
            AllowedMethods = new[] { "CreateRemoteThread", "ApcQueue", "ManualMap" },
            MaxLogBytes = 5 * 1024 * 1024
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_DefaultConstructedOptions_ReturnsSuccess()
    {
        var options = new DllInjectorOptions();

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ZeroTimeout_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DefaultTimeoutSeconds = 0
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultTimeoutSeconds must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeTimeout_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DefaultTimeoutSeconds = -10
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultTimeoutSeconds must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_ZeroMaxLogBytes_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            MaxLogBytes = 0
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxLogBytes must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeMaxLogBytes_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            MaxLogBytes = -1
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxLogBytes must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_ExcessiveMaxLogBytes_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            MaxLogBytes = 200L * 1024 * 1024 // 200 MB, exceeds 100 MB limit
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxLogBytes must not exceed", result.FailureMessage);
    }

    [Fact]
    public void Validate_MaxLogBytesAtBoundary_ReturnsSuccess()
    {
        var options = new DllInjectorOptions
        {
            MaxLogBytes = 100L * 1024 * 1024 // Exactly 100 MB
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_NullAllowedMethods_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            AllowedMethods = null!
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowedMethods must contain at least one method", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyAllowedMethods_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            AllowedMethods = Array.Empty<string>()
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowedMethods must contain at least one method", result.FailureMessage);
    }

    [Fact]
    public void Validate_AllowedMethodsWithWhitespace_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            AllowedMethods = new[] { "CreateRemoteThread", "   " }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowedMethods contains empty or whitespace-only entry", result.FailureMessage);
    }

    [Fact]
    public void Validate_AllowedMethodsWithInvalidMethod_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            AllowedMethods = new[] { "CreateRemoteThread", "InvalidMethod" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowedMethods contains invalid method 'InvalidMethod'", result.FailureMessage);
        Assert.Contains("Valid methods:", result.FailureMessage);
    }

    [Fact]
    public void Validate_AllowedMethodsWithValidNames_ReturnsSuccess()
    {
        var options = new DllInjectorOptions
        {
            AllowedMethods = new[] { "CreateRemoteThread", "ApcQueue", "ManualMap" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_NullDeniedTargets_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = null!
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DeniedTargets must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyDeniedTargets_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = Array.Empty<string>()
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DeniedTargets must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Validate_DeniedTargetsMissingCsrss_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = new[] { "smss.exe", "lsass.exe", "services.exe" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DeniedTargets must include critical process 'csrss.exe'", result.FailureMessage);
    }

    [Fact]
    public void Validate_DeniedTargetsMissingSmss_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = new[] { "csrss.exe", "lsass.exe", "services.exe" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DeniedTargets must include critical process 'smss.exe'", result.FailureMessage);
    }

    [Fact]
    public void Validate_DeniedTargetsMissingLsass_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "services.exe" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DeniedTargets must include critical process 'lsass.exe'", result.FailureMessage);
    }

    [Fact]
    public void Validate_DeniedTargetsMissingServices_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = new[] { "csrss.exe", "smss.exe", "lsass.exe" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DeniedTargets must include critical process 'services.exe'", result.FailureMessage);
    }

    [Fact]
    public void Validate_DeniedTargetsWithRequiredProcesses_CaseInsensitive_ReturnsSuccess()
    {
        var options = new DllInjectorOptions
        {
            DeniedTargets = new[] { "CSRSS.EXE", "smss.exe", "LSASS.exe", "Services.EXE" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_AllowedTargetsWithEmptyEntry_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            AllowedTargets = new[] { "Game.exe", "" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowedTargets contains empty or whitespace-only entry", result.FailureMessage);
    }

    [Fact]
    public void Validate_AllowedTargetsWithWhitespace_ReturnsFailure()
    {
        var options = new DllInjectorOptions
        {
            AllowedTargets = new[] { "Game.exe", "   " }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AllowedTargets contains empty or whitespace-only entry", result.FailureMessage);
    }

    [Fact]
    public void Validate_AllowedTargetsWithValidEntries_ReturnsSuccess()
    {
        var options = new DllInjectorOptions
        {
            AllowedTargets = new[] { "Game.exe", "App*.exe", "*Test.dll" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllInFailureMessage()
    {
        var options = new DllInjectorOptions
        {
            DefaultTimeoutSeconds = -1,
            MaxLogBytes = 0,
            AllowedMethods = Array.Empty<string>(),
            DeniedTargets = Array.Empty<string>()
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultTimeoutSeconds", result.FailureMessage);
        Assert.Contains("MaxLogBytes", result.FailureMessage);
        Assert.Contains("AllowedMethods", result.FailureMessage);
        Assert.Contains("DeniedTargets", result.FailureMessage);
    }
}
