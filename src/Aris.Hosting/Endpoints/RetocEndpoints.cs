using Aris.Adapters.Retoc;
using Aris.Contracts;
using Aris.Contracts.Retoc;
using Aris.Core.Errors;
using Aris.Core.Retoc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aris.Hosting.Endpoints;

using Aris.Hosting;

/// <summary>
/// HTTP endpoints for Retoc operations.
/// </summary>
public static class RetocEndpoints
{
    public static IEndpointRouteBuilder MapRetocEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/retoc");

        group.MapPost("/convert", async (
            HttpContext httpContext,
            RetocConvertRequest request,
            IRetocAdapter retocAdapter,
            ILogger<IRetocAdapter> logger,
            CancellationToken cancellationToken
        ) =>
        {
            var startedAt = DateTimeOffset.UtcNow;
            var operationId = Guid.NewGuid().ToString("N");

            try
            {
                if (!Enum.TryParse<RetocMode>(request.Mode, ignoreCase: true, out var mode))
                {
                    var error = new ErrorInfo(
                        "VALIDATION_ERROR",
                        $"Invalid mode '{request.Mode}'. Valid modes: {string.Join(", ", Enum.GetNames<RetocMode>())}",
                        "Specify a valid conversion mode."
                    );

                    var failureResponse = new RetocConvertResponse(
                        OperationId: operationId,
                        Status: OperationStatus.Failed,
                        Result: null,
                        Error: error,
                        StartedAt: startedAt,
                        CompletedAt: DateTimeOffset.UtcNow
                    );

                    return Results.BadRequest(failureResponse);
                }

                var mountKeys = new List<string>();
                if (request.MountKeys != null)
                {
                    foreach (var kvp in request.MountKeys)
                    {
                        mountKeys.Add($"{kvp.Key}={kvp.Value}");
                    }
                }

                // Map mode to explicit CommandType for simple Pack/Unpack flow
                var commandType = mode switch
                {
                    RetocMode.PakToIoStore => RetocCommandType.ToZen,     // Pack: Legacy → Zen
                    RetocMode.IoStoreToPak => RetocCommandType.ToLegacy,   // Unpack: Zen → Legacy
                    RetocMode.Validate => RetocCommandType.Verify,
                    _ => default(RetocCommandType)
                };

                var command = new RetocCommand
                {
                    OperationId = operationId,
                    CommandType = commandType,
                    InputPath = request.InputPath,
                    OutputPath = request.OutputPath,
                    Mode = mode,
                    Version = request.EngineVersion,  // --version flag for to-zen
                    GameVersion = request.Game,
                    UEVersion = request.UEVersion,
                    CompressionFormat = request.CompressionFormat,
                    CompressionLevel = request.CompressionLevel,
                    TimeoutSeconds = request.TimeoutSeconds,
                    MountKeys = mountKeys,
                    IncludeFilters = request.IncludeFilters ?? new List<string>(),
                    ExcludeFilters = request.ExcludeFilters ?? new List<string>()
                };

                logger.LogInformation("Starting Retoc convert operation {OperationId}: {Mode} from {InputPath} to {OutputPath}",
                    operationId, mode, request.InputPath, request.OutputPath);

                var result = await retocAdapter.ConvertAsync(command, cancellationToken, progress: null);

                var resultDto = MapToResultDto(result);

                var response = new RetocConvertResponse(
                    OperationId: operationId,
                    Status: OperationStatus.Succeeded,
                    Result: resultDto,
                    Error: null,
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow
                );

                logger.LogInformation("Retoc convert operation {OperationId} completed successfully", operationId);

                return Results.Ok(response);
            }
            catch (ArisException ex)
            {
                logger.LogError(ex, "Retoc convert operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

                var error = new ErrorInfo(
                    ex.ErrorCode,
                    ex.Message,
                    ex.RemediationHint
                );

                var failureResponse = new RetocConvertResponse(
                    OperationId: operationId,
                    Status: OperationStatus.Failed,
                    Result: null,
                    Error: error,
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow
                );

                var statusCode = MapExceptionToStatusCode(ex);
                return Results.Json(failureResponse, statusCode: statusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Retoc convert operation {OperationId} failed unexpectedly", operationId);

                var error = new ErrorInfo(
                    "UNEXPECTED_ERROR",
                    "An unexpected error occurred during the Retoc operation.",
                    "Check the logs for more details."
                );

                var failureResponse = new RetocConvertResponse(
                    OperationId: operationId,
                    Status: OperationStatus.Failed,
                    Result: null,
                    Error: error,
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow
                );

                return Results.Json(failureResponse, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        // POST /api/retoc/build - Build command for preview
        group.MapPost("/build", (
            RetocBuildCommandRequest request,
            IRetocAdapter retocAdapter,
            ILogger<IRetocAdapter> logger
        ) =>
        {
            try
            {
                if (!Enum.TryParse<RetocCommandType>(request.CommandType, ignoreCase: true, out var commandType))
                {
                    return Results.BadRequest(new ErrorInfo(
                        "VALIDATION_ERROR",
                        $"Invalid command type '{request.CommandType}'",
                        "Specify a valid Retoc command type"
                    ));
                }

                var command = MapBuildRequestToCommand(request, commandType);
                var (executablePath, arguments, commandLine) = retocAdapter.BuildCommand(command);

                var response = new RetocBuildCommandResponse
                {
                    ExecutablePath = executablePath,
                    Arguments = arguments,
                    CommandLine = commandLine
                };

                return Results.Ok(response);
            }
            catch (ArisException ex)
            {
                logger.LogError(ex, "Failed to build Retoc command: {ErrorCode}", ex.ErrorCode);
                var error = new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint);
                var statusCode = MapExceptionToStatusCode(ex);
                return Results.Json(error, statusCode: statusCode);
            }
        });

        // GET /api/retoc/schema - Get command schema for Advanced Mode
        // Derived from canonical schema + UI mapping overlay
        group.MapGet("/schema", (IWebHostEnvironment env) =>
        {
            var docsRoot = Path.Combine(ToolDocsPathResolver.GetDocsToolsRoot(env), "retoc");
            var canonicalPath = Path.Combine(docsRoot, "schema.effective.json");
            var mappingPath = Path.Combine(docsRoot, "ui.mapping.json");

            if (!File.Exists(canonicalPath))
            {
                return Results.NotFound(new ErrorInfo(
                    "SCHEMA_NOT_FOUND",
                    "Canonical schema not found.",
                    "Run ToolDocsGen to generate docs/tools/retoc/schema.effective.json"));
            }

            if (!File.Exists(mappingPath))
            {
                return Results.NotFound(new ErrorInfo(
                    "MAPPING_NOT_FOUND",
                    "UI mapping not found.",
                    "Create docs/tools/retoc/ui.mapping.json"));
            }

            try
            {
                var canonicalJson = File.ReadAllText(canonicalPath);
                var mappingJson = File.ReadAllText(mappingPath);
                var schema = RetocSchemaDerived.Derive(canonicalJson, mappingJson);
                return Results.Ok(schema);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Schema derivation failed");
            }
        });

        // GET /api/retoc/help - Get Retoc help text
        group.MapGet("/help", async (
            HttpContext httpContext,
            IRetocAdapter retocAdapter,
            ILogger<IRetocAdapter> logger,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                // Build a simple command to get the executable path
                var dummyCommand = new RetocCommand
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    CommandType = RetocCommandType.Info,
                    InputPath = "C:\\dummy",
                    OutputPath = "C:\\dummy"
                };

                var (executablePath, _, _) = retocAdapter.BuildCommand(dummyCommand);

                // Execute retoc --help using ProcessRunner
                // Use DI to get the process runner with proper logger
                var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var processRunnerLogger = loggerFactory.CreateLogger<Aris.Infrastructure.Process.ProcessRunner>();
                var processRunner = new Aris.Infrastructure.Process.ProcessRunner(processRunnerLogger);
                var result = await processRunner.ExecuteAsync(
                    executablePath,
                    "--help",
                    workingDirectory: null,
                    timeoutSeconds: 10,
                    environmentVariables: null,
                    cancellationToken: cancellationToken
                );

                // Wrap output in Markdown code fence
                var markdown = $"```\n{result.StdOut}\n```";

                var response = new RetocHelpResponse
                {
                    Markdown = markdown
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve Retoc help text");
                var error = new ErrorInfo(
                    "HELP_RETRIEVAL_ERROR",
                    "Failed to retrieve Retoc help text",
                    "Ensure Retoc is properly installed and accessible"
                );
                return Results.Json(error, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        // GET /api/retoc/stream - WebSocket endpoint for streaming Retoc execution with ConPTY
        group.MapGet("/stream", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ErrorInfo(
                    "WEBSOCKET_REQUIRED",
                    "This endpoint requires a WebSocket connection",
                    "Use a WebSocket client to connect to this endpoint"));
                return;
            }

            var handler = context.RequestServices.GetRequiredService<RetocStreamHandler>();
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await handler.HandleAsync(webSocket, context.RequestAborted);
        });

        return endpoints;
    }

    private static RetocCommand MapBuildRequestToCommand(RetocBuildCommandRequest request, RetocCommandType commandType)
    {
        var additionalArgs = new List<string>();

        if (request.Verbose)
        {
            additionalArgs.Add("--verbose");
        }

        RetocContainerHeaderVersion? containerHeaderVersion = null;
        if (!string.IsNullOrEmpty(request.ContainerHeaderVersion) &&
            Enum.TryParse<RetocContainerHeaderVersion>(request.ContainerHeaderVersion, out var chv))
        {
            containerHeaderVersion = chv;
        }

        RetocTocVersion? tocVersion = null;
        if (!string.IsNullOrEmpty(request.TocVersion) &&
            Enum.TryParse<RetocTocVersion>(request.TocVersion, out var tv))
        {
            tocVersion = tv;
        }

        return new RetocCommand
        {
            OperationId = Guid.NewGuid().ToString("N"),
            CommandType = commandType,
            InputPath = request.InputPath,
            OutputPath = request.OutputPath,
            Version = request.EngineVersion,
            AesKey = request.AesKey,
            ContainerHeaderVersion = containerHeaderVersion,
            TocVersion = tocVersion,
            ChunkId = request.ChunkId,
            TimeoutSeconds = request.TimeoutSeconds,
            AdditionalArgs = additionalArgs
        };
    }

    private static RetocResultDto MapToResultDto(RetocResult result)
    {
        var producedFiles = result.ProducedFiles
            .Select(pf => new ProducedFileDto(
                Path: pf.Path,
                SizeBytes: pf.SizeBytes,
                Sha256: pf.Sha256,
                FileType: pf.FileType
            ))
            .ToList();

        return new RetocResultDto(
            ExitCode: result.ExitCode,
            OutputPath: result.OutputPath,
            OutputFormat: result.OutputFormat,
            Duration: result.Duration,
            Warnings: result.Warnings.ToList(),
            ProducedFiles: producedFiles,
            SchemaVersion: null,
            UEVersion: null,
            LogExcerpt: result.LogExcerpt
        );
    }

    private static int MapExceptionToStatusCode(ArisException ex)
    {
        return ex switch
        {
            ValidationError => StatusCodes.Status400BadRequest,
            DependencyMissingError => StatusCodes.Status503ServiceUnavailable,
            ElevationRequiredError => StatusCodes.Status403Forbidden,
            ChecksumMismatchError => StatusCodes.Status502BadGateway,
            ToolExecutionError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
