using System.Diagnostics;
using ARIS.UI.Bootstrap;

namespace ARIS.UI.Tests.Bootstrap;

public class BackendProcessManagerTests
{
    [Fact]
    public void BuildProcessStartInfo_SetsCreateNoWindow()
    {
        // Arrange & Act
        var psi = BackendProcessManager.BuildProcessStartInfo("C:\\test\\app.exe");

        // Assert
        Assert.True(psi.CreateNoWindow);
    }

    [Fact]
    public void BuildProcessStartInfo_SetsUseShellExecuteFalse()
    {
        // Arrange & Act
        var psi = BackendProcessManager.BuildProcessStartInfo("C:\\test\\app.exe");

        // Assert
        Assert.False(psi.UseShellExecute);
    }

    [Fact]
    public void BuildProcessStartInfo_SetsRedirectStandardOutput()
    {
        // Arrange & Act
        var psi = BackendProcessManager.BuildProcessStartInfo("C:\\test\\app.exe");

        // Assert
        Assert.True(psi.RedirectStandardOutput);
    }

    [Fact]
    public void BuildProcessStartInfo_SetsRedirectStandardError()
    {
        // Arrange & Act
        var psi = BackendProcessManager.BuildProcessStartInfo("C:\\test\\app.exe");

        // Assert
        Assert.True(psi.RedirectStandardError);
    }

    [Fact]
    public void BuildProcessStartInfo_SetsWorkingDirectory()
    {
        // Arrange & Act
        var psi = BackendProcessManager.BuildProcessStartInfo("C:\\test\\subdir\\app.exe");

        // Assert
        Assert.Equal("C:\\test\\subdir", psi.WorkingDirectory);
    }

    [Fact]
    public void BuildProcessStartInfo_SetsEnvironmentVariables()
    {
        // Arrange & Act
        var psi = BackendProcessManager.BuildProcessStartInfo("C:\\test\\app.exe");

        // Assert
        Assert.Equal("http://127.0.0.1:0", psi.Environment["ASPNETCORE_URLS"]);
        Assert.Equal("Production", psi.Environment["ASPNETCORE_ENVIRONMENT"]);
    }

    [Fact]
    public void ParseUrlFromStdout_ValidLine_ExtractsUrl()
    {
        // Arrange
        var line = "ARIS_BACKEND_URL=http://127.0.0.1:5432";

        // Act
        var url = BackendProcessManager.ParseUrlFromStdout(line);

        // Assert
        Assert.Equal("http://127.0.0.1:5432", url);
    }

    [Fact]
    public void ParseUrlFromStdout_NoPrefix_ReturnsNull()
    {
        // Arrange
        var line = "info: Application started";

        // Act
        var url = BackendProcessManager.ParseUrlFromStdout(line);

        // Assert
        Assert.Null(url);
    }

    [Fact]
    public void ParseUrlFromStdout_PartialPrefix_ReturnsNull()
    {
        // Arrange
        var line = "ARIS_BACKEND=http://127.0.0.1:5432";

        // Act
        var url = BackendProcessManager.ParseUrlFromStdout(line);

        // Assert
        Assert.Null(url);
    }

    [Fact]
    public void ParseUrlFromStdout_EmptyLine_ReturnsNull()
    {
        // Act
        var url = BackendProcessManager.ParseUrlFromStdout("");

        // Assert
        Assert.Null(url);
    }

    [Fact]
    public void ParseUrlFromStdout_NullLine_ReturnsNull()
    {
        // Act
        var url = BackendProcessManager.ParseUrlFromStdout(null);

        // Assert
        Assert.Null(url);
    }

    [Fact]
    public void ParseUrlFromStdout_TrimsWhitespace()
    {
        // Arrange
        var line = "ARIS_BACKEND_URL=http://127.0.0.1:5432  \r\n";

        // Act
        var url = BackendProcessManager.ParseUrlFromStdout(line);

        // Assert
        Assert.Equal("http://127.0.0.1:5432", url);
    }
}
