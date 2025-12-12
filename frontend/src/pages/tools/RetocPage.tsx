import { useState } from 'react';
import { getBackendBaseUrl } from '../../config/backend';
import { RetocForm } from '../../components/retoc/RetocForm';
import { RetocResultPanel } from '../../components/retoc/RetocResultPanel';
import type { RetocConvertRequest, RetocConvertResponse } from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';
import { recordOperation, type OperationHistoryEntry } from '../../state/operationHistory';

const MAX_HISTORY_SIZE = 10;

export function RetocPage() {
  const [currentResponse, setCurrentResponse] = useState<RetocConvertResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [history, setHistory] = useState<RetocConvertResponse[]>([]);

  const handleSubmit = async (request: RetocConvertRequest) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const baseUrl = getBackendBaseUrl();
      const response = await fetch(`${baseUrl}/api/retoc/convert`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const data: RetocConvertResponse = await response.json();

      setCurrentResponse(data);

      // Record to global operation history
      const entry: OperationHistoryEntry = {
        id: data.operationId,
        tool: 'Retoc',
        kind: 'RetocConvert',
        status: data.status,
        startedAt: data.startedAt,
        completedAt: data.completedAt,
        label: 'Convert',
        summary: `${data.result?.outputFormat ?? 'Unknown'} → exit code ${data.result?.exitCode ?? 0}`,
        payload: data,
      };
      recordOperation(entry);

      setHistory((prev) => {
        const newHistory = [data, ...prev];
        return newHistory.slice(0, MAX_HISTORY_SIZE);
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to submit conversion: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleHistoryClick = (response: RetocConvertResponse) => {
    setCurrentResponse(response);
    setSubmitError(null);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">IoStore / Retoc</h1>
        <p className="text-gray-400 mt-2">
          Convert PAK archives to IoStore and back using Retoc
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
          <h2 className="text-xl font-semibold mb-4">Conversion Settings</h2>
          <RetocForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
        </div>

        <div className="space-y-4">
          {submitError && (
            <div className="border border-red-700 rounded-lg p-4 bg-red-900/20">
              <p className="text-red-400 text-sm font-medium">Network Error</p>
              <p className="text-red-300 text-sm mt-1">{submitError}</p>
            </div>
          )}

          <RetocResultPanel response={currentResponse} />

          {history.length > 0 && (
            <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
              <h3 className="text-lg font-semibold mb-4">Recent Operations</h3>
              <div className="space-y-2">
                {history.map((item) => (
                  <button
                    key={item.operationId}
                    onClick={() => handleHistoryClick(item)}
                    className={`w-full text-left p-3 rounded transition-colors ${
                      currentResponse?.operationId === item.operationId
                        ? 'bg-blue-900/30 border border-blue-700'
                        : 'bg-gray-900 hover:bg-gray-700 border border-gray-700'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <span
                          className={`px-2 py-1 rounded text-xs font-medium ${
                            item.status === OperationStatus.Succeeded
                              ? 'bg-green-900/30 text-green-400'
                              : 'bg-red-900/30 text-red-400'
                          }`}
                        >
                          {item.status === OperationStatus.Succeeded ? 'Success' : 'Failed'}
                        </span>
                        <span className="text-white font-mono text-xs">
                          {item.operationId.substring(0, 8)}...
                        </span>
                      </div>
                      <span className="text-gray-400 text-xs">
                        {new Date(item.startedAt).toLocaleTimeString()}
                      </span>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
