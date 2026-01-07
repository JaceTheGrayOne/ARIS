using Aris.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Aris.Core.Tests.Hosting;

public class ToolDocsPathResolverTests
{
    [Fact]
    public void GetDocsToolsRoot_InDevelopment_ReturnsRepoRootDocsTools()
    {
        // Arrange: ContentRootPath is src/Aris.Hosting
        var contentRootPath = @"G:\Development\ARIS(CS)\ARIS\src\Aris.Hosting";
        var env = new FakeWebHostEnvironment
        {
            ContentRootPath = contentRootPath,
            EnvironmentName = Environments.Development
        };

        // Act
        var result = ToolDocsPathResolver.GetDocsToolsRoot(env);

        // Assert: Should navigate up two levels to repo root, then into docs/tools
        var expected = @"G:\Development\ARIS(CS)\ARIS\docs\tools";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDocsToolsRoot_InProduction_ReturnsContentRootDocsTools()
    {
        // Arrange: ContentRootPath is the published app root
        var contentRootPath = @"C:\Program Files\ARIS";
        var env = new FakeWebHostEnvironment
        {
            ContentRootPath = contentRootPath,
            EnvironmentName = Environments.Production
        };

        // Act
        var result = ToolDocsPathResolver.GetDocsToolsRoot(env);

        // Assert: Should use ContentRootPath/docs/tools directly
        var expected = @"C:\Program Files\ARIS\docs\tools";
        Assert.Equal(expected, result);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Aris.Hosting";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
