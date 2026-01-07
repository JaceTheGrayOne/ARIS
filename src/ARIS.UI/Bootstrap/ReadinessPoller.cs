using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ARIS.UI.Bootstrap;

/// <summary>
/// Polls the backend /health endpoint until it reports ready.
/// </summary>
public sealed class ReadinessPoller : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);

    private readonly HttpClient _httpClient;
    private bool _disposed;

    public ReadinessPoller()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    /// <summary>
    /// Checks if the health response indicates ready status.
    /// </summary>
    public static bool IsReady(HealthResponse? response)
    {
        if (response == null)
            return false;

        return string.Equals(response.Status, "Ready", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Polls the backend until it reports ready.
    /// </summary>
    /// <param name="backendUrl">The backend base URL</param>
    /// <param name="timeout">Maximum time to wait for readiness</param>
    /// <param name="pollInterval">Time between poll attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WaitForReadyAsync(
        string backendUrl,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= DefaultTimeout;
        pollInterval ??= DefaultPollInterval;

        var healthUrl = $"{backendUrl.TrimEnd('/')}/health";
        var startTime = DateTime.UtcNow;
        string? lastStatus = null;

        while (DateTime.UtcNow - startTime < timeout.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await _httpClient.GetFromJsonAsync<HealthResponse>(healthUrl, cancellationToken);
                lastStatus = response?.Status;

                if (IsReady(response))
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Backend not yet listening - this is expected during startup
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // HTTP timeout - backend might be busy
            }

            await Task.Delay(pollInterval.Value, cancellationToken);
        }

        throw new BackendReadinessTimeoutException(timeout.Value, lastStatus);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _httpClient.Dispose();
    }
}

/// <summary>
/// Health endpoint response DTO.
/// </summary>
public sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("dependenciesReady")]
    public bool DependenciesReady { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
