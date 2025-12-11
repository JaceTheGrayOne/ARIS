import { useState } from 'react';
import type { UwpDumpResponse, UwpDumpResultDto } from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';

interface UwpDumperResultPanelProps {
  response: UwpDumpResponse | null;
}

export function UwpDumperResultPanel({ response }: UwpDumperResultPanelProps) {
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
                d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
              />
            </svg>
          </div>
          <p className="text-gray-400">No dump operation run yet</p>
          <p className="text-gray-500 text-sm mt-1">
            Submit a dump operation on the left to see results
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
          <h3 className="text-lg font-semibold">Dump Result</h3>
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

          {isSuccess && response.result && (() => {
            const result = response.result as UwpDumpResultDto;
            return (
              <div className="pt-4 border-t border-gray-700 space-y-4">
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-400">Package Family Name</span>
                    <div className="text-white font-mono text-xs mt-1 break-all">
                      {result.packageFamilyName}
                    </div>
                  </div>

                  {result.applicationId && (
                    <div>
                      <span className="text-gray-400">Application ID</span>
                      <div className="text-white font-mono text-xs mt-1">
                        {result.applicationId}
                      </div>
                    </div>
                  )}

                  <div>
                    <span className="text-gray-400">Mode</span>
                    <div className="text-white mt-1">{result.mode}</div>
                  </div>

                  <div>
                    <span className="text-gray-400">Duration</span>
                    <div className="text-white mt-1">{formatDuration(result.duration)}</div>
                  </div>

                  <div>
                    <span className="text-gray-400">Files Extracted</span>
                    <div className="text-white mt-1">{result.filesExtracted}</div>
                  </div>

                  <div>
                    <span className="text-gray-400">Total Size</span>
                    <div className="text-white mt-1">
                      {formatBytes(result.totalSizeBytes)}
                    </div>
                  </div>

                  <div className="col-span-2">
                    <span className="text-gray-400">Output Path</span>
                    <div className="text-white font-mono text-xs mt-1 break-all">
                      {result.outputPath}
                    </div>
                  </div>
                </div>

                {result.metadata && (
                  <div className="pt-4 border-t border-gray-700">
                    <h4 className="text-sm font-medium text-gray-300 mb-3">Package Metadata</h4>
                    <div className="grid grid-cols-1 gap-3 text-sm">
                      {result.metadata.packageFullName && (
                        <div>
                          <span className="text-gray-400">Full Name</span>
                          <div className="text-white font-mono text-xs mt-1 break-all">
                            {result.metadata.packageFullName}
                          </div>
                        </div>
                      )}

                      <div className="grid grid-cols-3 gap-4">
                        {result.metadata.version && (
                          <div>
                            <span className="text-gray-400">Version</span>
                            <div className="text-white mt-1">{result.metadata.version}</div>
                          </div>
                        )}

                        {result.metadata.architecture && (
                          <div>
                            <span className="text-gray-400">Architecture</span>
                            <div className="text-white mt-1">{result.metadata.architecture}</div>
                          </div>
                        )}
                      </div>

                      {result.metadata.publisher && (
                        <div>
                          <span className="text-gray-400">Publisher</span>
                          <div className="text-white font-mono text-xs mt-1 break-all">
                            {result.metadata.publisher}
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {result.warnings.length > 0 && (
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

                {result.producedFiles.length > 0 && (
                  <div className="pt-4 border-t border-gray-700">
                    <h4 className="text-sm font-medium text-gray-300 mb-3">
                      Produced Files ({result.producedFiles.length})
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
                          {result.producedFiles.map((file, idx) => (
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
