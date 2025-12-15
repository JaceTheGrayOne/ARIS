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
        // PakToIoStore maps to "to-zen" command with positional arguments
        Assert.Contains("to-zen", args);
        Assert.Contains("C:\\input\\test.pak", args);
        Assert.Contains("C:\\output\\test.utoc", args);
    }

    [Fact]
    public void Build_MissingInputPath_ThrowsValidationError()
    {
        var command = new RetocCommand
        {
            InputPath = "",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.PakToIoStore
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
            Mode = RetocMode.PakToIoStore
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
            Mode = RetocMode.PakToIoStore
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
            Mode = RetocMode.PakToIoStore,
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
            Mode = RetocMode.PakToIoStore,
            AdditionalArgs = new List<string> { "--verbose" }
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--verbose", args);
    }



    [Theory]
    [InlineData(RetocMode.PakToIoStore, "to-zen")]
    [InlineData(RetocMode.IoStoreToPak, "to-legacy")]
    [InlineData(RetocMode.Validate, "verify")]
    public void Build_DifferentModes_MapsToCorrectSubcommand(RetocMode mode, string expectedSubcommand)
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = mode
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains(expectedSubcommand, args);
    }

    [Fact]
    public void Build_RepackMode_ThrowsValidationError()
    {
        // RetocMode.Repack is not supported by the real retoc CLI
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.Repack
        };

        var ex = Assert.Throws<ValidationError>(() =>
            RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe"));

        Assert.Contains("Repack", ex.Message);
    }

    [Fact]
    public void Build_WithMountKeys_IncludesAesKeyArg()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Mode = RetocMode.PakToIoStore,
            MountKeys = new List<string> { "0x1234567890ABCDEF" }
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        // AES key should come before the subcommand
        Assert.Contains("--aes-key", args);
        Assert.Contains("0x1234567890ABCDEF", args);
    }

    [Fact]
    public void Build_WithAesKeyProperty_IncludesAesKeyArg()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.pak",
            OutputPath = "C:\\output\\test.utoc",
            Mode = RetocMode.PakToIoStore,
            AesKey = "0x1234567890ABCDEF"
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--aes-key", args);
        Assert.Contains("0x1234567890ABCDEF", args);
    }

    [Fact]
    public void Build_WithContainerHeaderOverride_IncludesOverrideArg()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.utoc",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.IoStoreToPak,
            ContainerHeaderVersion = RetocContainerHeaderVersion.Initial
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--override-container-header-version", args);
        Assert.Contains("Initial", args);
    }

    [Fact]
    public void Build_WithTocVersionOverride_IncludesOverrideArg()
    {
        var command = new RetocCommand
        {
            InputPath = "C:\\input\\test.utoc",
            OutputPath = "C:\\output\\test.pak",
            Mode = RetocMode.IoStoreToPak,
            TocVersion = RetocTocVersion.DirectoryIndex
        };

        var (_, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Contains("--override-toc-version", args);
        Assert.Contains("DirectoryIndex", args);
    }

    [Fact]
    public void Build_SimplePack_ProducesCorrectCommandLine()
    {
        // Simulates: Pack (Legacy → Zen) with version
        var command = new RetocCommand
        {
            CommandType = RetocCommandType.ToZen,
            InputPath = @"G:\Grounded\Modding\ModFolder",
            OutputPath = @"G:\Grounded\Modding\AwesomeMod\AwesomeMod.utoc",
            Mode = RetocMode.PakToIoStore,
            Version = "UE5_6"
        };

        var (execPath, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Equal("C:\\tools\\retoc.exe", execPath);

        // Expected: to-zen --version UE5_6 <input> <output>
        Assert.Contains("--version", args);
        Assert.Contains("UE5_6", args);
        Assert.Contains("to-zen", args);
        Assert.Contains(@"G:\Grounded\Modding\ModFolder", args);
        Assert.Contains(@"G:\Grounded\Modding\AwesomeMod\AwesomeMod.utoc", args);

        // Verify order: to-zen comes before --version (subcommand option, not global)
        var toZenIndex = args.IndexOf("to-zen");
        var versionIndex = args.IndexOf("--version");
        Assert.True(toZenIndex < versionIndex, "Subcommand should come before --version flag");
    }

    [Fact]
    public void Build_SimpleUnpack_ProducesCorrectCommandLine()
    {
        // Simulates: Unpack (Zen → Legacy) without version
        var command = new RetocCommand
        {
            CommandType = RetocCommandType.ToLegacy,
            InputPath = @"E:\SteamLibrary\steamapps\common\Grounded2\Augusta\Content\Paks",
            OutputPath = @"G:\Grounded\Modding\Grounded 2_Extracted",
            Mode = RetocMode.IoStoreToPak
        };

        var (execPath, args) = RetocCommandBuilder.Build(command, _defaultOptions, "C:\\tools\\retoc.exe");

        Assert.Equal("C:\\tools\\retoc.exe", execPath);

        // Expected: to-legacy <input> <output>
        Assert.Contains("to-legacy", args);
        Assert.Contains(@"E:\SteamLibrary\steamapps\common\Grounded2\Augusta\Content\Paks", args);
        Assert.Contains(@"G:\Grounded\Modding\Grounded 2_Extracted", args);

        // Should NOT contain --version
        Assert.DoesNotContain("--version", args);
    }

}
