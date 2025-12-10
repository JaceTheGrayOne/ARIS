import { useState } from 'react';
import { getBackendBaseUrl } from '../config/backend';
import type { RetocConvertResponse } from '../types/contracts';
import { OperationStatus } from '../types/contracts';

export function RetocTestPanel() {
  const [inputPath, setInputPath] = useState('C:/fake/input.pak');
  const [outputPath, setOutputPath] = useState('C:/fake/out');
  const [mode, setMode] = useState('PakToIoStore');
  const [loading, setLoading] = useState(false);
  const [response, setResponse] = useState<RetocConvertResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      const baseUrl = getBackendBaseUrl();
      const res = await fetch(`${baseUrl}/api/retoc/convert`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          inputPath,
          outputPath,
          mode,
        }),
      });

      const data = await res.json();
      setResponse(data);

      if (!res.ok) {
        setError(`HTTP ${res.status}: Request failed`);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(`Network error: ${message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="border border-gray-700 rounded p-4 bg-gray-800">
      <h2 className="text-xl font-semibold mb-4">Retoc Test</h2>

      <form onSubmit={handleSubmit} className="space-y-4 mb-4">
        <div>
          <label className="block text-sm text-gray-400 mb-1">Input Path</label>
          <input
            type="text"
            value={inputPath}
            onChange={(e) => setInputPath(e.target.value)}
            className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            required
          />
        </div>

        <div>
          <label className="block text-sm text-gray-400 mb-1">Output Path</label>
          <input
            type="text"
            value={outputPath}
            onChange={(e) => setOutputPath(e.target.value)}
            className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            required
          />
        </div>

        <div>
          <label className="block text-sm text-gray-400 mb-1">Mode</label>
          <select
            value={mode}
            onChange={(e) => setMode(e.target.value)}
            className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
          >
            <option value="PakToIoStore">PakToIoStore</option>
            <option value="IoStoreToPak">IoStoreToPak</option>
            <option value="Repack">Repack</option>
            <option value="Validate">Validate</option>
          </select>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 rounded"
        >
          {loading ? 'Running...' : 'Run Retoc Convert'}
        </button>
      </form>

      {error && (
        <div className="text-red-400 mb-4 p-2 bg-red-900/20 rounded">
          {error}
        </div>
      )}

      {response && (
        <div className="space-y-4">
          <div className="p-3 bg-gray-900 rounded">
            <h3 className="font-semibold mb-2">Response Summary</h3>
            <div className="space-y-1 text-sm">
              <div className="grid grid-cols-2 gap-2">
                <span className="text-gray-400">Operation ID:</span>
                <span className="text-white font-mono text-xs">{response.operationId}</span>

                <span className="text-gray-400">Status:</span>
                <span className={
                  response.status === OperationStatus.Succeeded ? 'text-green-400' : 'text-red-400'
                }>
                  {response.status === OperationStatus.Succeeded ? 'Succeeded' : 'Failed'}
                </span>

                {response.result && (
                  <>
                    <span className="text-gray-400">Exit Code:</span>
                    <span className="text-white">{response.result.exitCode}</span>

                    <span className="text-gray-400">Output Path:</span>
                    <span className="text-white truncate">{response.result.outputPath}</span>
                  </>
                )}

                {response.error && (
                  <>
                    <span className="text-gray-400">Error Code:</span>
                    <span className="text-red-400">{response.error.code}</span>

                    <span className="text-gray-400">Error Message:</span>
                    <span className="text-red-400">{response.error.message}</span>

                    {response.error.remediationHint && (
                      <>
                        <span className="text-gray-400">Hint:</span>
                        <span className="text-yellow-400">{response.error.remediationHint}</span>
                      </>
                    )}
                  </>
                )}
              </div>
            </div>
          </div>

          <details className="p-3 bg-gray-900 rounded">
            <summary className="cursor-pointer text-sm text-gray-400 hover:text-gray-300">
              Full JSON Response
            </summary>
            <pre className="mt-2 text-xs overflow-x-auto">
              {JSON.stringify(response, null, 2)}
            </pre>
          </details>
        </div>
      )}
    </div>
  );
}
