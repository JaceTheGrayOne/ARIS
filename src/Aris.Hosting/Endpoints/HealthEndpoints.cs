using System.Reflection;
using Aris.Contracts;
using Aris.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Aris.Hosting.Endpoints;

/// <summary>
/// Minimal health and info HTTP endpoints.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthAndInfoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", async (
            HttpContext httpContext,
            IOptions<WorkspaceOptions> workspaceOptions
        ) =>
        {
            // NOTE: For Phase 5 Chunk 1 we keep this simple and optimistic.
            // Later phases can wire real dependency validation and startup states.
            var workspacePath = workspaceOptions.Value.DefaultWorkspacePath;
            var response = new HealthResponse(
                Status: "Ready",
                DependenciesReady: true,
                CurrentWorkspace: string.IsNullOrWhiteSpace(workspacePath) ? null : workspacePath,
                Message: "ARIS backend is running."
            );

            await httpContext.Response.WriteAsJsonAsync(response);
        });

        endpoints.MapGet("/info", async (
            HttpContext httpContext
        ) =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "0.0.0";

            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.ToUriComponent()}";

            // Phase 5 Chunk 1: keep ToolVersions simple.
            // Later we can hydrate from the tools manifest if desired.
            var toolVersions = new Dictionary<string, string>();

            var info = new InfoResponse(
                Version: version,
                BackendBaseUrl: baseUrl,
                IpcToken: null,
                ToolVersions: toolVersions
            );

            await httpContext.Response.WriteAsJsonAsync(info);
        });

        return endpoints;
    }
}
