import { useState, useEffect } from 'react';
import {
  getOperations,
  subscribe,
  type OperationHistoryEntry,
  type ToolKind,
} from '../state/operationHistory';
import { OperationStatus } from '../types/contracts';

type StatusFilter = OperationStatus | 'All';

export function LogsPage() {
  const [entries, setEntries] = useState<OperationHistoryEntry[]>(() => getOperations());
  const [toolFilter, setToolFilter] = useState<ToolKind | 'All'>('All');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All');

  // Subscribe to operation history updates
  useEffect(() => {
    const unsubscribe = subscribe(() => {
      setEntries(getOperations());
    });
    return unsubscribe;
  }, []);

  // Filter entries
  const filteredEntries = entries.filter((e) => {
    if (toolFilter !== 'All' && e.tool !== toolFilter) return false;
    if (statusFilter !== 'All' && e.status !== statusFilter) return false;
    return true;
  });

  // Select first entry or maintain selection
  const [selectedId, setSelectedId] = useState<string | null>(
    filteredEntries[0]?.id ?? null
  );

  // Auto-select first if current selection is filtered out
  useEffect(() => {
    if (selectedId && !filteredEntries.find((e) => e.id === selectedId)) {
      setSelectedId(filteredEntries[0]?.id ?? null);
    } else if (!selectedId && filteredEntries.length > 0) {
      setSelectedId(filteredEntries[0].id);
    }
  }, [filteredEntries, selectedId]);

  const selected = filteredEntries.find((e) => e.id === selectedId) ?? null;

  const getToolBadgeColor = (tool: ToolKind): string => {
    switch (tool) {
      case 'Retoc':
        return 'bg-purple-900/30 text-purple-400';
      case 'UAsset':
        return 'bg-green-900/30 text-green-400';
      case 'UwpDumper':
        return 'bg-cyan-900/30 text-cyan-400';
      case 'DllInjector':
        return 'bg-orange-900/30 text-orange-400';
    }
  };

  const getStatusBadgeColor = (status: OperationStatus): string => {
    switch (status) {
      case OperationStatus.Succeeded:
        return 'bg-green-900/30 text-green-400';
      case OperationStatus.Failed:
        return 'bg-red-900/30 text-red-400';
      case OperationStatus.Pending:
        return 'bg-yellow-900/30 text-yellow-400';
      default:
        return 'bg-gray-900/30 text-gray-400';
    }
  };

  const getStatusLabel = (status: OperationStatus): string => {
    switch (status) {
      case OperationStatus.Succeeded:
        return 'Success';
      case OperationStatus.Failed:
        return 'Failed';
      case OperationStatus.Pending:
        return 'Pending';
      default:
        return 'Unknown';
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Operation Logs</h1>
        <p className="text-gray-400 mt-2">
          Session history across all tool operations
        </p>
      </div>

      {/* Filters */}
      <div className="flex items-center space-x-3">
        <div>
          <label className="text-sm text-gray-400 mr-2">Tool:</label>
          <select
            value={toolFilter}
            onChange={(e) => setToolFilter(e.target.value as ToolKind | 'All')}
            className="px-3 py-1 bg-gray-900 border border-gray-600 rounded text-sm text-white"
          >
            <option value="All">All Tools</option>
            <option value="Retoc">Retoc</option>
            <option value="UAsset">UAsset</option>
            <option value="UwpDumper">UWP Dumper</option>
            <option value="DllInjector">DLL Injector</option>
          </select>
        </div>

        <div>
          <label className="text-sm text-gray-400 mr-2">Status:</label>
          <select
            value={statusFilter}
            onChange={(e) => {
              const val = e.target.value;
              setStatusFilter((val === 'All' ? 'All' : Number(val)) as StatusFilter);
            }}
            className="px-3 py-1 bg-gray-900 border border-gray-600 rounded text-sm text-white"
          >
            <option value="All">All Status</option>
            <option value={OperationStatus.Succeeded}>Succeeded</option>
            <option value={OperationStatus.Failed}>Failed</option>
            <option value={OperationStatus.Pending}>Pending</option>
          </select>
        </div>

        <div className="text-sm text-gray-400 ml-auto">
          {filteredEntries.length} operation{filteredEntries.length !== 1 ? 's' : ''}
        </div>
      </div>

      {filteredEntries.length === 0 ? (
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
            <p className="text-gray-400">No operations recorded yet</p>
            <p className="text-gray-500 text-sm mt-1">
              Operations will appear here as you use the tools
            </p>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left: Operation List */}
          <div className="border border-gray-700 rounded-lg bg-gray-800 p-4">
            <h2 className="text-lg font-semibold mb-4">Operations</h2>
            <div className="space-y-2 max-h-[600px] overflow-y-auto">
              {filteredEntries.map((entry) => (
                <button
                  key={entry.id}
                  onClick={() => setSelectedId(entry.id)}
                  className={`w-full text-left p-3 rounded transition-colors ${
                    selectedId === entry.id
                      ? 'bg-blue-900/30 border border-blue-700'
                      : 'bg-gray-900 hover:bg-gray-700 border border-gray-700'
                  }`}
                >
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center space-x-2">
                      <span
                        className={`px-2 py-1 rounded text-xs font-medium ${getToolBadgeColor(
                          entry.tool
                        )}`}
                      >
                        {entry.tool}
                      </span>
                      <span
                        className={`px-2 py-1 rounded text-xs font-medium ${getStatusBadgeColor(
                          entry.status
                        )}`}
                      >
                        {getStatusLabel(entry.status)}
                      </span>
                    </div>
                    <span className="text-gray-400 text-xs">
                      {new Date(entry.completedAt).toLocaleTimeString()}
                    </span>
                  </div>
                  <div className="text-white text-sm font-medium mb-1">
                    {entry.kind.replace(/([A-Z])/g, ' $1').trim()}
                  </div>
                  <div className="text-gray-400 text-xs truncate">{entry.summary}</div>
                </button>
              ))}
            </div>
          </div>

          {/* Right: Detail View */}
          <div className="border border-gray-700 rounded-lg bg-gray-800 p-6">
            {selected ? (
              <OperationDetail entry={selected} />
            ) : (
              <div className="text-center py-12 text-gray-400">
                No operation selected
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// Detail component
function OperationDetail({ entry }: { entry: OperationHistoryEntry }) {
  const [showRawJson, setShowRawJson] = useState(false);

  const formatDuration = (start: string, end: string): string => {
    const ms = new Date(end).getTime() - new Date(start).getTime();
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    const parts = [];
    if (hours > 0) parts.push(`${hours}h`);
    if (minutes % 60 > 0) parts.push(`${minutes % 60}m`);
    parts.push(`${seconds % 60}s`);

    return parts.join(' ');
  };

  const getStatusBadgeColor = (status: OperationStatus): string => {
    switch (status) {
      case OperationStatus.Succeeded:
        return 'bg-green-900/30 text-green-400 border border-green-700';
      case OperationStatus.Failed:
        return 'bg-red-900/30 text-red-400 border border-red-700';
      case OperationStatus.Pending:
        return 'bg-yellow-900/30 text-yellow-400 border border-yellow-700';
      default:
        return 'bg-gray-900/30 text-gray-400 border border-gray-700';
    }
  };

  const getStatusLabel = (status: OperationStatus): string => {
    switch (status) {
      case OperationStatus.Succeeded:
        return 'Succeeded';
      case OperationStatus.Failed:
        return 'Failed';
      case OperationStatus.Pending:
        return 'Pending';
      default:
        return 'Unknown';
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-xl font-semibold">
          {entry.kind.replace(/([A-Z])/g, ' $1').trim()}
        </h3>
        <span className={`px-3 py-1 rounded text-sm font-medium ${getStatusBadgeColor(entry.status)}`}>
          {getStatusLabel(entry.status)}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-4 text-sm pt-4 border-t border-gray-700">
        <div>
          <span className="text-gray-400">Operation ID</span>
          <div className="text-white font-mono text-xs mt-1 break-all">{entry.id}</div>
        </div>

        <div>
          <span className="text-gray-400">Duration</span>
          <div className="text-white mt-1">
            {formatDuration(entry.startedAt, entry.completedAt)}
          </div>
        </div>

        <div className="col-span-2">
          <span className="text-gray-400">Started</span>
          <div className="text-white text-xs mt-1">
            {new Date(entry.startedAt).toLocaleString()}
          </div>
        </div>

        <div className="col-span-2">
          <span className="text-gray-400">Completed</span>
          <div className="text-white text-xs mt-1">
            {new Date(entry.completedAt).toLocaleString()}
          </div>
        </div>
      </div>

      {/* Tool-specific details */}
      <div className="pt-4 border-t border-gray-700">
        <h4 className="text-sm font-medium text-gray-300 mb-3">Operation Details</h4>
        <ToolSpecificDetails entry={entry} />
      </div>

      {/* Raw JSON toggle */}
      <div className="pt-4 border-t border-gray-700">
        <button
          onClick={() => setShowRawJson(!showRawJson)}
          className="text-sm text-blue-400 hover:text-blue-300 transition-colors"
        >
          {showRawJson ? 'Hide' : 'Show'} Raw JSON
        </button>

        {showRawJson && (
          <pre className="mt-3 p-3 bg-gray-900 rounded text-xs overflow-x-auto max-h-96">
            {JSON.stringify(entry.payload, null, 2)}
          </pre>
        )}
      </div>
    </div>
  );
}

function ToolSpecificDetails({ entry }: { entry: OperationHistoryEntry }) {
  if (entry.tool === 'Retoc' && 'result' in entry.payload && entry.payload.result) {
    const result = entry.payload.result as import('../types/contracts').RetocResultDto;
    return (
      <div className="space-y-3 text-sm">
        <div>
          <span className="text-gray-400">Output Format:</span>
          <span className="text-white ml-2">{result.outputFormat ?? 'N/A'}</span>
        </div>
        <div>
          <span className="text-gray-400">Exit Code:</span>
          <span className="text-white ml-2">{result.exitCode}</span>
        </div>
        <div>
          <span className="text-gray-400">Produced Files:</span>
          <span className="text-white ml-2">{result.producedFiles.length}</span>
        </div>
      </div>
    );
  }

  if (entry.tool === 'UAsset' && 'result' in entry.payload && entry.payload.result) {
    if ('summary' in entry.payload.result) {
      // Inspect
      const result = entry.payload.result as import('../types/contracts').UAssetInspectionDto;
      return (
        <div className="space-y-3 text-sm">
          <div>
            <span className="text-gray-400">Names:</span>
            <span className="text-white ml-2">{result.summary.nameCount}</span>
          </div>
          <div>
            <span className="text-gray-400">Exports:</span>
            <span className="text-white ml-2">{result.summary.exportCount}</span>
          </div>
          <div>
            <span className="text-gray-400">Imports:</span>
            <span className="text-white ml-2">{result.summary.importCount}</span>
          </div>
        </div>
      );
    } else {
      // Serialize/Deserialize
      const result = entry.payload.result as import('../types/contracts').UAssetResultDto;
      return (
        <div className="space-y-3 text-sm">
          <div>
            <span className="text-gray-400">Operation:</span>
            <span className="text-white ml-2">{result.operation}</span>
          </div>
          <div>
            <span className="text-gray-400">Input:</span>
            <div className="text-white font-mono text-xs mt-1 break-all">
              {result.inputPath}
            </div>
          </div>
          <div>
            <span className="text-gray-400">Output:</span>
            <div className="text-white font-mono text-xs mt-1 break-all">
              {result.outputPath}
            </div>
          </div>
          <div>
            <span className="text-gray-400">Produced Files:</span>
            <span className="text-white ml-2">{result.producedFiles.length}</span>
          </div>
        </div>
      );
    }
  }

  if (entry.tool === 'UwpDumper' && 'result' in entry.payload && entry.payload.result) {
    const result = entry.payload.result as import('../types/contracts').UwpDumpResultDto;
    return (
      <div className="space-y-3 text-sm">
        <div>
          <span className="text-gray-400">Package:</span>
          <div className="text-white font-mono text-xs mt-1 break-all">
            {result.packageFamilyName}
          </div>
        </div>
        <div>
          <span className="text-gray-400">Mode:</span>
          <span className="text-white ml-2">{result.mode}</span>
        </div>
        <div>
          <span className="text-gray-400">Files Extracted:</span>
          <span className="text-white ml-2">{result.filesExtracted}</span>
        </div>
      </div>
    );
  }

  if (entry.tool === 'DllInjector' && 'result' in entry.payload && entry.payload.result) {
    if ('dllPath' in entry.payload.result) {
      // Inject
      const result = entry.payload.result as import('../types/contracts').DllInjectResultDto;
      return (
        <div className="space-y-3 text-sm">
          <div>
            <span className="text-gray-400">Process:</span>
            <span className="text-white ml-2 font-mono text-xs">
              {result.processName} (PID: {result.processId})
            </span>
          </div>
          <div>
            <span className="text-gray-400">DLL:</span>
            <div className="text-white font-mono text-xs mt-1 break-all">
              {result.dllPath}
            </div>
          </div>
          <div>
            <span className="text-gray-400">Method:</span>
            <span className="text-white ml-2">{result.method}</span>
          </div>
          <div>
            <span className="text-gray-400">Elevation:</span>
            <span className="text-white ml-2">{result.elevationUsed ? 'Yes' : 'No'}</span>
          </div>
        </div>
      );
    } else {
      // Eject
      const result = entry.payload.result as import('../types/contracts').DllEjectResultDto;
      return (
        <div className="space-y-3 text-sm">
          <div>
            <span className="text-gray-400">Process:</span>
            <span className="text-white ml-2 font-mono text-xs">
              {result.processName} (PID: {result.processId})
            </span>
          </div>
          <div>
            <span className="text-gray-400">Module:</span>
            <span className="text-white ml-2 font-mono text-xs">{result.moduleName}</span>
          </div>
          <div>
            <span className="text-gray-400">Was Loaded:</span>
            <span className="text-white ml-2">{result.wasLoadedBefore ? 'Yes' : 'No'}</span>
          </div>
          <div>
            <span className="text-gray-400">Is Unloaded:</span>
            <span className="text-white ml-2">{result.isUnloaded ? 'Yes' : 'No'}</span>
          </div>
        </div>
      );
    }
  }

  return <div className="text-gray-400 text-sm">No additional details available.</div>;
}
