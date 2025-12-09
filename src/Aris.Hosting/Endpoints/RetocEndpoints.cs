using Aris.Adapters.Retoc;
using Aris.Contracts;
using Aris.Contracts.Retoc;
using Aris.Core.Errors;
using Aris.Core.Retoc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aris.Hosting.Endpoints;

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
                        Code: "VALIDATION_ERROR",
                        Message: $"Invalid mode '{request.Mode}'. Valid modes: {string.Join(", ", Enum.GetNames<RetocMode>())}",
                        RemediationHint: "Specify a valid conversion mode."
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

                var command = new RetocCommand
                {
                    OperationId = operationId,
                    InputPath = request.InputPath,
                    OutputPath = request.OutputPath,
                    Mode = mode,
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
                    Code: ex.ErrorCode,
                    Message: ex.Message,
                    RemediationHint: ex.RemediationHint
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
                    Code: "UNEXPECTED_ERROR",
                    Message: "An unexpected error occurred during the Retoc operation.",
                    RemediationHint: "Check the logs for more details."
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

        return endpoints;
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
