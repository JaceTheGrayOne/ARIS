import { useState } from 'react';
import type { RetocConvertResponse } from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';

interface RetocResultPanelProps {
  response: RetocConvertResponse | null;
}

export function RetocResultPanel({ response }: RetocResultPanelProps) {
  const [showRawJson, setShowRawJson] = useState(false);

  if (!response) {
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
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
          </div>
          <p className="text-gray-400">No operation run yet</p>
          <p className="text-gray-500 text-sm mt-1">
            Submit a conversion on the left to see results
          </p>
        </div>
      </div>
    );
  }

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

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

  const isSuccess = response.status === OperationStatus.Succeeded;

  return (
    <div className="space-y-4">
      <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold">Operation Result</h3>
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

          {isSuccess && response.result && (
            <div className="pt-4 border-t border-gray-700 space-y-4">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-gray-400">Exit Code</span>
                  <div className="text-white mt-1">{response.result.exitCode}</div>
                </div>

                <div>
                  <span className="text-gray-400">Duration</span>
                  <div className="text-white mt-1">
                    {formatDuration(response.result.duration)}
                  </div>
                </div>

                <div className="col-span-2">
                  <span className="text-gray-400">Output Path</span>
                  <div className="text-white font-mono text-xs mt-1 break-all">
                    {response.result.outputPath}
                  </div>
                </div>

                {response.result.outputFormat && (
                  <div>
                    <span className="text-gray-400">Output Format</span>
                    <div className="text-white mt-1">{response.result.outputFormat}</div>
                  </div>
                )}

                {response.result.ueVersion && (
                  <div>
                    <span className="text-gray-400">UE Version</span>
                    <div className="text-white mt-1">{response.result.ueVersion}</div>
                  </div>
                )}
              </div>

              {response.result.warnings && response.result.warnings.length > 0 && (
                <div className="pt-4 border-t border-gray-700">
                  <span className="text-amber-400 text-sm font-medium">Warnings</span>
                  <ul className="mt-2 space-y-1">
                    {response.result.warnings.map((warning, idx) => (
                      <li key={idx} className="text-amber-300 text-xs">
                        {warning}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {response.result.producedFiles && response.result.producedFiles.length > 0 && (
                <div className="pt-4 border-t border-gray-700">
                  <h4 className="text-sm font-medium text-gray-300 mb-3">
                    Produced Files ({response.result.producedFiles.length})
                  </h4>
                  <div className="overflow-x-auto">
                    <table className="w-full text-xs">
                      <thead>
                        <tr className="border-b border-gray-700">
                          <th className="text-left py-2 px-2 text-gray-400 font-medium">
                            Path
                          </th>
                          <th className="text-right py-2 px-2 text-gray-400 font-medium">
                            Size
                          </th>
                          <th className="text-left py-2 px-2 text-gray-400 font-medium">
                            Type
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {response.result.producedFiles.map((file, idx) => (
                          <tr key={idx} className="border-b border-gray-700/50">
                            <td className="py-2 px-2 text-white font-mono break-all">
                              {file.path}
                            </td>
                            <td className="py-2 px-2 text-gray-300 text-right">
                              {formatBytes(file.sizeBytes)}
                            </td>
                            <td className="py-2 px-2 text-gray-300">
                              {file.fileType || '-'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}

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
