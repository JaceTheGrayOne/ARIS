using System.CommandLine;
using Aris.ToolDocsGen.Commands;

// Root command
var rootCommand = new RootCommand("ARIS Tool Documentation Generator")
{
    Name = "Aris.ToolDocsGen"
};

// Generate command
var generateCommand = new Command("generate", "Generate documentation and schema for tools");

var toolOption = new Option<string?>(
    aliases: ["--tool", "-t"],
    description: "Tool ID from manifest (e.g., retoc, uwpdumper)");

var allOption = new Option<bool>(
    aliases: ["--all", "-a"],
    description: "Generate docs for all tools in manifest");

var outputOption = new Option<string>(
    aliases: ["--out", "-o"],
    description: "Output directory for generated docs")
{
    IsRequired = true
};

generateCommand.AddOption(toolOption);
generateCommand.AddOption(allOption);
generateCommand.AddOption(outputOption);

generateCommand.SetHandler(async (tool, all, output) =>
{
    var cmd = new GenerateCommand();

    if (all)
    {
        Environment.ExitCode = await cmd.ExecuteAllAsync(output);
    }
    else if (!string.IsNullOrEmpty(tool))
    {
        Environment.ExitCode = await cmd.ExecuteAsync(tool, output);
    }
    else
    {
        Console.Error.WriteLine("Error: Either --tool or --all must be specified.");
        Environment.ExitCode = 1;
    }
}, toolOption, allOption, outputOption);

rootCommand.AddCommand(generateCommand);

// Validate command
var validateCommand = new Command("validate", "Validate schema completeness for a tool");

var validateToolOption = new Option<string>(
    aliases: ["--tool", "-t"],
    description: "Tool ID to validate")
{
    IsRequired = true
};

var validateDocsOption = new Option<string>(
    aliases: ["--docs", "-d"],
    description: "Path to docs/tools directory")
{
    IsRequired = true
};

validateCommand.AddOption(validateToolOption);
validateCommand.AddOption(validateDocsOption);

validateCommand.SetHandler(async (tool, docs) =>
{
    var cmd = new ValidateCommand();
    Environment.ExitCode = await cmd.ExecuteAsync(tool, docs);
}, validateToolOption, validateDocsOption);

rootCommand.AddCommand(validateCommand);

// Run
return await rootCommand.InvokeAsync(args);
