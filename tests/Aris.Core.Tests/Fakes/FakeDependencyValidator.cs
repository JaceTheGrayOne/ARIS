using Aris.Infrastructure.Tools;

namespace Aris.Core.Tests.Fakes;

/// <summary>
/// Fake IDependencyValidator for testing. Returns configurable validation results.
/// </summary>
public class FakeDependencyValidator : IDependencyValidator
{
    public DependencyValidationResult ValidationResultToReturn { get; set; } = new DependencyValidationResult
    {
        ToolResults = new List<ToolValidationResult>
        {
            new ToolValidationResult
            {
                ToolId = "retoc",
                Status = DependencyStatus.Valid,
                ExpectedPath = "C:\\fake\\retoc.exe",
                ExpectedHash = "fakehash",
                ActualHash = "fakehash"
            }
        }
    };

    public ToolValidationResult ToolResultToReturn { get; set; } = new ToolValidationResult
    {
        ToolId = "retoc",
        Status = DependencyStatus.Valid,
        ExpectedPath = "C:\\fake\\retoc.exe",
        ExpectedHash = "fakehash",
        ActualHash = "fakehash"
    };

    public Task<DependencyValidationResult> ValidateAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResultToReturn);
    }

    public Task<ToolValidationResult> ValidateToolAsync(string toolId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ToolResultToReturn);
    }
}
