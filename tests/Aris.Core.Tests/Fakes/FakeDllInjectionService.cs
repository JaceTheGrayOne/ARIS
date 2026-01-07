using Aris.Core.DllInjector;

namespace Aris.Core.Tests.Fakes;

public class FakeDllInjectionService : IDllInjectionService
{
    public DllInjectionResult? ResultToReturn { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public int LastProcessId { get; private set; }
    public string? LastDllPath { get; private set; }

    public Task<DllInjectionResult> InjectAsync(int processId, string dllPath, CancellationToken cancellationToken = default)
    {
        LastProcessId = processId;
        LastDllPath = dllPath;

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        if (ResultToReturn != null)
        {
            return Task.FromResult(ResultToReturn);
        }

        // Default successful injection
        return Task.FromResult(new DllInjectionResult
        {
            Success = true,
            ProcessId = processId,
            DllPath = dllPath,
            LoadedModuleAddress = new IntPtr(0x12340000), // Fake module base
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            Win32ErrorCode = null,
            RequiresElevation = false
        });
    }
}
