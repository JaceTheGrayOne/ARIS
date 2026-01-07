import { useState } from 'react';
import type {
  DllInjectResponse,
  DllEjectResponse,
  DllInjectResultDto,
  DllEjectResultDto,
} from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';

interface DllInjectorResultPanelProps {
  injectResponse: DllInjectResponse | null;
  ejectResponse: DllEjectResponse | null;
}

export function DllInjectorResultPanel({
  injectResponse,
  ejectResponse,
}: DllInjectorResultPanelProps) {
  const [showRawJson, setShowRawJson] = useState(false);

  if (!injectResponse && !ejectResponse) {
    return (
      <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
        <div className="text-center py-12">
          <div className="text-gray-400 mb-2">
            <svg
              className="w-16 h-16 mx-auto mb-4 opacity-50"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={1.5}
                d="M13 10V3L4 14h7v7l9-11h-7z"
              />
            </svg>
          </div>
          <p className="text-gray-400">No injection or ejection run yet</p>
          <p className="text-gray-500 text-sm mt-1">
            Submit an operation on the left to see results
          </p>
        </div>
      </div>
    );
  }

  const formatDuration = (duration: string): string => {
    const match = duration.match(/(\d+):(\d+):(\d+)\.?(\d+)?/);
    if (!match) return duration;

    const [, hours, minutes, seconds] = match;
    const parts = [];

    if (hours !== '00') parts.push(`${parseInt(hours)}h`);
    if (minutes !== '00') parts.push(`${parseInt(minutes)}m`);
    parts.push(`${parseInt(seconds)}s`);

    return parts.join(' ');
  };

  // Determine which response to show (most recent)
  let response: DllInjectResponse | DllEjectResponse;
  let isInject: boolean;

  if (injectResponse && ejectResponse) {
    const injectTime = new Date(injectResponse.completedAt).getTime();
    const ejectTime = new Date(ejectResponse.completedAt).getTime();
    if (injectTime > ejectTime) {
      response = injectResponse;
      isInject = true;
    } else {
      response = ejectResponse;
      isInject = false;
    }
  } else if (injectResponse) {
    response = injectResponse;
    isInject = true;
  } else {
    response = ejectResponse!;
    isInject = false;
  }

  const isSuccess = response.status === OperationStatus.Succeeded;

  return (
    <div className="space-y-4">
      <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center space-x-3">
            <h3 className="text-lg font-semibold">Operation Result</h3>
            <span
              className={`px-2 py-1 rounded text-xs font-medium ${
                isInject
                  ? 'bg-blue-900/30 text-blue-400 border border-blue-700'
                  : 'bg-amber-900/30 text-amber-400 border border-amber-700'
              }`}
            >
              {isInject ? 'Inject' : 'Eject'}
            </span>
          </div>
          <span
            className={`px-3 py-1 rounded text-sm font-medium ${
              isSuccess
                ? 'bg-green-900/30 text-green-400 border border-green-700'
                : 'bg-red-900/30 text-red-400 border border-red-700'
            }`}
          >
            {isSuccess ? 'Succeeded' : 'Failed'}
          </span>
        </div>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Operation ID</span>
              <div className="text-white font-mono text-xs mt-1 break-all">
                {response.operationId}
              </div>
            </div>

            <div>
              <span className="text-gray-400">Started At</span>
              <div className="text-white text-xs mt-1">
                {new Date(response.startedAt).toLocaleString()}
              </div>
            </div>
          </div>

          {isSuccess && response.result && isInject && (() => {
            const result = response.result as DllInjectResultDto;
            return (
              <div className="pt-4 border-t border-gray-700 space-y-4">
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-400">Process Name</span>
                    <div className="text-white font-mono text-xs mt-1">
                      {result.processName}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Process ID</span>
                    <div className="text-white mt-1">{result.processId}</div>
                  </div>

                  <div className="col-span-2">
                    <span className="text-gray-400">DLL Path</span>
                    <div className="text-white font-mono text-xs mt-1 break-all">
                      {result.dllPath}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Injection Method</span>
                    <div className="text-white mt-1">{result.method}</div>
                  </div>

                  <div>
                    <span className="text-gray-400">Elevation Used</span>
                    <div className="text-white mt-1">
                      {result.elevationUsed ? 'Yes' : 'No'}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Duration</span>
                    <div className="text-white mt-1">
                      {formatDuration(result.duration)}
                    </div>
                  </div>
                </div>

                {result.warnings && result.warnings.length > 0 && (
                  <div className="pt-4 border-t border-gray-700">
                    <span className="text-amber-400 text-sm font-medium">Warnings</span>
                    <ul className="mt-2 space-y-1">
                      {result.warnings.map((warning, idx) => (
                        <li key={idx} className="text-amber-300 text-xs">
                          {warning}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {result.logExcerpt && (
                  <div className="pt-4 border-t border-gray-700">
                    <h4 className="text-sm font-medium text-gray-300 mb-2">Log Excerpt</h4>
                    <pre className="p-3 bg-gray-900 rounded text-xs overflow-x-auto text-gray-300 whitespace-pre-wrap">
                      {result.logExcerpt}
                    </pre>
                  </div>
                )}
              </div>
            );
          })()}

          {isSuccess && response.result && !isInject && (() => {
            const result = response.result as DllEjectResultDto;
            return (
              <div className="pt-4 border-t border-gray-700 space-y-4">
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-400">Process Name</span>
                    <div className="text-white font-mono text-xs mt-1">
                      {result.processName}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Process ID</span>
                    <div className="text-white mt-1">{result.processId}</div>
                  </div>

                  <div className="col-span-2">
                    <span className="text-gray-400">Module Name</span>
                    <div className="text-white font-mono text-xs mt-1">
                      {result.moduleName}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Was Loaded Before</span>
                    <div className="text-white mt-1">
                      {result.wasLoadedBefore ? 'Yes' : 'No'}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Is Unloaded</span>
                    <div className="text-white mt-1">
                      {result.isUnloaded ? 'Yes' : 'No'}
                    </div>
                  </div>

                  <div>
                    <span className="text-gray-400">Duration</span>
                    <div className="text-white mt-1">
                      {formatDuration(result.duration)}
                    </div>
                  </div>
                </div>

                {result.warnings && result.warnings.length > 0 && (
                  <div className="pt-4 border-t border-gray-700">
                    <span className="text-amber-400 text-sm font-medium">Warnings</span>
                    <ul className="mt-2 space-y-1">
                      {result.warnings.map((warning, idx) => (
                        <li key={idx} className="text-amber-300 text-xs">
                          {warning}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {result.logExcerpt && (
                  <div className="pt-4 border-t border-gray-700">
                    <h4 className="text-sm font-medium text-gray-300 mb-2">Log Excerpt</h4>
                    <pre className="p-3 bg-gray-900 rounded text-xs overflow-x-auto text-gray-300 whitespace-pre-wrap">
                      {result.logExcerpt}
                    </pre>
                  </div>
                )}
              </div>
            );
          })()}

          {!isSuccess && response.error && (
            <div className="pt-4 border-t border-gray-700 space-y-3">
              <div>
                <span className="text-red-400 text-sm font-medium">Error Code</span>
                <div className="text-red-300 font-mono text-sm mt-1">
                  {response.error.code}
                </div>
              </div>

              <div>
                <span className="text-red-400 text-sm font-medium">Message</span>
                <div className="text-red-300 text-sm mt-1">{response.error.message}</div>
              </div>

              {response.error.remediationHint && (
                <div>
                  <span className="text-amber-400 text-sm font-medium">Hint</span>
                  <div className="text-amber-300 text-sm mt-1">
                    {response.error.remediationHint}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="mt-6 pt-4 border-t border-gray-700">
          <button
            onClick={() => setShowRawJson(!showRawJson)}
            className="text-sm text-blue-400 hover:text-blue-300 transition-colors"
          >
            {showRawJson ? 'Hide' : 'Show'} Raw JSON
          </button>

          {showRawJson && (
            <pre className="mt-3 p-3 bg-gray-900 rounded text-xs overflow-x-auto">
              {JSON.stringify(response, null, 2)}
            </pre>
          )}
        </div>
      </div>
    </div>
  );
}
