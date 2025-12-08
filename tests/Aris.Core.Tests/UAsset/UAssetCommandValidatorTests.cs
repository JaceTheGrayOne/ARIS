using Aris.Adapters.UAsset;
using Aris.Core.Errors;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.UAsset;

public class UAssetCommandValidatorTests : IDisposable
{
    private readonly UAssetOptions _options;
    private readonly string _tempDirectory;
    private readonly string _testInputJsonPath;
    private readonly string _testInputAssetPath;
    private readonly string _testOutputAssetPath;
    private readonly string _testOutputJsonPath;

    public UAssetCommandValidatorTests()
    {
        _options = new UAssetOptions
        {
            DefaultUEVersion = "5.3",
            DefaultSchemaVersion = "1.0",
            MaxAssetSizeBytes = 1024 * 1024, // 1 MB for tests
            DefaultTimeoutSeconds = 300
        };

        _tempDirectory = Path.Combine(Path.GetTempPath(), "aris-validator-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _testInputJsonPath = Path.Combine(_tempDirectory, "test.json");
        _testInputAssetPath = Path.Combine(_tempDirectory, "test.uasset");
        _testOutputAssetPath = Path.Combine(_tempDirectory, "output.uasset");
        _testOutputJsonPath = Path.Combine(_tempDirectory, "output.json");

        File.WriteAllText(_testInputJsonPath, "{ \"test\": \"data\" }");
        File.WriteAllText(_testInputAssetPath, "fake asset content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region SerializeCommand Validation Tests

    [Fact]
    public void ValidateSerializeCommand_ValidCommand_DoesNotThrow()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = _testInputJsonPath,
            OutputAssetPath = _testOutputAssetPath,
            UEVersion = "5.3",
            SchemaVersion = "1.0"
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateSerializeCommand_EmptyInputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = "",
            OutputAssetPath = _testOutputAssetPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("InputJsonPath", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateSerializeCommand_WhitespaceInputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = "   ",
            OutputAssetPath = _testOutputAssetPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("InputJsonPath", ex.FieldName);
    }

    [Fact]
    public void ValidateSerializeCommand_EmptyOutputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = _testInputJsonPath,
            OutputAssetPath = ""
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("OutputAssetPath", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateSerializeCommand_RelativeInputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = "relative/path/test.json",
            OutputAssetPath = _testOutputAssetPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("InputJsonPath", ex.FieldName);
        Assert.Contains("absolute", ex.Message);
    }

    [Fact]
    public void ValidateSerializeCommand_RelativeOutputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = _testInputJsonPath,
            OutputAssetPath = "relative/output.uasset"
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("OutputAssetPath", ex.FieldName);
        Assert.Contains("absolute", ex.Message);
    }

    [Fact]
    public void ValidateSerializeCommand_NonExistentInputFile_ThrowsValidationError()
    {
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = nonExistentPath,
            OutputAssetPath = _testOutputAssetPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("InputJsonPath", ex.FieldName);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void ValidateSerializeCommand_InputFileExceedsMaxSize_ThrowsValidationError()
    {
        var largePath = Path.Combine(_tempDirectory, "large.json");
        File.WriteAllText(largePath, new string('x', (int)_options.MaxAssetSizeBytes + 1000));

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = largePath,
            OutputAssetPath = _testOutputAssetPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("InputJsonPath", ex.FieldName);
        Assert.Contains("exceeds maximum size", ex.Message);
        Assert.NotNull(ex.RemediationHint);
        Assert.Contains("MaxAssetSizeBytes", ex.RemediationHint);
    }

    [Fact]
    public void ValidateSerializeCommand_InvalidOutputDirectory_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = _testInputJsonPath,
            OutputAssetPath = "C:" // Invalid - no directory component
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("OutputAssetPath", ex.FieldName);
    }

    #endregion

    #region DeserializeCommand Validation Tests

    [Fact]
    public void ValidateDeserializeCommand_ValidCommand_DoesNotThrow()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = _testInputAssetPath,
            OutputJsonPath = _testOutputJsonPath,
            UEVersion = "5.3",
            SchemaVersion = "1.0"
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDeserializeCommand_EmptyInputPath_ThrowsValidationError()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = "",
            OutputJsonPath = _testOutputJsonPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateDeserializeCommand_EmptyOutputPath_ThrowsValidationError()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = _testInputAssetPath,
            OutputJsonPath = ""
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("OutputJsonPath", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateDeserializeCommand_RelativeInputPath_ThrowsValidationError()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = "relative/path/test.uasset",
            OutputJsonPath = _testOutputJsonPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("absolute", ex.Message);
    }

    [Fact]
    public void ValidateDeserializeCommand_RelativeOutputPath_ThrowsValidationError()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = _testInputAssetPath,
            OutputJsonPath = "relative/output.json"
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("OutputJsonPath", ex.FieldName);
        Assert.Contains("absolute", ex.Message);
    }

    [Fact]
    public void ValidateDeserializeCommand_NonExistentInputFile_ThrowsValidationError()
    {
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.uasset");

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = nonExistentPath,
            OutputJsonPath = _testOutputJsonPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void ValidateDeserializeCommand_InputFileExceedsMaxSize_ThrowsValidationError()
    {
        var largePath = Path.Combine(_tempDirectory, "large.uasset");
        File.WriteAllText(largePath, new string('x', (int)_options.MaxAssetSizeBytes + 1000));

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = largePath,
            OutputJsonPath = _testOutputJsonPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("exceeds maximum size", ex.Message);
        Assert.NotNull(ex.RemediationHint);
    }

    [Fact]
    public void ValidateDeserializeCommand_InvalidOutputDirectory_ThrowsValidationError()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = _testInputAssetPath,
            OutputJsonPath = "C:"
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Equal("OutputJsonPath", ex.FieldName);
    }

    #endregion

    #region InspectCommand Validation Tests

    [Fact]
    public void ValidateInspectCommand_ValidCommand_DoesNotThrow()
    {
        var command = new UAssetInspectCommand
        {
            InputAssetPath = _testInputAssetPath,
            Fields = new[] { "exports", "imports" }
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateInspectCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateInspectCommand_EmptyFieldsList_DoesNotThrow()
    {
        var command = new UAssetInspectCommand
        {
            InputAssetPath = _testInputAssetPath,
            Fields = Array.Empty<string>()
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateInspectCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateInspectCommand_EmptyInputPath_ThrowsValidationError()
    {
        var command = new UAssetInspectCommand
        {
            InputAssetPath = ""
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateInspectCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("required", ex.Message);
    }

    [Fact]
    public void ValidateInspectCommand_RelativeInputPath_ThrowsValidationError()
    {
        var command = new UAssetInspectCommand
        {
            InputAssetPath = "relative/path/test.uasset"
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateInspectCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("absolute", ex.Message);
    }

    [Fact]
    public void ValidateInspectCommand_NonExistentInputFile_ThrowsValidationError()
    {
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.uasset");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = nonExistentPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateInspectCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void ValidateInspectCommand_InputFileExceedsMaxSize_ThrowsValidationError()
    {
        var largePath = Path.Combine(_tempDirectory, "large.uasset");
        File.WriteAllText(largePath, new string('x', (int)_options.MaxAssetSizeBytes + 1000));

        var command = new UAssetInspectCommand
        {
            InputAssetPath = largePath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateInspectCommand(command, _options));

        Assert.Equal("InputAssetPath", ex.FieldName);
        Assert.Contains("exceeds maximum size", ex.Message);
        Assert.NotNull(ex.RemediationHint);
    }

    #endregion

    #region Path Normalization Tests

    [Fact]
    public void ValidateSerializeCommand_WindowsPathWithForwardSlashes_Normalizes()
    {
        var inputPath = _testInputJsonPath.Replace('\\', '/');
        var outputPath = _testOutputAssetPath.Replace('\\', '/');

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputPath,
            OutputAssetPath = outputPath
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDeserializeCommand_WindowsPathWithForwardSlashes_Normalizes()
    {
        var inputPath = _testInputAssetPath.Replace('\\', '/');
        var outputPath = _testOutputJsonPath.Replace('\\', '/');

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = inputPath,
            OutputJsonPath = outputPath
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Null(exception);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValidateSerializeCommand_InputFileSizeAtLimit_DoesNotThrow()
    {
        var atLimitPath = Path.Combine(_tempDirectory, "atlimit.json");
        File.WriteAllText(atLimitPath, new string('x', (int)_options.MaxAssetSizeBytes));

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = atLimitPath,
            OutputAssetPath = _testOutputAssetPath
        };

        var exception = Record.Exception(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDeserializeCommand_InputFileSizeOneByteOverLimit_ThrowsValidationError()
    {
        var overLimitPath = Path.Combine(_tempDirectory, "overlimit.uasset");
        File.WriteAllText(overLimitPath, new string('x', (int)_options.MaxAssetSizeBytes + 1));

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = overLimitPath,
            OutputJsonPath = _testOutputJsonPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateDeserializeCommand(command, _options));

        Assert.Contains("exceeds maximum size", ex.Message);
    }

    [Fact]
    public void ValidateSerializeCommand_NullInputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = null!,
            OutputAssetPath = _testOutputAssetPath
        };

        var ex = Assert.Throws<ValidationError>(() =>
            UAssetCommandValidator.ValidateSerializeCommand(command, _options));

        Assert.Equal("InputJsonPath", ex.FieldName);
    }

    #endregion
}
