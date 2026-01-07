import { useBackendStatus } from '../hooks/useBackendStatus';

export function SystemDiagnosticsPanel() {
  const backendStatus = useBackendStatus();

  const getStatusColor = () => {
    switch (backendStatus.effectiveStatus) {
      case 'ready':
        return 'text-green-400';
      case 'starting':
        return 'text-amber-400';
      case 'error':
        return 'text-red-400';
      default:
        return 'text-gray-400';
    }
  };

  const getStatusLabel = () => {
    switch (backendStatus.effectiveStatus) {
      case 'ready':
        return 'Ready';
      case 'starting':
        return 'Starting';
      case 'error':
        return 'Error';
      default:
        return 'Unknown';
    }
  };

  const formatTimestamp = (date: Date | null) => {
    if (!date) return 'Never';
    return date.toLocaleString();
  };

  return (
    <div className="space-y-6">
      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Backend Status</h2>

        {backendStatus.loading && !backendStatus.health && (
          <div className="text-gray-400 text-sm py-4">
            <div className="flex items-center space-x-2">
              <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
              <span>Checking backend...</span>
            </div>
          </div>
        )}

        {backendStatus.error && (
          <div className="mb-4 p-3 bg-red-900/20 border border-red-700 rounded">
            <p className="text-red-400 text-sm">
              Failed to refresh backend status. Status may be stale.
            </p>
            <p className="text-red-300 text-xs mt-1">{backendStatus.error}</p>
          </div>
        )}

        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <span className="text-sm text-gray-400">Effective Status</span>
              <div className={`text-lg font-semibold ${getStatusColor()}`}>
                {getStatusLabel()}
              </div>
            </div>

            <div>
              <span className="text-sm text-gray-400">Last Updated</span>
              <div className="text-sm text-white">
                {formatTimestamp(backendStatus.lastUpdated)}
              </div>
            </div>

            {backendStatus.health && (
              <>
                <div>
                  <span className="text-sm text-gray-400">Raw Status</span>
                  <div className="text-sm text-white">{backendStatus.health.status}</div>
                </div>

                <div>
                  <span className="text-sm text-gray-400">Dependencies Ready</span>
                  <div className={`text-sm ${
                    backendStatus.health.dependenciesReady ? 'text-green-400' : 'text-red-400'
                  }`}>
                    {backendStatus.health.dependenciesReady ? 'Yes' : 'No'}
                  </div>
                </div>

                {backendStatus.health.message && (
                  <div className="col-span-2">
                    <span className="text-sm text-gray-400">Status Message</span>
                    <div className="text-sm text-white">{backendStatus.health.message}</div>
                  </div>
                )}
              </>
            )}

            {!backendStatus.health && !backendStatus.loading && (
              <div className="col-span-2 text-sm text-gray-400">
                Health data not available
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Backend Information</h2>

        {backendStatus.info ? (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <span className="text-sm text-gray-400">Version</span>
                <div className="text-sm text-white font-mono">{backendStatus.info.version}</div>
              </div>

              <div>
                <span className="text-sm text-gray-400">Base URL</span>
                <div className="text-sm text-white font-mono break-all">
                  {backendStatus.info.backendBaseUrl}
                </div>
              </div>

              {backendStatus.info.ipcToken && (
                <div className="col-span-2">
                  <span className="text-sm text-gray-400">IPC Token</span>
                  <div className="text-sm text-white font-mono break-all">
                    {backendStatus.info.ipcToken}
                  </div>
                </div>
              )}
            </div>

            <div className="pt-4 border-t border-gray-700">
              <h3 className="text-lg font-semibold mb-3">Tool Versions</h3>

              {Object.keys(backendStatus.info.toolVersions).length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-gray-700">
                        <th className="text-left py-2 px-3 text-gray-400 font-semibold">Tool ID</th>
                        <th className="text-left py-2 px-3 text-gray-400 font-semibold">Version</th>
                      </tr>
                    </thead>
                    <tbody>
                      {Object.entries(backendStatus.info.toolVersions).map(([toolId, version]) => (
                        <tr key={toolId} className="border-b border-gray-700/50">
                          <td className="py-2 px-3 text-white font-mono">{toolId}</td>
                          <td className="py-2 px-3 text-gray-300">{version}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="text-sm text-gray-400 py-2">
                  No tools reported
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="text-sm text-gray-400">
            Backend information not available
          </div>
        )}
      </div>
    </div>
  );
}
