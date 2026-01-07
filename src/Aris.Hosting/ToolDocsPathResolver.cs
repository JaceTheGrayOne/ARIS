using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Aris.Hosting;

/// <summary>
/// Resolves the path to tool documentation files.
/// In development, docs are at repo root; in production, they're copied to ContentRootPath.
/// </summary>
public static class ToolDocsPathResolver
{
    /// <summary>
    /// Gets the root path for tool documentation (docs/tools directory).
    /// </summary>
    public static string GetDocsToolsRoot(IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            // Development: ContentRootPath is src/Aris.Hosting
            // Navigate up two levels to repo root, then into docs/tools
            return Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "docs", "tools"));
        }

        // Production: docs are copied to ContentRootPath/docs/tools
        return Path.Combine(env.ContentRootPath, "docs", "tools");
    }
}
