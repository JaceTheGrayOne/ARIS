using Aris.Adapters.UwpDumper;
using Aris.Core.Errors;
using Aris.Core.UwpDumper;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.UwpDumper;

public class UwpDumpCommandValidatorTests : IDisposable
{
    private readonly UwpDumperOptions _options;
    private readonly string _tempTestDir;
    private readonly string _tempOutputPath;

    public UwpDumpCommandValidatorTests()
    {
        _options = new UwpDumperOptions
        {
            DefaultTimeoutSeconds = 300,
            RequireElevation = true,
            AllowedModes = new[] { "FullDump", "MetadataOnly", "ValidateOnly" },
            MaxLogBytes = 5 * 1024 * 1024
        };

        _tempTestDir = Path.Combine(Path.GetTempPath(), "aris-uwpdumper-validator-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);

        _tempOutputPath = Path.Combine(_tempTestDir, "output", "uwp");
        Directory.CreateDirectory(_tempOutputPath);
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

    [Fact]
    public void ValidateDumpCommand_ValidCommand_DoesNotThrow()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump,
            OperationId = "test-op-123"
        };

        var exception = Record.Exception(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDumpCommand_EmptyPackageFamilyName_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Equal("PackageFamilyName", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateDumpCommand_WhitespacePackageFamilyName_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "   ",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Equal("PackageFamilyName", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateDumpCommand_EmptyOutputPath_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = "",
            Mode = UwpDumpMode.FullDump
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Equal("OutputPath", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateDumpCommand_RelativeOutputPath_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = "relative/path/output",
            Mode = UwpDumpMode.FullDump
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Equal("OutputPath", ex.FieldName);
        Assert.Contains("absolute", ex.Message);
    }

    [Fact]
    public void ValidateDumpCommand_AbsoluteOutputPath_DoesNotThrow()
    {
        // After workspace removal, any absolute output path is valid
        var outsidePath = Path.Combine(Path.GetTempPath(), "output-anywhere", "uwp");
        Directory.CreateDirectory(outsidePath);

        try
        {
            var command = new UwpDumpCommand
            {
                PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                OutputPath = outsidePath,
                Mode = UwpDumpMode.FullDump
            };

            var exception = Record.Exception(() =>
                UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

            Assert.Null(exception);
        }
        finally
        {
            if (Directory.Exists(outsidePath))
            {
                Directory.Delete(outsidePath, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidateDumpCommand_PathNormalizationWithForwardSlashes_Validates()
    {
        var pathWithForwardSlashes = _tempOutputPath.Replace('\\', '/');

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = pathWithForwardSlashes,
            Mode = UwpDumpMode.FullDump
        };

        var exception = Record.Exception(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDumpCommand_ModeNotInAllowedModes_ThrowsValidationError()
    {
        var restrictedOptions = new UwpDumperOptions
        {
            AllowedModes = new[] { "MetadataOnly" }
        };

        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, restrictedOptions));

        Assert.Equal("Mode", ex.FieldName);
        Assert.Contains("not allowed", ex.Message);
        Assert.Contains("FullDump", ex.Message);
        Assert.NotNull(ex.RemediationHint);
        Assert.Contains("AllowedModes", ex.RemediationHint);
    }

    [Fact]
    public void ValidateDumpCommand_NegativeTimeout_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump,
            TimeoutSeconds = -10
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Equal("TimeoutSeconds", ex.FieldName);
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void ValidateDumpCommand_ZeroTimeout_ThrowsValidationError()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump,
            TimeoutSeconds = 0
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Equal("TimeoutSeconds", ex.FieldName);
        Assert.Contains("greater than 0", ex.Message);
    }

    [Fact]
    public void ValidateDumpCommand_PositiveTimeout_DoesNotThrow()
    {
        var command = new UwpDumpCommand
        {
            PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
            OutputPath = _tempOutputPath,
            Mode = UwpDumpMode.FullDump,
            TimeoutSeconds = 600
        };

        var exception = Record.Exception(() =>
            UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDumpCommand_AllModesAllowed_ValidatesAllModes()
    {
        var modes = new[] { UwpDumpMode.FullDump, UwpDumpMode.MetadataOnly, UwpDumpMode.ValidateOnly };

        foreach (var mode in modes)
        {
            var command = new UwpDumpCommand
            {
                PackageFamilyName = "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                OutputPath = _tempOutputPath,
                Mode = mode
            };

            var exception = Record.Exception(() =>
                UwpDumpCommandValidator.ValidateDumpCommand(command, _options));

            Assert.Null(exception);
        }
    }
}
