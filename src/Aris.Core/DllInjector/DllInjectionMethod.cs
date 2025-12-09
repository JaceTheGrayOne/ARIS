namespace Aris.Core.DllInjector;

/// <summary>
/// Specifies the technique used to inject a DLL into a target process.
/// </summary>
public enum DllInjectionMethod
{
    /// <summary>
    /// Uses CreateRemoteThread API to load the DLL.
    /// Most compatible but detectable by anticheat/security software.
    /// </summary>
    CreateRemoteThread,

    /// <summary>
    /// Queues an APC (Asynchronous Procedure Call) to inject the DLL.
    /// More stealthy than CreateRemoteThread but requires target thread to be alertable.
    /// </summary>
    ApcQueue,

    /// <summary>
    /// Manually maps the DLL into target memory without using LoadLibrary.
    /// Most stealthy, bypasses most DLL load detection, but more complex and fragile.
    /// </summary>
    ManualMap
}
