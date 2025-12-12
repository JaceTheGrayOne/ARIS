using Aris.Adapters.DllInjector;
using Aris.Contracts;
using Aris.Contracts.DllInjector;
using Aris.Core.DllInjector;
using Aris.Core.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aris.Hosting.Endpoints;

/// <summary>
/// HTTP endpoints for DLL Injector operations.
/// </summary>
public static class DllInjectorEndpoints
{
    public static IEndpointRouteBuilder MapDllInjectorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dllinjector")
            .WithTags("DllInjector");

        group.MapPost("/inject", InjectAsync);
        group.MapPost("/eject", EjectAsync);

        return endpoints;
    }

    private static async Task<IResult> InjectAsync(
        DllInjectRequest request,
        IDllInjectorAdapter adapter,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;

        var logger = loggerFactory.CreateLogger("DllInjectorEndpoints");
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId
        });

        try
        {
            if (!Enum.TryParse<DllInjectionMethod>(request.Method, ignoreCase: true, out var method))
            {
                var error = new ErrorInfo(
                    Code: "VALIDATION_ERROR",
                    Message: $"Invalid injection method '{request.Method}'. Valid methods: {string.Join(", ", Enum.GetNames<DllInjectionMethod>())}",
                    RemediationHint: "Specify a valid injection method (CreateRemoteThread, ApcQueue, or ManualMap)."
                );

                var failureResponse = new DllInjectResponse(
                    OperationId: operationId,
                    Status: OperationStatus.Failed,
                    Result: null,
                    Error: error,
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow
                );

                return Results.BadRequest(failureResponse);
            }

            var command = new DllInjectCommand
            {
                OperationId = operationId,
                ProcessId = request.ProcessId,
                ProcessName = request.ProcessName,
                DllPath = request.DllPath,
                Method = method,
                RequireElevationOverride = request.RequireElevation,
                Arguments = request.Arguments ?? new List<string>(),
                WorkingDirectory = null,
                TimeoutSeconds = null
            };

            logger.LogInformation("Starting DLL injection: ProcessId={ProcessId}, ProcessName={ProcessName}, DllPath={DllPath}, Method={Method}",
                request.ProcessId, request.ProcessName, request.DllPath, method);

            var result = await adapter.InjectAsync(command, cancellationToken, progress: null);

            var resultDto = new DllInjectResultDto(
                OperationId: result.OperationId,
                ProcessId: result.ProcessId,
                ProcessName: result.ProcessName,
                DllPath: result.DllPath,
                Method: result.Method.ToString(),
                ElevationUsed: result.ElevationUsed,
                Duration: result.Duration.ToString(),
                Warnings: result.Warnings.ToList(),
                LogExcerpt: result.LogExcerpt
            );

            var response = new DllInjectResponse(
                OperationId: operationId,
                Status: OperationStatus.Succeeded,
                Result: resultDto,
                Error: null,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            logger.LogInformation("DLL injection operation {OperationId} completed successfully in {Duration}ms (warnings: {WarningCount})",
                operationId, result.Duration.TotalMilliseconds, result.Warnings.Count);

            return Results.Ok(response);
        }
        catch (ArisException ex)
        {
            logger.LogError(ex, "DLL injection operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

            var error = new ErrorInfo(
                Code: ex.ErrorCode,
                Message: ex.Message,
                RemediationHint: ex.RemediationHint
            );

            var failureResponse = new DllInjectResponse(
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
            logger.LogError(ex, "DLL injection operation {OperationId} failed unexpectedly", operationId);

            var error = new ErrorInfo(
                Code: "UNEXPECTED_ERROR",
                Message: "An unexpected error occurred during the DLL injection operation.",
                RemediationHint: "Check the logs for more details."
            );

            var failureResponse = new DllInjectResponse(
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

    private static async Task<IResult> EjectAsync(
        DllEjectRequest request,
        IDllInjectorAdapter adapter,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;

        var logger = loggerFactory.CreateLogger("DllInjectorEndpoints");
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId
        });

        try
        {
            var command = new DllEjectCommand
            {
                OperationId = operationId,
                ProcessId = request.ProcessId,
                ProcessName = request.ProcessName,
                ModuleName = request.ModuleName,
                WorkingDirectory = null,
                TimeoutSeconds = null
            };

            logger.LogInformation("Starting DLL ejection: ProcessId={ProcessId}, ProcessName={ProcessName}, ModuleName={ModuleName}",
                request.ProcessId, request.ProcessName, request.ModuleName);

            var result = await adapter.EjectAsync(command, cancellationToken, progress: null);

            var resultDto = new DllEjectResultDto(
                OperationId: result.OperationId,
                ProcessId: result.ProcessId,
                ProcessName: result.ProcessName,
                ModuleName: result.ModuleName,
                WasLoadedBefore: result.WasLoadedBefore,
                IsUnloaded: result.IsUnloaded,
                Duration: result.Duration.ToString(),
                Warnings: result.Warnings.ToList(),
                LogExcerpt: result.LogExcerpt
            );

            var response = new DllEjectResponse(
                OperationId: operationId,
                Status: OperationStatus.Succeeded,
                Result: resultDto,
                Error: null,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.UtcNow
            );

            logger.LogInformation("DLL ejection operation {OperationId} completed successfully in {Duration}ms (warnings: {WarningCount})",
                operationId, result.Duration.TotalMilliseconds, result.Warnings.Count);

            return Results.Ok(response);
        }
        catch (ArisException ex)
        {
            logger.LogError(ex, "DLL ejection operation {OperationId} failed: {ErrorCode}", operationId, ex.ErrorCode);

            var error = new ErrorInfo(
                Code: ex.ErrorCode,
                Message: ex.Message,
                RemediationHint: ex.RemediationHint
            );

            var failureResponse = new DllEjectResponse(
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
            logger.LogError(ex, "DLL ejection operation {OperationId} failed unexpectedly", operationId);

            var error = new ErrorInfo(
                Code: "UNEXPECTED_ERROR",
                Message: "An unexpected error occurred during the DLL ejection operation.",
                RemediationHint: "Check the logs for more details."
            );

            var failureResponse = new DllEjectResponse(
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
