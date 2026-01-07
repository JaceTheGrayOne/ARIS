using System.IO;
using System.Security.Cryptography;
using System.Text;
using ARIS.UI.Bootstrap;

namespace ARIS.UI.Tests.Bootstrap;

public class PayloadExtractorTests
{
    [Fact]
    public async Task ComputePayloadHash_SameInput_ReturnsSameHash()
    {
        // Arrange
        var content = "test payload content";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash1 = await PayloadExtractor.ComputePayloadHashAsync(stream1);
        var hash2 = await PayloadExtractor.ComputePayloadHashAsync(stream2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputePayloadHash_DifferentInput_ReturnsDifferentHash()
    {
        // Arrange
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("content A"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("content B"));

        // Act
        var hash1 = await PayloadExtractor.ComputePayloadHashAsync(stream1);
        var hash2 = await PayloadExtractor.ComputePayloadHashAsync(stream2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ParseLockFile_ValidJson_ReturnsLockFile()
    {
        // Arrange
        var json = """
            {
                "payloadHash": "abc123",
                "extractedAt": "2025-01-01T00:00:00Z",
                "version": "1.0.0"
            }
            """;

        // Act
        var lockFile = PayloadExtractor.ParseLockFile(json);

        // Assert
        Assert.NotNull(lockFile);
        Assert.Equal("abc123", lockFile.PayloadHash);
        Assert.Equal("1.0.0", lockFile.Version);
    }

    [Fact]
    public void ParseLockFile_InvalidJson_ReturnsNull()
    {
        // Arrange
        var json = "{ not valid json";

        // Act
        var lockFile = PayloadExtractor.ParseLockFile(json);

        // Assert
        Assert.Null(lockFile);
    }

    [Fact]
    public void ParseLockFile_EmptyString_ReturnsNull()
    {
        // Act
        var lockFile = PayloadExtractor.ParseLockFile("");

        // Assert
        Assert.Null(lockFile);
    }

    [Fact]
    public void ParseLockFile_NullString_ReturnsNull()
    {
        // Act
        var lockFile = PayloadExtractor.ParseLockFile(null!);

        // Assert
        Assert.Null(lockFile);
    }

    [Fact]
    public void IsPayloadUpToDate_HashMatches_ReturnsTrue()
    {
        // Arrange
        var extractor = new PayloadExtractor();
        var lockFile = new PayloadLockFile { PayloadHash = "abc123" };

        // Act
        var result = extractor.IsPayloadUpToDate("abc123", lockFile);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPayloadUpToDate_HashMismatch_ReturnsFalse()
    {
        // Arrange
        var extractor = new PayloadExtractor();
        var lockFile = new PayloadLockFile { PayloadHash = "abc123" };

        // Act
        var result = extractor.IsPayloadUpToDate("different-hash", lockFile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPayloadUpToDate_NoLockFile_ReturnsFalse()
    {
        // Arrange
        var extractor = new PayloadExtractor();

        // Act
        var result = extractor.IsPayloadUpToDate("abc123", null);

        // Assert
        Assert.False(result);
    }
}
