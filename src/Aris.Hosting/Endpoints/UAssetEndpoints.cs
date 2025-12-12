using Aris.Adapters.UAsset;
using Aris.Contracts;
using Aris.Contracts.Retoc;
using Aris.Contracts.UAsset;
using Aris.Core.Errors;
using Aris.Core.UAsset;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aris.Hosting.Endpoints;

/// <summary>
/// HTTP endpoints for UAsset operations.
/// </summary>
public static class UAssetEndpoints
{
    public static IEndpointRouteBuilder MapUAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/uasset")
            .WithTags("UAsset");

        group.MapPost("/serialize", SerializeAsync);
        group.MapPost("/deserialize", DeserializeAsync);
        group.MapPost("/inspect", InspectAsync);

        return endpoints;
    }

    private static async Task<IResult> SerializeAsync(
        UAssetSerializeRequest request,
        IUAssetService uassetService,
        ILogger<IUAssetService> logger,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var operationId = Guid.NewGuid().ToString("N");

        try
        {
            var command = new UAssetSerializeCommand
            {
                OperationId = operationId,
                InputJsonPath = request.InputJsonPath,
                OutputAssetPath = request.OutputAssetPath,
                Game = request.Game,
                UEVersion = request.UEVersion,
                SchemaVersion = request.SchemaVersion,
                CompressionFormat = request.CompressionFormat,
                CompressionLevel = request.CompressionLevel,
                TimeoutSeconds = request.TimeoutSeconds
            };

            logger.LogInformation("Starting UAsset serialize operation {OperationId}: {InputPath} to {OutputPath}",
                operationId, request.InputJsonPath, request.OutputAssetPath);

            var result = await uassetService.SerializeAsync(command, cancellationToken, progress: null);

            var resultDto = MapToResultDto(operationId, result);

            var response = new UAssetSerializeResponse(
                OperationId: operationId,
                Status: OperationStatus.Succeeded,
                Result: resultDto,
                Error: null,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            logger.LogInformation("UAsset serialize operation {OperationId} completed successfully in {Duration}ms",
                operationId, result.Duration.TotalMilliseconds);

            return Results.Ok(response);
        }
        catch (ArisException ex)
        {
            logger.LogError(ex, "UAsset serialize operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

            var error = new ErrorInfo(
                Code: ex.ErrorCode,
                Message: ex.Message,
                RemediationHint: ex.RemediationHint
            );

            var failureResponse = new UAssetSerializeResponse(
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
            logger.LogError(ex, "UAsset serialize operation {OperationId} failed unexpectedly", operationId);

            var error = new ErrorInfo(
                Code: "UNEXPECTED_ERROR",
                Message: "An unexpected error occurred during the UAsset serialize operation.",
                RemediationHint: "Check the logs for more details."
            );

            var failureResponse = new UAssetSerializeResponse(
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

    private static async Task<IResult> DeserializeAsync(
        UAssetDeserializeRequest request,
        IUAssetService uassetService,
        ILogger<IUAssetService> logger,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var operationId = Guid.NewGuid().ToString("N");

        try
        {
            var command = new UAssetDeserializeCommand
            {
                OperationId = operationId,
                InputAssetPath = request.InputAssetPath,
                OutputJsonPath = request.OutputJsonPath,
                Game = request.Game,
                UEVersion = request.UEVersion,
                SchemaVersion = request.SchemaVersion,
                IncludeBulkData = request.IncludeBulkData,
                TimeoutSeconds = request.TimeoutSeconds
            };

            logger.LogInformation("Starting UAsset deserialize operation {OperationId}: {InputPath} to {OutputPath}",
                operationId, request.InputAssetPath, request.OutputJsonPath);

            var result = await uassetService.DeserializeAsync(command, cancellationToken, progress: null);

            var resultDto = MapToResultDto(operationId, result);

            var response = new UAssetDeserializeResponse(
                OperationId: operationId,
                Status: OperationStatus.Succeeded,
                Result: resultDto,
                Error: null,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            logger.LogInformation("UAsset deserialize operation {OperationId} completed successfully in {Duration}ms",
                operationId, result.Duration.TotalMilliseconds);

            return Results.Ok(response);
        }
        catch (ArisException ex)
        {
            logger.LogError(ex, "UAsset deserialize operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

            var error = new ErrorInfo(
                Code: ex.ErrorCode,
                Message: ex.Message,
                RemediationHint: ex.RemediationHint
            );

            var failureResponse = new UAssetDeserializeResponse(
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
            logger.LogError(ex, "UAsset deserialize operation {OperationId} failed unexpectedly", operationId);

            var error = new ErrorInfo(
                Code: "UNEXPECTED_ERROR",
                Message: "An unexpected error occurred during the UAsset deserialize operation.",
                RemediationHint: "Check the logs for more details."
            );

            var failureResponse = new UAssetDeserializeResponse(
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

    private static async Task<IResult> InspectAsync(
        UAssetInspectRequest request,
        IUAssetService uassetService,
        ILogger<IUAssetService> logger,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var operationId = Guid.NewGuid().ToString("N");

        try
        {
            var command = new UAssetInspectCommand
            {
                OperationId = operationId,
                InputAssetPath = request.InputAssetPath,
                Fields = request.Fields ?? Array.Empty<string>()
            };

            logger.LogInformation("Starting UAsset inspect operation {OperationId}: {InputPath} (fields: {Fields})",
                operationId, request.InputAssetPath, string.Join(", ", request.Fields ?? Array.Empty<string>()));

            var inspection = await uassetService.InspectAsync(command, cancellationToken, progress: null);

            var inspectionDto = MapToInspectionDto(operationId, inspection);

            var response = new UAssetInspectResponse(
                OperationId: operationId,
                Status: OperationStatus.Succeeded,
                Result: inspectionDto,
                Error: null,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            logger.LogInformation("UAsset inspect operation {OperationId} completed successfully", operationId);

            return Results.Ok(response);
        }
        catch (ArisException ex)
        {
            logger.LogError(ex, "UAsset inspect operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

            var error = new ErrorInfo(
                Code: ex.ErrorCode,
                Message: ex.Message,
                RemediationHint: ex.RemediationHint
            );

            var failureResponse = new UAssetInspectResponse(
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
            logger.LogError(ex, "UAsset inspect operation {OperationId} failed unexpectedly", operationId);

            var error = new ErrorInfo(
                Code: "UNEXPECTED_ERROR",
                Message: "An unexpected error occurred during the UAsset inspect operation.",
                RemediationHint: "Check the logs for more details."
            );

            var failureResponse = new UAssetInspectResponse(
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

    private static UAssetResultDto MapToResultDto(string operationId, UAssetResult result)
    {
        var producedFiles = result.ProducedFiles
            .Select(pf => new ProducedFileDto(
                Path: pf.Path,
                SizeBytes: pf.SizeBytes,
                Sha256: pf.Sha256,
                FileType: pf.FileType
            ))
            .ToList();

        return new UAssetResultDto(
            OperationId: operationId,
            InputPath: result.InputPath,
            OutputPath: result.OutputPath,
            Operation: result.Operation.ToString(),
            UEVersion: result.UEVersion,
            SchemaVersion: result.SchemaVersion,
            Duration: result.Duration,
            Warnings: result.Warnings.ToList(),
            ProducedFiles: producedFiles,
            LogExcerpt: result.LogExcerpt
        );
    }

    private static UAssetInspectionDto MapToInspectionDto(string operationId, UAssetInspection inspection)
    {
        var summaryDto = new UAssetSummaryDto(
            UEVersion: inspection.Summary.UEVersion,
            LicenseeVersion: inspection.Summary.LicenseeVersion,
            CustomVersionCount: inspection.Summary.CustomVersionCount,
            NameCount: inspection.Summary.NameCount,
            ExportCount: inspection.Summary.ExportCount,
            ImportCount: inspection.Summary.ImportCount
        );

        return new UAssetInspectionDto(
            OperationId: operationId,
            AssetPath: inspection.InputPath,
            Summary: summaryDto,
            Exports: inspection.Exports,
            Imports: inspection.Imports,
            Names: inspection.Names,
            LogExcerpt: null
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
