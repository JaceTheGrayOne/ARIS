using Aris.Adapters.DllInjector;
using Aris.Core.Errors;
using Aris.Infrastructure.Configuration;

namespace Aris.Core.Tests.Fakes;

/// <summary>
/// Fake IProcessResolver for testing. Returns configurable process IDs and names.
/// </summary>
public class FakeProcessResolver : IProcessResolver
{
    public int? LastProcessId { get; private set; }
    public string? LastProcessName { get; private set; }
    public DllInjectorOptions? LastOptions { get; private set; }

    public int ProcessIdToReturn { get; set; } = 1234;
    public ValidationError? ErrorToThrow { get; set; }

    public int ResolveAndValidateTarget(int? processId, string? processName, DllInjectorOptions options)
    {
        LastProcessId = processId;
        LastProcessName = processName;
        LastOptions = options;

        if (ErrorToThrow != null)
        {
            throw ErrorToThrow;
        }

        return ProcessIdToReturn;
    }
}
