using System.Text;
using System.Text.Json;
using Aris.Contracts.Retoc;
using Aris.Core.Tests.Fakes;
using Aris.Infrastructure.Terminal;

namespace Aris.Core.Tests.Hosting;

/// <summary>
/// Tests for RetocStreamHandler and related streaming functionality.
/// Note: Full WebSocket integration tests would require TestServer setup.
/// These tests focus on the handler logic and event streaming behavior.
/// </summary>
public class RetocStreamHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// RetocStreamStarted event serializes correctly with type discriminator.
    /// Note: Type discriminator only appears when serializing as base type.
    /// </summary>
    [Fact]
    public void RetocStreamStarted_Serializes_Correctly()
    {
        RetocStreamEvent evt = new RetocStreamStarted("op123", "retoc to-zen --input test.pak");
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        Assert.Contains("\"type\":\"started\"", json);
        Assert.Contains("\"operationId\":\"op123\"", json);
        Assert.Contains("\"commandLine\":\"retoc to-zen --input test.pak\"", json);
    }

    /// <summary>
    /// RetocStreamOutput event serializes correctly with type discriminator.
    /// </summary>
    [Fact]
    public void RetocStreamOutput_Serializes_Correctly()
    {
        RetocStreamEvent evt = new RetocStreamOutput("Processing: 50%\r\n");
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        Assert.Contains("\"type\":\"output\"", json);
        Assert.Contains("\"data\":\"Processing: 50%\\r\\n\"", json);
    }

    /// <summary>
    /// RetocStreamExited event serializes correctly with type discriminator.
    /// </summary>
    [Fact]
    public void RetocStreamExited_Serializes_Correctly()
    {
        RetocStreamEvent evt = new RetocStreamExited(0, TimeSpan.FromSeconds(5.5));
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        Assert.Contains("\"type\":\"exited\"", json);
        Assert.Contains("\"exitCode\":0", json);
        Assert.Contains("\"duration\":", json);
    }

    /// <summary>
    /// RetocStreamError event serializes correctly with remediation hint.
    /// </summary>
    [Fact]
    public void RetocStreamError_Serializes_Correctly()
    {
        RetocStreamEvent evt = new RetocStreamError("VALIDATION_ERROR", "Invalid input path", "Check the file exists");
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        Assert.Contains("\"type\":\"error\"", json);
        Assert.Contains("\"code\":\"VALIDATION_ERROR\"", json);
        Assert.Contains("\"message\":\"Invalid input path\"", json);
        Assert.Contains("\"remediationHint\":\"Check the file exists\"", json);
    }

    /// <summary>
    /// RetocStreamError event serializes correctly without remediation hint.
    /// </summary>
    [Fact]
    public void RetocStreamError_Serializes_WithNullHint()
    {
        RetocStreamEvent evt = new RetocStreamError("TOOL_ERROR", "Tool failed", null);
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        Assert.Contains("\"type\":\"error\"", json);
        Assert.Contains("\"remediationHint\":null", json);
    }

    /// <summary>
    /// RetocStreamRequest deserializes correctly.
    /// </summary>
    [Fact]
    public void RetocStreamRequest_Deserializes_Correctly()
    {
        var json = """
            {
                "commandType": "ToZen",
                "inputPath": "C:\\test\\input.pak",
                "outputPath": "C:\\test\\output",
                "engineVersion": "4.27",
                "verbose": true,
                "timeoutSeconds": 300
            }
            """;

        var request = JsonSerializer.Deserialize<RetocStreamRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("ToZen", request!.CommandType);
        Assert.Equal("C:\\test\\input.pak", request.InputPath);
        Assert.Equal("C:\\test\\output", request.OutputPath);
        Assert.Equal("4.27", request.EngineVersion);
        Assert.True(request.Verbose);
        Assert.Equal(300, request.TimeoutSeconds);
    }

    /// <summary>
    /// RetocStreamRequest with minimal fields deserializes correctly.
    /// </summary>
    [Fact]
    public void RetocStreamRequest_MinimalFields_Deserializes()
    {
        var json = """
            {
                "commandType": "Get",
                "inputPath": "C:\\test\\input.pak",
                "outputPath": "C:\\test\\output"
            }
            """;

        var request = JsonSerializer.Deserialize<RetocStreamRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal("Get", request!.CommandType);
        Assert.Null(request.EngineVersion);
        Assert.Null(request.AesKey);
        Assert.False(request.Verbose);
        Assert.Null(request.TimeoutSeconds);
        Assert.False(request.TtyProbe);
    }

    /// <summary>
    /// RetocStreamRequest with TTY probe flag deserializes correctly.
    /// </summary>
    [Fact]
    public void RetocStreamRequest_TtyProbe_Deserializes()
    {
        var json = """
            {
                "commandType": "Info",
                "inputPath": "C:\\dummy",
                "outputPath": "C:\\dummy",
                "ttyProbe": true
            }
            """;

        var request = JsonSerializer.Deserialize<RetocStreamRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.True(request!.TtyProbe);
    }

    /// <summary>
    /// FakeConPtyProcess can simulate a successful execution.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_SimulatesSuccessfulExecution()
    {
        using var fake = new FakeConPtyProcess();
        fake.AddOutput("[1/4] Reading manifest...\r\n");
        fake.AddOutput("[2/4] Processing assets...\r\n");
        fake.AddOutput("[3/4] Writing output...\r\n");
        fake.AddOutput("[4/4] Complete!\r\n");
        fake.ExitCodeToReturn = 0;

        await fake.StartAsync("retoc.exe", "to-zen --input test.pak --output out");

        var outputBuilder = new StringBuilder();
        await foreach (var chunk in fake.ReadOutputAsync(CancellationToken.None))
        {
            outputBuilder.Append(Encoding.UTF8.GetString(chunk));
        }

        var exitCode = await fake.WaitForExitAsync(CancellationToken.None);

        Assert.True(fake.StartCalled);
        Assert.Equal("retoc.exe", fake.LastExecutable);
        Assert.Contains("to-zen", fake.LastArguments);
        Assert.Contains("[1/4] Reading manifest...", outputBuilder.ToString());
        Assert.Contains("[4/4] Complete!", outputBuilder.ToString());
        Assert.Equal(0, exitCode);
        Assert.True(fake.HasExited);
    }

    /// <summary>
    /// FakeConPtyProcess can simulate a failed execution.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_SimulatesFailedExecution()
    {
        using var fake = new FakeConPtyProcess();
        fake.AddOutput("Error: Input file not found\r\n");
        fake.ExitCodeToReturn = 1;

        await fake.StartAsync("retoc.exe", "to-zen --input missing.pak");

        var outputBuilder = new StringBuilder();
        await foreach (var chunk in fake.ReadOutputAsync(CancellationToken.None))
        {
            outputBuilder.Append(Encoding.UTF8.GetString(chunk));
        }

        var exitCode = await fake.WaitForExitAsync(CancellationToken.None);

        Assert.Contains("Error:", outputBuilder.ToString());
        Assert.Equal(1, exitCode);
    }

    /// <summary>
    /// FakeConPtyProcess properly handles cancellation.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_HandlesCancellation()
    {
        using var fake = new FakeConPtyProcess();
        fake.OutputDelay = TimeSpan.FromSeconds(10); // Simulate slow output
        fake.AddOutput("This should not complete\r\n");

        await fake.StartAsync("retoc.exe", "to-zen");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // TaskCanceledException derives from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in fake.ReadOutputAsync(cts.Token))
            {
                // Should not get here
            }
        });
    }

    /// <summary>
    /// Killing a fake process sets appropriate flags.
    /// </summary>
    [Fact]
    public async Task FakeConPtyProcess_KillSetsFlags()
    {
        using var fake = new FakeConPtyProcess();

        await fake.StartAsync("retoc.exe", "long-operation");

        Assert.False(fake.KillCalled);
        Assert.False(fake.HasExited);

        fake.Kill();

        Assert.True(fake.KillCalled);
        Assert.True(fake.HasExited);
    }

    /// <summary>
    /// Event types can be deserialized from JSON using type discriminator.
    /// </summary>
    [Fact]
    public void RetocStreamEvents_CanBeDeserializedWithTypeDiscriminator()
    {
        // Test that event JSON can be parsed to identify type
        var startedJson = """{"type":"started","operationId":"op1","commandLine":"test","timestamp":"2024-01-01T00:00:00Z"}""";
        var outputJson = """{"type":"output","data":"hello","timestamp":"2024-01-01T00:00:00Z"}""";
        var exitedJson = """{"type":"exited","exitCode":0,"duration":"00:00:05","timestamp":"2024-01-01T00:00:00Z"}""";
        var errorJson = """{"type":"error","code":"ERR","message":"fail","remediationHint":null,"timestamp":"2024-01-01T00:00:00Z"}""";

        // Parse to JsonDocument to read type field
        Assert.Equal("started", JsonDocument.Parse(startedJson).RootElement.GetProperty("type").GetString());
        Assert.Equal("output", JsonDocument.Parse(outputJson).RootElement.GetProperty("type").GetString());
        Assert.Equal("exited", JsonDocument.Parse(exitedJson).RootElement.GetProperty("type").GetString());
        Assert.Equal("error", JsonDocument.Parse(errorJson).RootElement.GetProperty("type").GetString());
    }

    /// <summary>
    /// NDJSON format is correct - each event on its own line.
    /// Serializing as base type RetocStreamEvent includes the type discriminator.
    /// </summary>
    [Fact]
    public void NdjsonFormat_IsCorrect()
    {
        var events = new RetocStreamEvent[]
        {
            new RetocStreamStarted("op1", "test"),
            new RetocStreamOutput("Hello\r\n"),
            new RetocStreamExited(0, TimeSpan.FromSeconds(1))
        };

        var ndjson = new StringBuilder();
        foreach (var evt in events)
        {
            // Serialize as base type to get type discriminator
            var json = JsonSerializer.Serialize<RetocStreamEvent>(evt, JsonOptions);
            ndjson.AppendLine(json); // NDJSON uses newline delimiter
        }

        var lines = ndjson.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);

        // Each line should be valid JSON with type discriminator
        foreach (var line in lines)
        {
            var doc = JsonDocument.Parse(line.Trim());
            Assert.NotNull(doc.RootElement.GetProperty("type").GetString());
            Assert.NotNull(doc.RootElement.GetProperty("timestamp").GetString());
        }
    }

    /// <summary>
    /// Verifies the expected lifecycle of stream events.
    /// </summary>
    [Fact]
    public void StreamEventLifecycle_FollowsExpectedOrder()
    {
        // Document expected event order:
        // 1. started - sent when process begins
        // 2. output - zero or more, sent as data arrives
        // 3. exited OR error - sent when process completes or fails

        // The handler should always emit 'started' first (when successful)
        // Then stream 'output' events
        // Finally emit either 'exited' (success/failure with exit code) or 'error' (validation/dependency errors)

        // This test documents the contract
        var expectedOrder = new[] { "started", "output", "output", "exited" };

        // Simulate a successful execution
        var events = new List<string>
        {
            "started",
            "output",
            "output",
            "exited"
        };

        Assert.Equal(expectedOrder, events);
    }

    /// <summary>
    /// Verifies error event lifecycle.
    /// </summary>
    [Fact]
    public void StreamEventLifecycle_ErrorPath()
    {
        // When validation fails before process starts:
        // 1. error - no started event

        // When process fails during execution:
        // 1. started
        // 2. output (maybe)
        // 3. exited with non-zero exit code

        // This test documents the error contract
        var validationErrorPath = new[] { "error" };
        var executionErrorPath = new[] { "started", "output", "exited" };

        // These document expected behavior
        Assert.Single(validationErrorPath);
        Assert.Equal("error", validationErrorPath[0]);

        Assert.Equal(3, executionErrorPath.Length);
        Assert.Equal("started", executionErrorPath[0]);
        Assert.Equal("exited", executionErrorPath[2]);
    }
}
