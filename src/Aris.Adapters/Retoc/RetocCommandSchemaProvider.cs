using Aris.Contracts.Retoc;
using Aris.Core.Retoc;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Provides schema definitions for all supported Retoc commands.
/// Used to render dynamic UI in Advanced Mode.
/// This is manually maintained and must stay in sync with RetocCommandType enum.
/// </summary>
public static class RetocCommandSchemaProvider
{
    public static RetocCommandSchemaResponse GetSchema()
    {
        return new RetocCommandSchemaResponse
        {
            Commands = GetCommands(),
            GlobalOptions = GetGlobalOptions(),
            AllowlistedFlags = new[] { "--verbose" }
        };
    }

    private static RetocCommandDefinition[] GetCommands()
    {
        return new[]
        {
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.ToLegacy),
                DisplayName = "Unpack (Zen → Legacy)",
                Description = "Convert IoStore containers to editable legacy UAsset files",
                RequiredFields = new[] { "InputPath", "OutputPath" },
                OptionalFields = new[] { "AesKey", "ContainerHeaderVersion", "TocVersion" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.ToZen),
                DisplayName = "Pack (Legacy → Zen)",
                Description = "Build IoStore containers from modified legacy UAsset files",
                RequiredFields = new[] { "InputPath", "OutputPath", "EngineVersion" },
                OptionalFields = new[] { "AesKey", "ContainerHeaderVersion", "TocVersion" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.Manifest),
                DisplayName = "Extract Manifest",
                Description = "Extract manifest data from .utoc file",
                RequiredFields = new[] { "InputPath", "OutputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.Info),
                DisplayName = "Display Info",
                Description = "Display container information",
                RequiredFields = new[] { "InputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.List),
                DisplayName = "List Files",
                Description = "List files in .utoc directory index",
                RequiredFields = new[] { "InputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.Verify),
                DisplayName = "Verify Container",
                Description = "Validate IoStore container integrity",
                RequiredFields = new[] { "InputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.Unpack),
                DisplayName = "Unpack Chunks",
                Description = "Extract chunks (files) from .utoc",
                RequiredFields = new[] { "InputPath", "OutputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.UnpackRaw),
                DisplayName = "Unpack Raw Chunks",
                Description = "Extract raw chunks from container",
                RequiredFields = new[] { "InputPath", "OutputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.PackRaw),
                DisplayName = "Pack Raw Chunks",
                Description = "Pack directory of raw chunks into container",
                RequiredFields = new[] { "InputPath", "OutputPath" },
                OptionalFields = new[] { "AesKey", "ContainerHeaderVersion", "TocVersion" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.Get),
                DisplayName = "Get Chunk",
                Description = "Retrieve chunk by ID and write to file or stdout",
                RequiredFields = new[] { "InputPath", "ChunkId" },
                OptionalFields = new[] { "OutputPath", "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.DumpTest),
                DisplayName = "Dump Test",
                Description = "Execute dump test operation",
                RequiredFields = new[] { "InputPath" },
                OptionalFields = new[] { "AesKey" }
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.GenScriptObjects),
                DisplayName = "Generate Script Objects",
                Description = "Generate script objects global container from UE reflection data (.jmap)",
                RequiredFields = new[] { "InputPath", "OutputPath" },
                OptionalFields = Array.Empty<string>()
            },
            new RetocCommandDefinition
            {
                CommandType = nameof(RetocCommandType.PrintScriptObjects),
                DisplayName = "Print Script Objects",
                Description = "Output script objects from container",
                RequiredFields = new[] { "InputPath" },
                OptionalFields = new[] { "AesKey" }
            }
        };
    }

    public static RetocCommandFieldDefinition[] GetGlobalOptions()
    {
        return new[]
        {
            new RetocCommandFieldDefinition
            {
                FieldName = "InputPath",
                Label = "Input Path",
                FieldType = "Path",
                HelpText = "Full path to input file or directory"
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "OutputPath",
                Label = "Output Path",
                FieldType = "Path",
                HelpText = "Full path to output file or directory"
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "EngineVersion",
                Label = "Engine Version",
                FieldType = "Enum",
                HelpText = "Unreal Engine version (required for ToZen)",
                EnumValues = new[] { "UE5_0", "UE5_1", "UE5_2", "UE5_3", "UE5_4", "UE5_5", "UE5_6" }
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "AesKey",
                Label = "AES Key",
                FieldType = "String",
                HelpText = "AES encryption key (hex format, e.g., 0x1234...)"
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "ContainerHeaderVersion",
                Label = "Container Header Version",
                FieldType = "Enum",
                HelpText = "Override container header version",
                EnumValues = new[] { "Initial", "LocalizedPackages", "OptimizedNames" }
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "TocVersion",
                Label = "TOC Version",
                FieldType = "Enum",
                HelpText = "Override TOC version",
                EnumValues = new[] { "DirectoryIndex", "PartitionSize", "PerfectHash", "PerfectHashWithOverflow" }
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "ChunkId",
                Label = "Chunk ID",
                FieldType = "String",
                HelpText = "Chunk ID to retrieve (required for Get command)"
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "Verbose",
                Label = "Verbose Output",
                FieldType = "Boolean",
                HelpText = "Enable verbose logging"
            },
            new RetocCommandFieldDefinition
            {
                FieldName = "TimeoutSeconds",
                Label = "Timeout (seconds)",
                FieldType = "Integer",
                HelpText = "Execution timeout in seconds",
                MinValue = 1,
                MaxValue = 3600
            }
        };
    }
}
