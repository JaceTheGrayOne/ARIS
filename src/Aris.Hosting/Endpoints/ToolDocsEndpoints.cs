using Aris.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Aris.Hosting.Endpoints;

using Aris.Hosting;

/// <summary>
/// HTTP endpoints for serving tool documentation and schema.
/// </summary>
public static class ToolDocsEndpoints
{
    private static readonly HashSet<string> AllowedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "retoc",
        "uwpdumper",
        "dllinjector",
        "uasset"
    };

    public static IEndpointRouteBuilder MapToolDocsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tools/{tool}");

        group.MapGet("/help", GetToolHelp);
        group.MapGet("/schema", GetToolSchema);

        return endpoints;
    }

    private static IResult GetToolHelp(string tool, IWebHostEnvironment env)
    {
        // Validate tool name (security: allowlist only)
        if (!AllowedTools.Contains(tool))
        {
            return Results.BadRequest(new ErrorInfo(
                Code: "INVALID_TOOL",
                Message: $"Tool '{tool}' is not recognized.",
                RemediationHint: $"Valid tools: {string.Join(", ", AllowedTools)}"));
        }

        // Resolve help file path via ToolDocsPathResolver
        var docsRoot = ToolDocsPathResolver.GetDocsToolsRoot(env);
        var helpPath = Path.Combine(docsRoot, tool.ToLowerInvariant(), "help.txt");

        if (!File.Exists(helpPath))
        {
            return Results.NotFound(new ErrorInfo(
                Code: "HELP_NOT_FOUND",
                Message: $"Help documentation for '{tool}' not found.",
                RemediationHint: "Run the ToolDocsGen generator to create help files."));
        }

        var content = File.ReadAllText(helpPath);
        return Results.Text(content, "text/plain");
    }

    private static IResult GetToolSchema(string tool, IWebHostEnvironment env)
    {
        // Validate tool name (security: allowlist only)
        if (!AllowedTools.Contains(tool))
        {
            return Results.BadRequest(new ErrorInfo(
                Code: "INVALID_TOOL",
                Message: $"Tool '{tool}' is not recognized.",
                RemediationHint: $"Valid tools: {string.Join(", ", AllowedTools)}"));
        }

        // Resolve schema file path via ToolDocsPathResolver
        var docsRoot = ToolDocsPathResolver.GetDocsToolsRoot(env);
        var schemaPath = Path.Combine(docsRoot, tool.ToLowerInvariant(), "schema.effective.json");

        if (!File.Exists(schemaPath))
        {
            return Results.NotFound(new ErrorInfo(
                Code: "SCHEMA_NOT_FOUND",
                Message: $"Schema for '{tool}' not found.",
                RemediationHint: "Run the ToolDocsGen generator to create schema files."));
        }

        var content = File.ReadAllText(schemaPath);
        return Results.Content(content, "application/json");
    }
}
