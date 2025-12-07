using Aris.Adapters.Retoc;
using Aris.Core.Errors;
using Aris.Core.Retoc;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.Retoc;

public class RetocCommandBuilderTests
{
    private readonly RetocOptions _defaultOptions = new RetocOptions
    {
        DefaultCompressionFormat = "Zlib",
        DefaultCompressionLevel = 6,
        AllowedAdditionalArgs = new List<string> { "--verbose", "--no-warnings" }
    };

    [Fact]
    public void Build_ValidCommand_ReturnsCorrectArguments()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Mode = RetocMode.PakToIoStore,
            GameVersion = "1.0",
            UEVersion = "5.3"
        };

        var (execPath, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Equal("C:\\tools\\retoc.exe", execPath);
        Assert.Contains("convert --to-iostore", args);
        Assert.Contains("--input \"C:\\input\\test.pak\"", args);
        Assert.Contains("--output \"C:\\output\\test.utoc\"", args);
        Assert.Contains("--game-version \"1.0\"", args);
        Assert.Contains("--ue-version \"5.3\"", args);
        Assert.Contains("--compression \"Zlib\"", args);
        Assert.Contains("--compression-level 6", args);
    }

    [Fact]
    public void Build_MissingInputPath_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack
        };

        var ex = Assert.Throws<ValidationError>(() =>
            RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe"));

        Assert.Contains("InputPath", ex.FieldName);
    }

    [Fact]
    public void Build_MissingOutputPath_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "",
            Mode = RetocMode.Repack
        };

        var ex = Assert.Throws<ValidationError>(() =>
            RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe"));

        Assert.Contains("OutputPath", ex.FieldName);
    }

    [Fact]
    public void Build_RelativeInputPath_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "relative\\path\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack
        };

        var ex = Assert.Throws<ValidationError>(() =>
            RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe"));

        Assert.Contains("absolute path", ex.Message);
    }

    [Fact]
    public void Build_DisallowedAdditionalArg_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            AdditionalArgs = new List<string> { "--dangerous-flag" }
        };

        var ex = Assert.Throws<ValidationError>(() =>
            RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe"));

        Assert.Contains("not in the allowlist", ex.Message);
        Assert.Contains("--dangerous-flag", ex.InvalidValue);
    }

    [Fact]
    public void Build_AllowedAdditionalArg_IncludedInCommand()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            AdditionalArgs = new List<string> { "--verbose" }
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--verbose", args);
    }

    [Fact]
    public void Build_FilterWithPathTraversal_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            IncludeFilters = new List<string> { "../../etc/passwd" }
        };

        var ex = Assert.Throws<ValidationError>(() =>
            RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe"));

        Assert.Contains("path traversal", ex.Message);
    }

    [Fact]
    public void Build_ValidFilters_IncludedInCommand()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            IncludeFilters = new List<string> { "*.uasset", "Content/*" },
            ExcludeFilters = new List<string> { "*.tmp" }
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--include \"*.uasset\"", args);
        Assert.Contains("--include \"Content/*\"", args);
        Assert.Contains("--exclude \"*.tmp\"", args);
    }

    [Theory]
    [InlineData(RetocMode.PakToIoStore, "convert --to-iostore")]
    [InlineData(RetocMode.IoStoreToPak, "convert --to-pak")]
    [InlineData(RetocMode.Repack, "repack")]
    [InlineData(RetocMode.Validate, "validate")]
    public void Build_DifferentModes_MapsToCorrectSubcommand(RetocMode mode, string expectedSubcommand)
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = mode
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.StartsWith(expectedSubcommand, args);
    }

    [Fact]
    public void Build_WithMountKeys_IncludesAesKeyArgs()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Mode = RetocMode.PakToIoStore,
            MountKeys = new List<string> { "key1", "key2" }
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--aes-key \"key1\"", args);
        Assert.Contains("--aes-key \"key2\"", args);
    }

    [Fact]
    public void Build_WithCompressionOptions_IncludesCompressionArgs()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack,
            CompressionFormat = "Oodle",
            CompressionLevel = 9,
            CompressionBlockSize = 65536
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--compression \"Oodle\"", args);
        Assert.Contains("--compression-level 9", args);
        Assert.Contains("--block-size 65536", args);
    }
}
