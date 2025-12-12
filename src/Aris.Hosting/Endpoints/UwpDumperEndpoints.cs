using Aris.Adapters.UwpDumper;
using Aris.Contracts;
using Aris.Contracts.Retoc;
using Aris.Contracts.UwpDumper;
using Aris.Core.Errors;
using Aris.Core.UwpDumper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aris.Hosting.Endpoints;

/// <summary>
/// HTTP endpoints for UWPDumper operations.
/// </summary>
public static class UwpDumperEndpoints
{
    public static IEndpointRouteBuilder MapUwpDumperEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/uwpdumper")
            .WithTags("UwpDumper");

        group.MapPost("/dump", DumpAsync);

        return endpoints;
    }

    private static async Task<IResult> DumpAsync(
        UwpDumpRequest request,
        IUwpDumperAdapter adapter,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;

        var logger = loggerFactory.CreateLogger("UwpDumperEndpoints");
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId
        });

        try
        {
            if (!Enum.TryParse<UwpDumpMode>(request.Mode, ignoreCase: true, out var mode))
            {
                var error = new ErrorInfo(
                    Code: "VALIDATION_ERROR",
                    Message: $"Invalid UWP dump mode '{request.Mode}'. Valid modes: {string.Join(", ", Enum.GetNames<UwpDumpMode>())}",
                    RemediationHint: "Specify a valid dump mode (FullDump, MetadataOnly, or ValidateOnly)."
                );

                var failureResponse = new UwpDumpResponse(
                    OperationId: operationId,
                    Status: OperationStatus.Failed,
                    Result: null,
                    Error: error,
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow
                );

                return Results.BadRequest(failureResponse);
            }

            var command = new UwpDumpCommand
            {
                OperationId = operationId,
                PackageFamilyName = request.PackageFamilyName,
                ApplicationId = request.ApplicationId,
                OutputPath = request.OutputPath,
                Mode = mode,
                IncludeSymbols = request.IncludeSymbols,
                WorkingDirectory = null,
                TimeoutSeconds = null
            };

            logger.LogInformation("Starting UWP dump for {PackageFamilyName} with mode {Mode}",
                request.PackageFamilyName, mode);

            var result = await adapter.DumpAsync(command, cancellationToken, progress: null);

            var resultDto = MapToResultDto(result, mode);

            var response = new UwpDumpResponse(
                OperationId: operationId,
                Status: OperationStatus.Succeeded,
                Result: resultDto,
                Error: null,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            logger.LogInformation("UWP dump operation {OperationId} completed successfully in {Duration}ms",
                operationId, result.Duration.TotalMilliseconds);

            return Results.Ok(response);
        }
        catch (ArisException ex)
        {
            logger.LogError(ex, "UWP dump operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

            var error = new ErrorInfo(
                Code: ex.ErrorCode,
                Message: ex.Message,
                RemediationHint: ex.RemediationHint
            );

            var failureResponse = new UwpDumpResponse(
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
            logger.LogError(ex, "UWP dump operation {OperationId} failed unexpectedly", operationId);

            var error = new ErrorInfo(
                Code: "UNEXPECTED_ERROR",
                Message: "An unexpected error occurred during the UWP dump operation.",
                RemediationHint: "Check the logs for more details."
            );

            var failureResponse = new UwpDumpResponse(
                OperationId: operationId,
                Status: OperationStatus.Failed,
                Result: null,
                Error: error,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            return Results.Json(failureResponse, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static UwpDumpResultDto MapToResultDto(UwpDumpResult result, UwpDumpMode mode)
    {
        var artifacts = result.Artifacts
            .Select(a => new ProducedFileDto(
                Path: a.Path,
                SizeBytes: a.SizeBytes,
                Sha256: a.Sha256,
                FileType: a.FileType
            ))
            .ToList();

        return new UwpDumpResultDto(
            OperationId: result.OperationId,
            PackageFamilyName: result.PackageFamilyName,
            ApplicationId: result.ApplicationId,
            OutputPath: result.OutputPath,
            Mode: mode.ToString(),
            Duration: result.Duration,
            Warnings: result.Warnings.ToList(),
            Artifacts: artifacts,
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
