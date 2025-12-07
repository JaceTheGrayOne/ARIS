using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.Configuration;

public class RetocOptionsValidatorTests
{
    private readonly RetocOptionsValidator _validator = new RetocOptionsValidator();

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--verbose", "--no-warnings" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ZeroTimeout_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 0,
            MaxLogBytes = 5 * 1024 * 1024
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultTimeoutSeconds must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeTimeout_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = -10,
            MaxLogBytes = 5 * 1024 * 1024
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultTimeoutSeconds must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_ZeroMaxLogBytes_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 0
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxLogBytes must be greater than 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_ExcessiveMaxLogBytes_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 200 * 1024 * 1024 // 200 MB, exceeds 100 MB limit
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxLogBytes must not exceed", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithSemicolon_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--flag; rm -rf /" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsafe characters", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithBacktick_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "`echo bad`" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsafe characters", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithDoubleAmpersand_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--flag && echo bad" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsafe sequence", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithDoublePipe_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--flag || echo bad" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsafe sequence", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithPipe_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--flag | grep something" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsafe sequence", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithRedirect_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--flag > output.txt" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsafe sequence", result.FailureMessage);
    }

    [Fact]
    public void Validate_ArgWithUnbalancedQuotes_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "--flag \"value" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("unbalanced quotes", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyArg_ReturnsFailure()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            AllowedAdditionalArgs = new List<string> { "" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("empty or whitespace", result.FailureMessage);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllInFailureMessage()
    {
        var options = new RetocOptions
        {
            DefaultTimeoutSeconds = -1,
            MaxLogBytes = 0,
            AllowedAdditionalArgs = new List<string> { "--flag; bad" }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("DefaultTimeoutSeconds", result.FailureMessage);
        Assert.Contains("MaxLogBytes", result.FailureMessage);
        Assert.Contains("unsafe characters", result.FailureMessage);
    }
}
