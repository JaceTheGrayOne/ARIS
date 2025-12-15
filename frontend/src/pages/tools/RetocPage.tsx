import { useState } from 'react';
import { getBackendBaseUrl } from '../../config/backend';
import { RetocResultPanel } from '../../components/retoc/RetocResultPanel';
import type { RetocConvertRequest, RetocConvertResponse } from '../../types/contracts';
import { OperationStatus } from '../../types/contracts';
import { recordOperation, type OperationHistoryEntry } from '../../state/operationHistory';

const MAX_HISTORY_SIZE = 10;

const UE_VERSIONS = [
  { value: 'UE5_6', label: 'UE 5.6' },
  { value: 'UE5_5', label: 'UE 5.5' },
  { value: 'UE5_4', label: 'UE 5.4' },
  { value: 'UE5_3', label: 'UE 5.3' },
  { value: 'UE5_2', label: 'UE 5.2' },
  { value: 'UE5_1', label: 'UE 5.1' },
  { value: 'UE5_0', label: 'UE 5.0' },
];

export function RetocPage() {
  const [currentResponse, setCurrentResponse] = useState<RetocConvertResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [history, setHistory] = useState<RetocConvertResponse[]>([]);

  // Pack form state
  const [packModifiedDir, setPackModifiedDir] = useState('');
  const [packModOutputDir, setPackModOutputDir] = useState('');
  const [packModName, setPackModName] = useState('');
  const [packEngineVersion, setPackEngineVersion] = useState('UE5_6');
  const [packErrors, setPackErrors] = useState<Record<string, string>>({});

  // Unpack form state
  const [unpackPaksDir, setUnpackPaksDir] = useState('');
  const [unpackOutputDir, setUnpackOutputDir] = useState('');
  const [unpackErrors, setUnpackErrors] = useState<Record<string, string>>({});

  const executeConversion = async (request: RetocConvertRequest, operationLabel: string) => {
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
        label: operationLabel,
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

  const handlePackSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: Record<string, string> = {};

    if (!packModifiedDir.trim()) {
      newErrors.packModifiedDir = 'Modified directory is required';
    }

    if (!packModOutputDir.trim()) {
      newErrors.packModOutputDir = 'Output directory is required';
    }

    if (!packModName.trim()) {
      newErrors.packModName = 'Mod name is required';
    }

    if (!packEngineVersion) {
      newErrors.packEngineVersion = 'Engine version is required';
    }

    setPackErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      return;
    }

    // Compute output UTOC path: <modOutputDir>\<modName>.utoc
    const outputUtocPath = `${packModOutputDir.trim()}\\${packModName.trim()}.utoc`;

    const request: RetocConvertRequest = {
      inputPath: packModifiedDir.trim(),
      outputPath: outputUtocPath,
      mode: 'PakToIoStore',
      engineVersion: packEngineVersion,
    };

    executeConversion(request, 'Pack');
  };

  const handleUnpackSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: Record<string, string> = {};

    if (!unpackPaksDir.trim()) {
      newErrors.unpackPaksDir = 'Paks directory is required';
    }

    if (!unpackOutputDir.trim()) {
      newErrors.unpackOutputDir = 'Output directory is required';
    }

    setUnpackErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      return;
    }

    const request: RetocConvertRequest = {
      inputPath: unpackPaksDir.trim(),
      outputPath: unpackOutputDir.trim(),
      mode: 'IoStoreToPak',
    };

    executeConversion(request, 'Unpack');
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
          Pack and unpack Unreal Engine IoStore containers
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column: Pack & Unpack Forms */}
        <div className="space-y-6">
          {/* Pack Section */}
          <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
            <h2 className="text-xl font-semibold mb-4">Pack (Legacy → Zen)</h2>
            <p className="text-gray-400 text-sm mb-4">
              Build a mod from modified UAsset files
            </p>
            <form onSubmit={handlePackSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Modified UAsset Directory <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  value={packModifiedDir}
                  onChange={(e) => setPackModifiedDir(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="G:\Grounded\Modding\ModFolder"
                  className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                    packErrors.packModifiedDir ? 'border-red-500' : 'border-gray-600'
                  } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                />
                {packErrors.packModifiedDir && (
                  <p className="text-red-400 text-xs mt-1">{packErrors.packModifiedDir}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Mod Output Directory <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  value={packModOutputDir}
                  onChange={(e) => setPackModOutputDir(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="G:\Grounded\Modding\AwesomeMod"
                  className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                    packErrors.packModOutputDir ? 'border-red-500' : 'border-gray-600'
                  } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                />
                {packErrors.packModOutputDir && (
                  <p className="text-red-400 text-xs mt-1">{packErrors.packModOutputDir}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Mod Name <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  value={packModName}
                  onChange={(e) => setPackModName(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="AwesomeMod"
                  className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                    packErrors.packModName ? 'border-red-500' : 'border-gray-600'
                  } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                />
                {packErrors.packModName && (
                  <p className="text-red-400 text-xs mt-1">{packErrors.packModName}</p>
                )}
                <p className="text-gray-500 text-xs mt-1">
                  Output will be: {packModOutputDir || '<output-dir>'}\{packModName || '<mod-name>'}.utoc
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  UE Version <span className="text-red-400">*</span>
                </label>
                <select
                  value={packEngineVersion}
                  onChange={(e) => setPackEngineVersion(e.target.value)}
                  disabled={isSubmitting}
                  className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                    packErrors.packEngineVersion ? 'border-red-500' : 'border-gray-600'
                  } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                >
                  {UE_VERSIONS.map((v) => (
                    <option key={v.value} value={v.value}>
                      {v.label}
                    </option>
                  ))}
                </select>
                {packErrors.packEngineVersion && (
                  <p className="text-red-400 text-xs mt-1">{packErrors.packEngineVersion}</p>
                )}
              </div>

              <button
                type="submit"
                disabled={isSubmitting}
                className={`w-full px-4 py-3 rounded font-medium ${
                  isSubmitting
                    ? 'bg-gray-600 cursor-not-allowed'
                    : 'bg-blue-600 hover:bg-blue-700'
                } text-white transition-colors`}
              >
                {isSubmitting ? (
                  <span className="flex items-center justify-center space-x-2">
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                    <span>Building Mod...</span>
                  </span>
                ) : (
                  'Build Mod'
                )}
              </button>
            </form>
          </div>

          {/* Unpack Section */}
          <div className="border border-gray-700 rounded-lg p-6 bg-gray-800">
            <h2 className="text-xl font-semibold mb-4">Unpack (Zen → Legacy)</h2>
            <p className="text-gray-400 text-sm mb-4">
              Extract IoStore containers to editable UAsset files
            </p>
            <form onSubmit={handleUnpackSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Base Game Paks Directory <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  value={unpackPaksDir}
                  onChange={(e) => setUnpackPaksDir(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="E:\SteamLibrary\steamapps\common\Grounded2\Augusta\Content\Paks"
                  className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                    unpackErrors.unpackPaksDir ? 'border-red-500' : 'border-gray-600'
                  } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                />
                {unpackErrors.unpackPaksDir && (
                  <p className="text-red-400 text-xs mt-1">{unpackErrors.unpackPaksDir}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Extracted Output Directory <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  value={unpackOutputDir}
                  onChange={(e) => setUnpackOutputDir(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="G:\Grounded\Modding\Grounded 2_Extracted"
                  className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                    unpackErrors.unpackOutputDir ? 'border-red-500' : 'border-gray-600'
                  } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                />
                {unpackErrors.unpackOutputDir && (
                  <p className="text-red-400 text-xs mt-1">{unpackErrors.unpackOutputDir}</p>
                )}
              </div>

              <button
                type="submit"
                disabled={isSubmitting}
                className={`w-full px-4 py-3 rounded font-medium ${
                  isSubmitting
                    ? 'bg-gray-600 cursor-not-allowed'
                    : 'bg-green-600 hover:bg-green-700'
                } text-white transition-colors`}
              >
                {isSubmitting ? (
                  <span className="flex items-center justify-center space-x-2">
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                    <span>Extracting Files...</span>
                  </span>
                ) : (
                  'Extract Files'
                )}
              </button>
            </form>
          </div>
        </div>

        {/* Right Column: Results & History */}
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
