import { useState } from 'react';
import { UwpDumperForm } from '../../components/uwpdumper/UwpDumperForm';
import { UwpDumperResultPanel } from '../../components/uwpdumper/UwpDumperResultPanel';
import { runDump } from '../../api/uwpDumperMockClient';
import type { UwpDumpCommandDto, UwpDumpResponse } from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';
import { recordOperation, type OperationHistoryEntry } from '../../state/operationHistory';

const MAX_HISTORY_SIZE = 10;

export function UwpDumperPage() {
  const [currentResponse, setCurrentResponse] = useState<UwpDumpResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [history, setHistory] = useState<UwpDumpResponse[]>([]);

  const handleSubmit = async (command: UwpDumpCommandDto) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await runDump(command);
      setCurrentResponse(response);

      // Record to global operation history
      const entry: OperationHistoryEntry = {
        id: response.operationId,
        tool: 'UwpDumper',
        kind: 'UwpDump',
        status: response.status,
        startedAt: response.startedAt,
        completedAt: response.completedAt,
        label: response.result?.mode ?? 'Dump',
        summary: response.result
          ? `${response.result.filesExtracted} files, ${response.result.duration}`
          : 'No result',
        payload: response,
      };
      recordOperation(entry);

      setHistory((prev) => {
        const newHistory = [response, ...prev];
        return newHistory.slice(0, MAX_HISTORY_SIZE);
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to submit dump operation: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleHistoryClick = (response: UwpDumpResponse) => {
    setCurrentResponse(response);
    setSubmitError(null);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">UWP Dumper</h1>
        <p className="text-gray-400 mt-2">
          Extract and inspect Universal Windows Platform application packages
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
          <h2 className="text-xl font-semibold mb-4">Dump Settings</h2>
          <UwpDumperForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
        </div>

        <div className="space-y-4">
          {submitError && (
            <div className="border border-red-700 rounded-lg p-4 bg-red-900/20">
              <p className="text-red-400 text-sm font-medium">Error</p>
              <p className="text-red-300 text-sm mt-1">{submitError}</p>
            </div>
          )}

          <UwpDumperResultPanel response={currentResponse} />

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
                        <span className="text-white text-xs">
                          {item.result?.mode || 'Unknown'}
                        </span>
                        <span className="text-gray-400 font-mono text-xs">
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
