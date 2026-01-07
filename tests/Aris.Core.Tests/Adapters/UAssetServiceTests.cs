using Aris.Adapters.UAsset;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using System.Linq;

namespace Aris.Core.Tests.Adapters;

public class UAssetServiceTests : IDisposable
{
    private readonly UAssetOptions _options;
    private readonly string _tempTestDir;
    private readonly string _tempInputPath;
    private readonly UAssetService _service;

    public UAssetServiceTests()
    {
        _options = new UAssetOptions
        {
            DefaultUEVersion = "5.3",
            DefaultSchemaVersion = "1.0",
            MaxAssetSizeBytes = 500 * 1024 * 1024,
            DefaultTimeoutSeconds = 300,
            MaxLogBytes = 5 * 1024 * 1024,
            KeepTempOnFailure = false
        };

        _tempTestDir = Path.Combine(Path.GetTempPath(), "aris-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);

        _tempInputPath = Path.Combine(_tempTestDir, "input");
        Directory.CreateDirectory(_tempInputPath);

        var backend = new StubUAssetBackend();
        _service = new UAssetService(
            backend,
            new NullLogger<UAssetService>(),
            Options.Create(_options));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempTestDir))
        {
            try
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public async Task SerializeAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var inputJsonPath = Path.Combine(_tempInputPath, "test.json");
        var outputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputJsonPath, "{ \"test\": \"data\" }");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputJsonPath,
            OutputAssetPath = outputAssetPath,
            OperationId = "test-op-123"
        };

        var result = await _service.SerializeAsync(command);

        Assert.Equal(UAssetOperation.Serialize, result.Operation);
        Assert.Equal(inputJsonPath, result.InputPath);
        Assert.Equal(outputAssetPath, result.OutputPath);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.NotEmpty(result.ProducedFiles);
    }

    [Fact]
    public async Task DeserializeAsync_HappyPath_ReturnsSuccessfulResult()
    {
        var inputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        var outputJsonPath = Path.Combine(_tempInputPath, "test.json");
        File.WriteAllText(inputAssetPath, "fake asset content");

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = inputAssetPath,
            OutputJsonPath = outputJsonPath,
            OperationId = "test-op-456"
        };

        var result = await _service.DeserializeAsync(command);

        Assert.Equal(UAssetOperation.Deserialize, result.Operation);
        Assert.Equal(inputAssetPath, result.InputPath);
        Assert.Equal(outputJsonPath, result.OutputPath);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.NotEmpty(result.ProducedFiles);
    }

    [Fact]
    public async Task InspectAsync_HappyPath_ReturnsInspection()
    {
        var inputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputAssetPath, "fake asset content");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = inputAssetPath,
            Fields = new[] { "exports", "imports" },
            OperationId = "test-op-789"
        };

        var inspection = await _service.InspectAsync(command);

        Assert.Equal(inputAssetPath, inspection.InputPath);
        Assert.NotNull(inspection.Summary);
        Assert.NotNull(inspection.Exports);
        Assert.NotNull(inspection.Imports);
    }

    [Fact]
    public async Task SerializeAsync_MissingInputFile_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = Path.Combine(_tempInputPath, "nonexistent.json"),
            OutputAssetPath = Path.Combine(_tempInputPath, "test.uasset")
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() => _service.SerializeAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task DeserializeAsync_MissingInputFile_ThrowsValidationError()
    {
        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = Path.Combine(_tempInputPath, "nonexistent.uasset"),
            OutputJsonPath = Path.Combine(_tempInputPath, "test.json")
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() => _service.DeserializeAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task SerializeAsync_EmitsProgressEvents()
    {
        var inputJsonPath = Path.Combine(_tempInputPath, "test.json");
        var outputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputJsonPath, "{ \"test\": \"data\" }");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputJsonPath,
            OutputAssetPath = outputAssetPath
        };

        var progressEvents = new System.Collections.Concurrent.ConcurrentQueue<ProgressEvent>();
        IProgress<ProgressEvent> progress = new Progress<ProgressEvent>(e => progressEvents.Enqueue(e));

        await _service.SerializeAsync(command, default, progress);

        // bounded wait for Progress<T> callbacks to flush (avoid race)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 250 && !progressEvents.Any(e => e.Step == "complete"))
        {
            await Task.Delay(10);
        }

        var events = progressEvents.ToArray();

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Step == "opening");
        Assert.Contains(events, e => e.Step == "finalizing");
        Assert.Contains(events, e => e.Step == "complete");

    }

    [Fact]
    public async Task DeserializeAsync_EmitsProgressEvents()
    {
        var inputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        var outputJsonPath = Path.Combine(_tempInputPath, "test.json");
        File.WriteAllText(inputAssetPath, "fake asset content");

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = inputAssetPath,
            OutputJsonPath = outputJsonPath
        };

        var progressEvents = new System.Collections.Concurrent.ConcurrentQueue<ProgressEvent>();
        IProgress<ProgressEvent> progress = new Progress<ProgressEvent>(e => progressEvents.Enqueue(e));

        await _service.DeserializeAsync(command, default, progress);

        // bounded wait for Progress<T> callbacks to flush (avoid race)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 250 && !progressEvents.Any(e => e.Step == "complete"))
        {
            await Task.Delay(10);
        }

        var events = progressEvents.ToArray();

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Step == "opening");
        Assert.Contains(events, e => e.Step == "finalizing");
        Assert.Contains(events, e => e.Step == "complete");

    }

    [Fact]
    public async Task SerializeAsync_FileSizeExceedsLimit_ThrowsValidationError()
    {
        _options.MaxAssetSizeBytes = 100; // Set very small limit

        var inputJsonPath = Path.Combine(_tempInputPath, "large.json");
        File.WriteAllText(inputJsonPath, new string('x', 1000)); // Create file larger than limit

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputJsonPath,
            OutputAssetPath = Path.Combine(_tempInputPath, "test.uasset")
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() => _service.SerializeAsync(command));
        Assert.Contains("exceeds maximum size", ex.Message);
    }

    [Fact]
    public async Task SerializeAsync_CreatesTemporaryStagingDirectory()
    {
        var operationId = "test-staging-" + Guid.NewGuid().ToString("N");
        var inputJsonPath = Path.Combine(_tempInputPath, "test.json");
        var outputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputJsonPath, "{ \"test\": \"data\" }");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputJsonPath,
            OutputAssetPath = outputAssetPath,
            OperationId = operationId
        };

        await _service.SerializeAsync(command);

        // Staging directory is now created in system temp directory
        var expectedStagingPath = Path.Combine(Path.GetTempPath(), "aris", "temp", $"uasset-{operationId}");
        Assert.True(Directory.Exists(expectedStagingPath), $"Expected staging directory at {expectedStagingPath}");
    }

    [Fact]
    public async Task SerializeAsync_ReturnsUEVersionFromCommand()
    {
        var inputJsonPath = Path.Combine(_tempInputPath, "test.json");
        var outputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputJsonPath, "{ \"test\": \"data\" }");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputJsonPath,
            OutputAssetPath = outputAssetPath,
            UEVersion = "4.27"
        };

        var result = await _service.SerializeAsync(command);

        Assert.Equal("4.27", result.UEVersion);
    }

    [Fact]
    public async Task SerializeAsync_EmptyInputPath_ThrowsValidationError()
    {
        var command = new UAssetSerializeCommand
        {
            InputJsonPath = "",
            OutputAssetPath = Path.Combine(_tempInputPath, "test.uasset")
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() => _service.SerializeAsync(command));
        Assert.Contains("InputJsonPath", ex.FieldName);
    }

    [Fact]
    public async Task DeserializeAsync_EmptyOutputPath_ThrowsValidationError()
    {
        var inputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputAssetPath, "fake content");

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = inputAssetPath,
            OutputJsonPath = ""
        };

        var ex = await Assert.ThrowsAsync<ValidationError>(() => _service.DeserializeAsync(command));
        Assert.Contains("OutputJsonPath", ex.FieldName);
    }
}
