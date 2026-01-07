using Aris.Adapters.Retoc;
using Aris.Core.Retoc;
using Xunit;

namespace Aris.Core.Tests.Retoc;

public class RetocCommandSchemaProviderTests
{
    [Fact]
    public void GetSchema_ReturnsValidSchema()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        Assert.NotNull(schema);
        Assert.NotNull(schema.Commands);
        Assert.NotEmpty(schema.Commands);
    }

    [Fact]
    public void GetSchema_IncludesAllRetocCommandTypes()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        // Verify all command types are present in the schema
        var commandTypes = Enum.GetNames<RetocCommandType>();

        foreach (var commandType in commandTypes)
        {
            Assert.Contains(schema.Commands, c => c.CommandType == commandType);
        }
    }

    [Fact]
    public void GetSchema_ToLegacyCommand_HasCorrectFields()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        var toLegacyCommand = schema.Commands.FirstOrDefault(c => c.CommandType == nameof(RetocCommandType.ToLegacy));
        Assert.NotNull(toLegacyCommand);
        Assert.Equal(nameof(RetocCommandType.ToLegacy), toLegacyCommand.CommandType);
        Assert.NotNull(toLegacyCommand.DisplayName);
        Assert.NotNull(toLegacyCommand.Description);

        // ToLegacy should have InputPath and OutputPath
        var allFields = toLegacyCommand.RequiredFields.Concat(toLegacyCommand.OptionalFields);
        Assert.Contains("InputPath", allFields);
        Assert.Contains("OutputPath", allFields);
    }

    [Fact]
    public void GetSchema_ToZenCommand_HasCorrectFields()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        var toZenCommand = schema.Commands.FirstOrDefault(c => c.CommandType == nameof(RetocCommandType.ToZen));
        Assert.NotNull(toZenCommand);
        Assert.Equal(nameof(RetocCommandType.ToZen), toZenCommand.CommandType);
        Assert.NotNull(toZenCommand.DisplayName);

        var allFields = toZenCommand.RequiredFields.Concat(toZenCommand.OptionalFields);
        Assert.Contains("InputPath", allFields);
        Assert.Contains("OutputPath", allFields);
    }

    [Fact]
    public void GetSchema_GetCommand_RequiresChunkId()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        var getCommand = schema.Commands.FirstOrDefault(c => c.CommandType == nameof(RetocCommandType.Get));
        Assert.NotNull(getCommand);

        // ChunkId should be in required fields for Get command
        Assert.Contains("ChunkId", getCommand.RequiredFields);
        // OutputPath should be optional for Get command
        Assert.Contains("OutputPath", getCommand.OptionalFields);
    }

    [Fact]
    public void GetSchema_InfoCommand_DoesNotRequireOutputPath()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        var infoCommand = schema.Commands.FirstOrDefault(c => c.CommandType == nameof(RetocCommandType.Info));
        Assert.NotNull(infoCommand);

        // Info command should not require OutputPath
        Assert.DoesNotContain("OutputPath", infoCommand.RequiredFields);
    }

    [Fact]
    public void GetSchema_AllCommands_HaveInputPath()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        foreach (var command in schema.Commands)
        {
            var allFields = command.RequiredFields.Concat(command.OptionalFields);
            Assert.Contains("InputPath", allFields);
        }
    }

    [Fact]
    public void GetSchema_Commands_HaveUniqueCommandTypes()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        var commandTypes = schema.Commands.Select(c => c.CommandType).ToList();
        var uniqueTypes = commandTypes.Distinct().ToList();

        Assert.Equal(commandTypes.Count, uniqueTypes.Count);
    }

    [Theory]
    [InlineData(nameof(RetocCommandType.ToLegacy))]
    [InlineData(nameof(RetocCommandType.ToZen))]
    [InlineData(nameof(RetocCommandType.Verify))]
    [InlineData(nameof(RetocCommandType.Info))]
    [InlineData(nameof(RetocCommandType.List))]
    public void GetSchema_CommonCommands_ArePresent(string commandType)
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        Assert.Contains(schema.Commands, c => c.CommandType == commandType);
    }

    [Fact]
    public void GetSchema_IsSerializable()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        // Verify that the schema can be serialized to JSON (important for API endpoint)
        var json = System.Text.Json.JsonSerializer.Serialize(schema);
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // Verify it can be deserialized back
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<Aris.Contracts.Retoc.RetocCommandSchemaResponse>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(schema.Commands.Length, deserialized!.Commands.Length);
    }

    [Fact]
    public void GetSchema_AllCommands_HaveDisplayNameAndDescription()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        foreach (var command in schema.Commands)
        {
            Assert.False(string.IsNullOrWhiteSpace(command.DisplayName),
                $"Command {command.CommandType} should have a DisplayName");
            Assert.False(string.IsNullOrWhiteSpace(command.Description),
                $"Command {command.CommandType} should have a Description");
        }
    }

    [Fact]
    public void GetSchema_CommandCount_MatchesEnumCount()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();
        var enumCount = Enum.GetNames<RetocCommandType>().Length;

        Assert.Equal(enumCount, schema.Commands.Length);
    }

    [Fact]
    public void GetSchema_RequiredAndOptionalFields_DoNotOverlap()
    {
        var schema = RetocCommandSchemaProvider.GetSchema();

        foreach (var command in schema.Commands)
        {
            var requiredSet = new HashSet<string>(command.RequiredFields);
            var optionalSet = new HashSet<string>(command.OptionalFields);

            requiredSet.IntersectWith(optionalSet);
            Assert.Empty(requiredSet); // No overlap allowed
        }
    }
}
