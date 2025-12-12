import { useState } from 'react';
import { UAssetForm, type UAssetFormSubmit } from '../../components/uasset/UAssetForm';
import { UAssetResultPanel } from '../../components/uasset/UAssetResultPanel';
import {
  runSerialize,
  runDeserialize,
  runInspect,
} from '../../api/uassetClient';
import type {
  UAssetSerializeResponse,
  UAssetDeserializeResponse,
  UAssetInspectResponse,
} from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';
import { recordOperation, type OperationHistoryEntry, type OperationKind } from '../../state/operationHistory';

const MAX_HISTORY_SIZE = 10;

type UAssetResponse =
  | UAssetSerializeResponse
  | UAssetDeserializeResponse
  | UAssetInspectResponse;

export function UAssetPage() {
  const [currentResponse, setCurrentResponse] = useState<UAssetResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [history, setHistory] = useState<UAssetResponse[]>([]);

  const handleSubmit = async (payload: UAssetFormSubmit) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      let response: UAssetResponse;

      if (payload.kind === 'serialize') {
        response = await runSerialize(payload.request);
      } else if (payload.kind === 'deserialize') {
        response = await runDeserialize(payload.request);
      } else {
        response = await runInspect(payload.request);
      }

      setCurrentResponse(response);

      // Record to global operation history
      let kind: OperationKind;
      let label: string;
      let summary: string;

      if (payload.kind === 'serialize') {
        kind = 'UAssetSerialize';
        label = 'Serialize';
        const result = (response as UAssetSerializeResponse).result;
        summary = result
          ? `${result.producedFiles.length} files, ${result.duration}`
          : 'No result';
      } else if (payload.kind === 'deserialize') {
        kind = 'UAssetDeserialize';
        label = 'Deserialize';
        const result = (response as UAssetDeserializeResponse).result;
        summary = result
          ? `${result.producedFiles.length} files, ${result.duration}`
          : 'No result';
      } else {
        kind = 'UAssetInspect';
        label = 'Inspect';
        const result = (response as UAssetInspectResponse).result;
        summary = result
          ? `Names: ${result.summary.nameCount}, Exports: ${result.summary.exportCount}, Imports: ${result.summary.importCount}`
          : 'No result';
      }

      const entry: OperationHistoryEntry = {
        id: response.operationId,
        tool: 'UAsset',
        kind,
        status: response.status,
        startedAt: response.startedAt,
        completedAt: response.completedAt,
        label,
        summary,
        payload: response,
      };
      recordOperation(entry);

      setHistory((prev) => {
        const newHistory = [response, ...prev];
        return newHistory.slice(0, MAX_HISTORY_SIZE);
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to submit operation: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleHistoryClick = (response: UAssetResponse) => {
    setCurrentResponse(response);
    setSubmitError(null);
  };

  const getOperationKind = (response: UAssetResponse): string => {
    if ('result' in response && response.result) {
      if ('operation' in response.result) {
        return response.result.operation;
      }
      if ('summary' in response.result) {
        return 'Inspect';
      }
    }
    return 'Unknown';
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">UAsset Inspector / Converter</h1>
        <p className="text-gray-400 mt-2">
          Serialize and deserialize Unreal Engine asset files using UAssetAPI (in-process)
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
          <h2 className="text-xl font-semibold mb-4">Operation Settings</h2>
          <UAssetForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
        </div>

        <div className="space-y-4">
          {submitError && (
            <div className="border border-red-700 rounded-lg p-4 bg-red-900/20">
              <p className="text-red-400 text-sm font-medium">Error</p>
              <p className="text-red-300 text-sm mt-1">{submitError}</p>
            </div>
          )}

          <UAssetResultPanel response={currentResponse} />

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
                        <span className="text-white text-xs">{getOperationKind(item)}</span>
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
