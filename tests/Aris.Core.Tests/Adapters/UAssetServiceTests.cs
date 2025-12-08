using Aris.Adapters.UAsset;
using Aris.Core.Errors;
using Aris.Core.Models;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.Adapters;

public class UAssetServiceTests : IDisposable
{
    private readonly UAssetOptions _options;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly string _tempWorkspacePath;
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

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), "aris-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspacePath);

        _tempInputPath = Path.Combine(_tempWorkspacePath, "input");
        Directory.CreateDirectory(_tempInputPath);

        _workspaceOptions = new WorkspaceOptions
        {
            DefaultWorkspacePath = _tempWorkspacePath
        };

        var backend = new StubUAssetBackend();
        _service = new UAssetService(
            backend,
            new NullLogger<UAssetService>(),
            Options.Create(_options),
            Options.Create(_workspaceOptions));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWorkspacePath))
        {
            try
            {
                Directory.Delete(_tempWorkspacePath, recursive: true);
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

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _service.SerializeAsync(command, default, progress);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.Step == "opening");
        Assert.Contains(progressEvents, e => e.Step == "parsing");
        Assert.Contains(progressEvents, e => e.Step == "hashing");
        Assert.Contains(progressEvents, e => e.Step == "finalizing");
        Assert.Contains(progressEvents, e => e.Step == "complete");
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

        var progressEvents = new List<ProgressEvent>();
        var progress = new Progress<ProgressEvent>(e => progressEvents.Add(e));

        await _service.DeserializeAsync(command, default, progress);

        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, e => e.Step == "opening");
        Assert.Contains(progressEvents, e => e.Step == "parsing");
        Assert.Contains(progressEvents, e => e.Step == "hashing");
        Assert.Contains(progressEvents, e => e.Step == "finalizing");
        Assert.Contains(progressEvents, e => e.Step == "complete");
    }

    [Fact]
    public async Task SerializeAsync_WritesOperationLogFile()
    {
        var operationId = "test-log-" + Guid.NewGuid().ToString("N");
        var inputJsonPath = Path.Combine(_tempInputPath, "test.json");
        var outputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        File.WriteAllText(inputJsonPath, "{ \"test\": \"data\" }");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = inputJsonPath,
            OutputAssetPath = outputAssetPath,
            UEVersion = "5.3",
            SchemaVersion = "1.0",
            OperationId = operationId
        };

        await _service.SerializeAsync(command);

        var expectedLogPath = Path.Combine(_tempWorkspacePath, "logs", $"uasset-{operationId}.log");
        Assert.True(File.Exists(expectedLogPath), $"Expected log file at {expectedLogPath}");

        var logContent = File.ReadAllText(expectedLogPath);
        Assert.Contains(operationId, logContent);
        Assert.Contains("Operation: Serialize", logContent);
        Assert.Contains("UE Version: 5.3", logContent);
        Assert.Contains("Schema Version: 1.0", logContent);
        Assert.Contains(inputJsonPath, logContent);
        Assert.Contains(outputAssetPath, logContent);
    }

    [Fact]
    public async Task DeserializeAsync_WritesOperationLogFile()
    {
        var operationId = "test-deserialize-log-" + Guid.NewGuid().ToString("N");
        var inputAssetPath = Path.Combine(_tempInputPath, "test.uasset");
        var outputJsonPath = Path.Combine(_tempInputPath, "test.json");
        File.WriteAllText(inputAssetPath, "fake asset content");

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = inputAssetPath,
            OutputJsonPath = outputJsonPath,
            UEVersion = "5.3",
            SchemaVersion = "1.0",
            OperationId = operationId
        };

        await _service.DeserializeAsync(command);

        var expectedLogPath = Path.Combine(_tempWorkspacePath, "logs", $"uasset-{operationId}.log");
        Assert.True(File.Exists(expectedLogPath), $"Expected log file at {expectedLogPath}");

        var logContent = File.ReadAllText(expectedLogPath);
        Assert.Contains(operationId, logContent);
        Assert.Contains("Operation: Deserialize", logContent);
        Assert.Contains(inputAssetPath, logContent);
        Assert.Contains(outputJsonPath, logContent);
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

        var expectedStagingPath = Path.Combine(_tempWorkspacePath, "temp", $"uasset-{operationId}");
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
