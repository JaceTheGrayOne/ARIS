using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Aris.Adapters.Retoc;
using Aris.Contracts;
using Aris.Contracts.Retoc;
using Aris.Core.Errors;
using Aris.Core.Retoc;
using Aris.Infrastructure.Terminal;
using Microsoft.Extensions.Logging;

namespace Aris.Hosting.Endpoints;

/// <summary>
/// Handles WebSocket connections for streaming Retoc execution with ConPTY terminal output.
/// </summary>
public sealed class RetocStreamHandler
{
    private readonly IRetocAdapter _retocAdapter;
    private readonly Func<IConPtyProcess> _conPtyFactory;
    private readonly ILogger<RetocStreamHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RetocStreamHandler(
        IRetocAdapter retocAdapter,
        Func<IConPtyProcess> conPtyFactory,
        ILogger<RetocStreamHandler> logger)
    {
        _retocAdapter = retocAdapter;
        _conPtyFactory = conPtyFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Handles a WebSocket connection for Retoc streaming.
    /// </summary>
    public async Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("WebSocket connected for Retoc streaming: {OperationId}", operationId);

        try
        {
            // Step 1: Receive the request message
            var request = await ReceiveRequestAsync(webSocket, cancellationToken);
            if (request == null)
            {
                await SendErrorAsync(webSocket, "INVALID_REQUEST", "Failed to parse request", null, cancellationToken);
                return;
            }

            _logger.LogDebug("Received stream request: CommandType={CommandType}, Input={InputPath}",
                request.CommandType, request.InputPath);

            // Step 2: Handle TTY probe mode
            if (request.TtyProbe)
            {
                await RunTtyProbeAsync(webSocket, operationId, request, cancellationToken);
                return;
            }

            // Step 3: Build and execute the Retoc command
            await ExecuteRetocStreamAsync(webSocket, operationId, request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("WebSocket operation cancelled: {OperationId}", operationId);
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogDebug("WebSocket closed prematurely: {OperationId}", operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket handler: {OperationId}", operationId);
            try
            {
                await SendErrorAsync(webSocket, "UNEXPECTED_ERROR", ex.Message, null, cancellationToken);
            }
            catch
            {
                // Ignore errors sending error response
            }
        }
        finally
        {
            _logger.LogInformation("WebSocket connection closed: {OperationId}", operationId);
        }
    }

    private async Task<RetocStreamRequest?> ReceiveRequestAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await webSocket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);

        try
        {
            return await JsonSerializer.DeserializeAsync<RetocStreamRequest>(ms, _jsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse WebSocket request");
            return null;
        }
    }

    private async Task ExecuteRetocStreamAsync(
        WebSocket webSocket,
        string operationId,
        RetocStreamRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate and parse command type
            if (!Enum.TryParse<RetocCommandType>(request.CommandType, ignoreCase: true, out var commandType))
            {
                await SendErrorAsync(webSocket, "VALIDATION_ERROR",
                    $"Invalid command type '{request.CommandType}'",
                    $"Valid types: {string.Join(", ", Enum.GetNames<RetocCommandType>())}",
                    cancellationToken);
                return;
            }

            // Build the command
            var command = new RetocCommand
            {
                OperationId = operationId,
                CommandType = commandType,
                InputPath = request.InputPath,
                OutputPath = request.OutputPath,
                Version = request.EngineVersion,
                AesKey = request.AesKey,
                ContainerHeaderVersion = request.ContainerHeaderVersion != null
                    ? Enum.Parse<RetocContainerHeaderVersion>(request.ContainerHeaderVersion, ignoreCase: true)
                    : null,
                TocVersion = request.TocVersion != null
                    ? Enum.Parse<RetocTocVersion>(request.TocVersion, ignoreCase: true)
                    : null,
                ChunkId = request.ChunkId,
                TimeoutSeconds = request.TimeoutSeconds
            };

            // Get the executable path and arguments from the adapter
            var (executablePath, arguments, commandLine) = _retocAdapter.BuildCommand(command);

            _logger.LogInformation("Starting Retoc stream: {OperationId}, Command: {CommandLine}",
                operationId, commandLine);

            // Send started event
            await SendEventAsync(webSocket, new RetocStreamStarted(operationId, commandLine), cancellationToken);

            // Create and start the ConPTY process
            using var conPtyProcess = _conPtyFactory();
            var argumentsString = string.Join(" ", arguments);

            await conPtyProcess.StartAsync(
                executablePath,
                argumentsString,
                workingDirectory: null,
                terminalWidth: 120,
                terminalHeight: 30);

            // Create a cancellation token that fires when WebSocket is closed
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start a task to monitor for WebSocket messages (cancel command)
            var monitorTask = MonitorWebSocketAsync(webSocket, conPtyProcess, linkedCts, cancellationToken);

            // Stream output to the WebSocket
            await foreach (var data in conPtyProcess.ReadOutputAsync(linkedCts.Token))
            {
                var text = Encoding.UTF8.GetString(data);
                await SendEventAsync(webSocket, new RetocStreamOutput(text), linkedCts.Token);
            }

            // Wait for the process to exit
            var exitCode = await conPtyProcess.WaitForExitAsync(linkedCts.Token);

            stopwatch.Stop();

            // Send exited event
            await SendEventAsync(webSocket, new RetocStreamExited(exitCode, stopwatch.Elapsed), linkedCts.Token);

            _logger.LogInformation(
                "Retoc stream completed: {OperationId}, ExitCode={ExitCode}, Duration={Duration}",
                operationId, exitCode, stopwatch.Elapsed);
        }
        catch (DependencyMissingError ex)
        {
            await SendErrorAsync(webSocket, "DEPENDENCY_MISSING", ex.Message, ex.RemediationHint, cancellationToken);
        }
        catch (ValidationError ex)
        {
            await SendErrorAsync(webSocket, "VALIDATION_ERROR", ex.Message, ex.RemediationHint, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected or cancelled
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Retoc stream: {OperationId}", operationId);
            await SendErrorAsync(webSocket, "TOOL_EXECUTION_ERROR", ex.Message, null, cancellationToken);
        }
    }

    private async Task MonitorWebSocketAsync(
        WebSocket webSocket,
        IConPtyProcess process,
        CancellationTokenSource cts,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogDebug("Client requested close, killing process");
                    process.Kill();
                    cts.Cancel();
                    break;
                }

                // Check for cancel command
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (message.Contains("\"action\":\"cancel\"", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Cancel command received, killing process");
                        process.Kill();
                        cts.Cancel();
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (WebSocketException)
        {
            // WebSocket closed, kill process
            process.Kill();
            cts.Cancel();
        }
    }

    private async Task RunTtyProbeAsync(
        WebSocket webSocket,
        string operationId,
        RetocStreamRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running TTY probe: {OperationId}", operationId);

        await SendEventAsync(webSocket, new RetocStreamStarted(operationId, "TTY Probe"), cancellationToken);

        using var conPtyProcess = _conPtyFactory();

        try
        {
            await conPtyProcess.StartAsync(
                "cmd.exe",
                "/c \"echo === ARIS TTY Probe === && echo. && echo STD_OUTPUT_HANDLE test && echo. && echo === End Probe ===\"",
                workingDirectory: null,
                terminalWidth: 120,
                terminalHeight: 30);

            var stopwatch = Stopwatch.StartNew();

            await foreach (var data in conPtyProcess.ReadOutputAsync(cancellationToken))
            {
                var text = Encoding.UTF8.GetString(data);
                await SendEventAsync(webSocket, new RetocStreamOutput(text), cancellationToken);
            }

            var exitCode = await conPtyProcess.WaitForExitAsync(cancellationToken);
            stopwatch.Stop();

            // Add probe information
            var probeInfo = new StringBuilder();
            probeInfo.AppendLine();
            probeInfo.AppendLine("=== ConPTY Probe Results ===");
            probeInfo.AppendLine($"Process ID: {conPtyProcess.ProcessId}");
            probeInfo.AppendLine($"Exit Code: {exitCode}");
            probeInfo.AppendLine($"Duration: {stopwatch.Elapsed}");
            probeInfo.AppendLine();
            probeInfo.AppendLine("If indicatif progress bars are not appearing:");
            probeInfo.AppendLine("1. Verify GetConsoleMode succeeds on stdout handle");
            probeInfo.AppendLine("2. Verify GetFileType returns FILE_TYPE_CHAR (0x0002)");
            probeInfo.AppendLine("3. Ensure TERM environment variable is set");
            probeInfo.AppendLine("=== End Probe ===");

            await SendEventAsync(webSocket, new RetocStreamOutput(probeInfo.ToString()), cancellationToken);
            await SendEventAsync(webSocket, new RetocStreamExited(exitCode, stopwatch.Elapsed), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTY probe failed: {OperationId}", operationId);
            await SendErrorAsync(webSocket, "TTY_PROBE_ERROR", ex.Message, null, cancellationToken);
        }
    }

    private async Task SendEventAsync(WebSocket webSocket, RetocStreamEvent evt, CancellationToken cancellationToken)
    {
        if (webSocket.State != WebSocketState.Open)
            return;

        // Serialize as base type to include the type discriminator
        var json = JsonSerializer.Serialize(evt, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json + "\n"); // NDJSON format

        await webSocket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    private async Task SendErrorAsync(
        WebSocket webSocket,
        string code,
        string message,
        string? remediationHint,
        CancellationToken cancellationToken)
    {
        var errorEvent = new RetocStreamError(code, message, remediationHint);
        await SendEventAsync(webSocket, errorEvent, cancellationToken);
    }
}
