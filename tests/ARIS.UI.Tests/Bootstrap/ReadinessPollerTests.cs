using ARIS.UI.Bootstrap;

namespace ARIS.UI.Tests.Bootstrap;

public class ReadinessPollerTests
{
    [Fact]
    public void IsReady_StatusReady_ReturnsTrue()
    {
        // Arrange
        var response = new HealthResponse { Status = "Ready", DependenciesReady = true };

        // Act
        var result = ReadinessPoller.IsReady(response);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsReady_StatusStarting_ReturnsFalse()
    {
        // Arrange
        var response = new HealthResponse { Status = "Starting", DependenciesReady = false };

        // Act
        var result = ReadinessPoller.IsReady(response);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsReady_StatusError_ReturnsFalse()
    {
        // Arrange
        var response = new HealthResponse { Status = "Error", DependenciesReady = false, Message = "Tool validation failed" };

        // Act
        var result = ReadinessPoller.IsReady(response);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsReady_NullResponse_ReturnsFalse()
    {
        // Act
        var result = ReadinessPoller.IsReady(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsReady_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var response = new HealthResponse { Status = "READY", DependenciesReady = true };

        // Act
        var result = ReadinessPoller.IsReady(response);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsReady_EmptyStatus_ReturnsFalse()
    {
        // Arrange
        var response = new HealthResponse { Status = "", DependenciesReady = true };

        // Act
        var result = ReadinessPoller.IsReady(response);

        // Assert
        Assert.False(result);
    }
}
