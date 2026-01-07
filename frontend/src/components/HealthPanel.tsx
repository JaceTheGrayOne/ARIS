import { useState, useEffect } from 'react';
import { getBackendBaseUrl } from '../config/backend';
import type { HealthResponse } from '../types/contracts';

export function HealthPanel() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchHealth = async () => {
    setLoading(true);
    setError(null);

    try {
      const baseUrl = getBackendBaseUrl();
      const response = await fetch(`${baseUrl}/health`);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const data = await response.json();
      setHealth(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(`Failed to fetch health: ${message}`);
      setHealth(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHealth();
  }, []);

  return (
    <div className="border border-gray-700 rounded p-4 bg-gray-800">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xl font-semibold">Backend Health</h2>
        <button
          onClick={fetchHealth}
          disabled={loading}
          className="px-3 py-1 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 rounded text-sm"
        >
          {loading ? 'Loading...' : 'Refresh'}
        </button>
      </div>

      {error && (
        <div className="text-red-400 mb-4 p-2 bg-red-900/20 rounded">
          {error}
        </div>
      )}

      {health && !error && (
        <div className="space-y-2 text-sm">
          <div className="grid grid-cols-2 gap-2">
            <span className="text-gray-400">Status:</span>
            <span className={
              health.status === 'Ready' ? 'text-green-400' :
              health.status === 'Starting' ? 'text-yellow-400' :
              'text-red-400'
            }>
              {health.status}
            </span>

            <span className="text-gray-400">Dependencies Ready:</span>
            <span className={health.dependenciesReady ? 'text-green-400' : 'text-red-400'}>
              {health.dependenciesReady ? 'Yes' : 'No'}
            </span>

            {health.message && (
              <>
                <span className="text-gray-400">Message:</span>
                <span className="text-white">{health.message}</span>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
