import { useState } from 'react';
import { DllInjectorForm } from '../../components/dllinjector/DllInjectorForm';
import { DllInjectorResultPanel } from '../../components/dllinjector/DllInjectorResultPanel';
import { runInject, runEject } from '../../api/dllInjectorClient';
import type {
  DllInjectCommandDto,
  DllEjectCommandDto,
  DllInjectResponse,
  DllEjectResponse,
} from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';
import { recordOperation, type OperationHistoryEntry } from '../../state/operationHistory';

const MAX_HISTORY_SIZE = 10;

type HistoryEntry = DllInjectResponse | DllEjectResponse;

export function DllInjectorPage() {
  const [injectResponse, setInjectResponse] = useState<DllInjectResponse | null>(null);
  const [ejectResponse, setEjectResponse] = useState<DllEjectResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [history, setHistory] = useState<HistoryEntry[]>([]);

  const handleInject = async (command: DllInjectCommandDto) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await runInject(command);
      setInjectResponse(response);
      setEjectResponse(null);

      // Record to global operation history
      const entry: OperationHistoryEntry = {
        id: response.operationId,
        tool: 'DllInjector',
        kind: 'DllInject',
        status: response.status,
        startedAt: response.startedAt,
        completedAt: response.completedAt,
        label: 'Inject',
        summary: response.result
          ? `${response.result.processName} (PID: ${response.result.processId})`
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
      setSubmitError(`Failed to perform injection: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEject = async (command: DllEjectCommandDto) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await runEject(command);
      setEjectResponse(response);
      setInjectResponse(null);

      // Record to global operation history
      const entry: OperationHistoryEntry = {
        id: response.operationId,
        tool: 'DllInjector',
        kind: 'DllEject',
        status: response.status,
        startedAt: response.startedAt,
        completedAt: response.completedAt,
        label: 'Eject',
        summary: response.result
          ? `${response.result.moduleName} from ${response.result.processName}`
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
      setSubmitError(`Failed to perform ejection: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleHistoryClick = (entry: HistoryEntry) => {
    if ('result' in entry && entry.result && 'dllPath' in entry.result) {
      setInjectResponse(entry as DllInjectResponse);
      setEjectResponse(null);
    } else {
      setEjectResponse(entry as DllEjectResponse);
      setInjectResponse(null);
    }
    setSubmitError(null);
  };

  const getOperationType = (entry: HistoryEntry): 'inject' | 'eject' => {
    if ('result' in entry && entry.result && 'dllPath' in entry.result) {
      return 'inject';
    }
    return 'eject';
  };

  const getProcessInfo = (entry: HistoryEntry): string => {
    if (entry.result) {
      return entry.result.processName || `PID ${entry.result.processId}`;
    }
    return 'Unknown';
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">DLL Injector</h1>
        <p className="text-gray-400 mt-2">
          Inject and eject ARIS payloads into running processes (mocked UI; no real injection)
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
          <h2 className="text-xl font-semibold mb-4">Operation Settings</h2>
          <DllInjectorForm
            isSubmitting={isSubmitting}
            onInject={handleInject}
            onEject={handleEject}
          />
        </div>

        <div className="space-y-4">
          {submitError && (
            <div className="border border-red-700 rounded-lg p-4 bg-red-900/20">
              <p className="text-red-400 text-sm font-medium">Error</p>
              <p className="text-red-300 text-sm mt-1">{submitError}</p>
            </div>
          )}

          <DllInjectorResultPanel
            injectResponse={injectResponse}
            ejectResponse={ejectResponse}
          />

          {history.length > 0 && (
            <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
              <h3 className="text-lg font-semibold mb-4">Recent Operations</h3>
              <div className="space-y-2">
                {history.map((item) => {
                  const isInject = getOperationType(item) === 'inject';
                  const isActive =
                    (isInject && injectResponse?.operationId === item.operationId) ||
                    (!isInject && ejectResponse?.operationId === item.operationId);

                  return (
                    <button
                      key={item.operationId}
                      onClick={() => handleHistoryClick(item)}
                      className={`w-full text-left p-3 rounded transition-colors ${
                        isActive
                          ? 'bg-blue-900/30 border border-blue-700'
                          : 'bg-gray-900 hover:bg-gray-700 border border-gray-700'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-3">
                          <span
                            className={`px-2 py-1 rounded text-xs font-medium ${
                              isInject
                                ? 'bg-blue-900/30 text-blue-400'
                                : 'bg-amber-900/30 text-amber-400'
                            }`}
                          >
                            {isInject ? 'Inject' : 'Eject'}
                          </span>
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
                            {getProcessInfo(item)}
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
                  );
                })}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
