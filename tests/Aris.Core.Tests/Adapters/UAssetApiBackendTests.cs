using Aris.Adapters.UAsset;
using Aris.Core.UAsset;
using Aris.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aris.Core.Tests.Adapters;

public class UAssetApiBackendTests
{
    private readonly UAssetOptions _options;
    private readonly UAssetApiBackend _backend;

    public UAssetApiBackendTests()
    {
        _options = new UAssetOptions
        {
            DefaultUEVersion = "5.3",
            DefaultSchemaVersion = "1.0",
            MaxAssetSizeBytes = 500 * 1024 * 1024,
            DefaultTimeoutSeconds = 300
        };

        _backend = new UAssetApiBackend(
            NullLogger<UAssetApiBackend>.Instance,
            Options.Create(_options));
    }

    [Fact]
    public async Task InspectAsync_ValidAsset_ReturnsSummaryWithMetadata()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        Assert.True(File.Exists(testAssetPath), $"Test asset not found at {Path.GetFullPath(testAssetPath)}");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = testAssetPath,
            Fields = Array.Empty<string>(),
            OperationId = "test-inspect"
        };

        var result = await _backend.InspectAsync(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(testAssetPath, result.InputPath);
        Assert.NotNull(result.Summary);
        Assert.True(result.Summary.ExportCount > 0, "Expected at least one export");
        Assert.True(result.Summary.NameCount > 0, "Expected at least one name");
        Assert.NotNull(result.Summary.UEVersion);
    }

    [Fact]
    public async Task InspectAsync_WithExportsField_ReturnsExportsList()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = testAssetPath,
            Fields = new[] { "exports" },
            OperationId = "test-inspect-exports"
        };

        var result = await _backend.InspectAsync(command, CancellationToken.None);

        Assert.NotNull(result.Exports);
        Assert.True(result.Exports.Count > 0, "Expected exports to be populated");
        Assert.All(result.Exports, export => Assert.NotNull(export));
    }

    [Fact]
    public async Task InspectAsync_WithImportsField_ReturnsImportsList()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = testAssetPath,
            Fields = new[] { "imports" },
            OperationId = "test-inspect-imports"
        };

        var result = await _backend.InspectAsync(command, CancellationToken.None);

        Assert.NotNull(result.Imports);
        Assert.True(result.Imports.Count > 0, "Expected imports to be populated");
    }

    [Fact]
    public async Task InspectAsync_WithNamesField_ReturnsNamesList()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = testAssetPath,
            Fields = new[] { "names" },
            OperationId = "test-inspect-names"
        };

        var result = await _backend.InspectAsync(command, CancellationToken.None);

        Assert.NotNull(result.Names);
        Assert.True(result.Names.Count > 0, "Expected names to be populated");
    }

    [Fact]
    public async Task InspectAsync_WithMultipleFields_ReturnsAllRequestedData()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = testAssetPath,
            Fields = new[] { "exports", "imports", "names" },
            OperationId = "test-inspect-all"
        };

        var result = await _backend.InspectAsync(command, CancellationToken.None);

        Assert.NotNull(result.Exports);
        Assert.NotNull(result.Imports);
        Assert.NotNull(result.Names);
        Assert.True(result.Exports.Count > 0);
        Assert.True(result.Imports.Count > 0);
        Assert.True(result.Names.Count > 0);
    }

    [Fact]
    public async Task InspectAsync_NonExistentFile_ThrowsToolExecutionError()
    {
        var command = new UAssetInspectCommand
        {
            InputAssetPath = "nonexistent.uasset",
            Fields = Array.Empty<string>()
        };

        await Assert.ThrowsAsync<Aris.Core.Errors.ToolExecutionError>(
            () => _backend.InspectAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task InspectAsync_EmptyFieldsList_ReturnsOnlySummary()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");

        var command = new UAssetInspectCommand
        {
            InputAssetPath = testAssetPath,
            Fields = Array.Empty<string>(),
            OperationId = "test-inspect-summary-only"
        };

        var result = await _backend.InspectAsync(command, CancellationToken.None);

        Assert.NotNull(result.Summary);
        Assert.Null(result.Exports);
        Assert.Null(result.Imports);
        Assert.Null(result.Names);
    }

    [Fact]
    public async Task DeserializeAsync_ValidAsset_ProducesJsonFile()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var outputJsonPath = Path.Combine(Path.GetTempPath(), $"test-deserialize-{Guid.NewGuid()}.json");

        try
        {
            var command = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = outputJsonPath,
                IncludeBulkData = false,
                OperationId = "test-deserialize"
            };

            var result = await _backend.DeserializeAsync(command, Path.GetTempPath(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(outputJsonPath, result.OutputPath);
            Assert.True(File.Exists(outputJsonPath), $"Output JSON file not found at {outputJsonPath}");
            Assert.Contains(outputJsonPath, result.ProducedFilePaths);

            var jsonContent = File.ReadAllText(outputJsonPath);
            Assert.NotEmpty(jsonContent);
            Assert.Contains("\"Exports\"", jsonContent); // Verify it's valid UAssetAPI JSON
        }
        finally
        {
            if (File.Exists(outputJsonPath))
            {
                File.Delete(outputJsonPath);
            }
        }
    }

    [Fact]
    public async Task DeserializeAsync_ValidAsset_ReturnsCorrectMetadata()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var outputJsonPath = Path.Combine(Path.GetTempPath(), $"test-deserialize-meta-{Guid.NewGuid()}.json");

        try
        {
            var command = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = outputJsonPath,
                IncludeBulkData = false,
                OperationId = "test-deserialize-metadata"
            };

            var result = await _backend.DeserializeAsync(command, Path.GetTempPath(), CancellationToken.None);

            Assert.NotNull(result.DetectedUEVersion);
            Assert.NotEmpty(result.DetectedUEVersion);
            Assert.NotNull(result.UsedSchemaVersion);
            Assert.Equal(1, result.ProducedFilePaths.Length);
        }
        finally
        {
            if (File.Exists(outputJsonPath))
            {
                File.Delete(outputJsonPath);
            }
        }
    }

    [Fact]
    public async Task DeserializeAsync_NonExistentFile_ThrowsToolExecutionError()
    {
        var outputJsonPath = Path.Combine(Path.GetTempPath(), $"test-deserialize-error-{Guid.NewGuid()}.json");

        var command = new UAssetDeserializeCommand
        {
            InputAssetPath = "nonexistent.uasset",
            OutputJsonPath = outputJsonPath,
            IncludeBulkData = false
        };

        await Assert.ThrowsAsync<Aris.Core.Errors.ToolExecutionError>(
            () => _backend.DeserializeAsync(command, Path.GetTempPath(), CancellationToken.None));
    }

    [Fact]
    public async Task DeserializeAsync_CreatesOutputDirectory()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var outputDir = Path.Combine(Path.GetTempPath(), $"test-deserialize-dir-{Guid.NewGuid()}");
        var outputJsonPath = Path.Combine(outputDir, "output.json");

        try
        {
            var command = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = outputJsonPath,
                IncludeBulkData = false,
                OperationId = "test-deserialize-dir"
            };

            var result = await _backend.DeserializeAsync(command, Path.GetTempPath(), CancellationToken.None);

            Assert.True(Directory.Exists(outputDir), "Output directory was not created");
            Assert.True(File.Exists(outputJsonPath), "Output file was not created");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeserializeAsync_IncludeBulkDataFalse_AddsWarning()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var outputJsonPath = Path.Combine(Path.GetTempPath(), $"test-deserialize-bulkdata-{Guid.NewGuid()}.json");

        try
        {
            var command = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = outputJsonPath,
                IncludeBulkData = false,
                OperationId = "test-bulkdata-warning"
            };

            var result = await _backend.DeserializeAsync(command, Path.GetTempPath(), CancellationToken.None);

            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("IncludeBulkData") && w.Contains("SerializeJson"));
        }
        finally
        {
            if (File.Exists(outputJsonPath))
            {
                File.Delete(outputJsonPath);
            }
        }
    }

    [Fact]
    public async Task SerializeAsync_ValidJson_ProducesAssetFile()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var jsonPath = Path.Combine(Path.GetTempPath(), $"test-serialize-input-{Guid.NewGuid()}.json");
        var outputAssetPath = Path.Combine(Path.GetTempPath(), $"test-serialize-output-{Guid.NewGuid()}.uasset");

        try
        {
            var deserializeCommand = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = jsonPath,
                IncludeBulkData = true,
                OperationId = "test-serialize-prep"
            };

            await _backend.DeserializeAsync(deserializeCommand, Path.GetTempPath(), CancellationToken.None);

            var serializeCommand = new UAssetSerializeCommand
            {
                InputJsonPath = jsonPath,
                OutputAssetPath = outputAssetPath,
                OperationId = "test-serialize"
            };

            var result = await _backend.SerializeAsync(serializeCommand, Path.GetTempPath(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(outputAssetPath, result.OutputPath);
            Assert.True(File.Exists(outputAssetPath), $"Output asset file not found at {outputAssetPath}");
            Assert.Contains(outputAssetPath, result.ProducedFilePaths);

            var fileInfo = new FileInfo(outputAssetPath);
            Assert.True(fileInfo.Length > 0, "Output asset file is empty");
        }
        finally
        {
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(outputAssetPath)) File.Delete(outputAssetPath);

            var uexpPath = Path.ChangeExtension(outputAssetPath, ".uexp");
            var ubulkPath = Path.ChangeExtension(outputAssetPath, ".ubulk");
            if (File.Exists(uexpPath)) File.Delete(uexpPath);
            if (File.Exists(ubulkPath)) File.Delete(ubulkPath);
        }
    }

    [Fact]
    public async Task SerializeAsync_NonExistentJsonFile_ThrowsToolExecutionError()
    {
        var outputAssetPath = Path.Combine(Path.GetTempPath(), $"test-serialize-error-{Guid.NewGuid()}.uasset");

        var command = new UAssetSerializeCommand
        {
            InputJsonPath = "nonexistent.json",
            OutputAssetPath = outputAssetPath
        };

        await Assert.ThrowsAsync<Aris.Core.Errors.ToolExecutionError>(
            () => _backend.SerializeAsync(command, Path.GetTempPath(), CancellationToken.None));
    }

    [Fact]
    public async Task SerializeAsync_CreatesOutputDirectory()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var jsonPath = Path.Combine(Path.GetTempPath(), $"test-serialize-dir-input-{Guid.NewGuid()}.json");
        var outputDir = Path.Combine(Path.GetTempPath(), $"test-serialize-dir-{Guid.NewGuid()}");
        var outputAssetPath = Path.Combine(outputDir, "output.uasset");

        try
        {
            var deserializeCommand = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = jsonPath,
                IncludeBulkData = true,
                OperationId = "test-serialize-dir-prep"
            };

            await _backend.DeserializeAsync(deserializeCommand, Path.GetTempPath(), CancellationToken.None);

            var serializeCommand = new UAssetSerializeCommand
            {
                InputJsonPath = jsonPath,
                OutputAssetPath = outputAssetPath,
                OperationId = "test-serialize-dir"
            };

            var result = await _backend.SerializeAsync(serializeCommand, Path.GetTempPath(), CancellationToken.None);

            Assert.True(Directory.Exists(outputDir), "Output directory was not created");
            Assert.True(File.Exists(outputAssetPath), "Output asset file was not created");
        }
        finally
        {
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SerializeAsync_RoundTrip_ProducesValidAsset()
    {
        var testAssetPath = Path.Combine("TestAssets", "Assault_M1A1Thompson_WW2_DrumSuppressor.uasset");
        var jsonPath = Path.Combine(Path.GetTempPath(), $"test-roundtrip-{Guid.NewGuid()}.json");
        var outputAssetPath = Path.Combine(Path.GetTempPath(), $"test-roundtrip-{Guid.NewGuid()}.uasset");

        try
        {
            var deserializeCommand = new UAssetDeserializeCommand
            {
                InputAssetPath = testAssetPath,
                OutputJsonPath = jsonPath,
                IncludeBulkData = true,
                OperationId = "test-roundtrip-deserialize"
            };

            var deserializeResult = await _backend.DeserializeAsync(deserializeCommand, Path.GetTempPath(), CancellationToken.None);
            Assert.True(File.Exists(jsonPath), "Deserialize did not produce JSON file");

            var serializeCommand = new UAssetSerializeCommand
            {
                InputJsonPath = jsonPath,
                OutputAssetPath = outputAssetPath,
                OperationId = "test-roundtrip-serialize"
            };

            var serializeResult = await _backend.SerializeAsync(serializeCommand, Path.GetTempPath(), CancellationToken.None);
            Assert.True(File.Exists(outputAssetPath), "Serialize did not produce asset file");

            var inspectCommand = new UAssetInspectCommand
            {
                InputAssetPath = outputAssetPath,
                Fields = Array.Empty<string>(),
                OperationId = "test-roundtrip-inspect"
            };

            var inspectResult = await _backend.InspectAsync(inspectCommand, CancellationToken.None);

            Assert.NotNull(inspectResult);
            Assert.NotNull(inspectResult.Summary);
            Assert.True(inspectResult.Summary.ExportCount > 0, "Round-trip asset has zero exports");
            Assert.True(inspectResult.Summary.ImportCount > 0, "Round-trip asset has zero imports");
            Assert.True(inspectResult.Summary.NameCount > 0, "Round-trip asset has zero names");
            Assert.NotNull(inspectResult.Summary.UEVersion);
        }
        finally
        {
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(outputAssetPath)) File.Delete(outputAssetPath);

            var uexpPath = Path.ChangeExtension(outputAssetPath, ".uexp");
            var ubulkPath = Path.ChangeExtension(outputAssetPath, ".ubulk");
            if (File.Exists(uexpPath)) File.Delete(uexpPath);
            if (File.Exists(ubulkPath)) File.Delete(ubulkPath);
        }
    }
}
