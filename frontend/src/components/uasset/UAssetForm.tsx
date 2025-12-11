import { useState } from 'react';
import type {
  UAssetSerializeRequest,
  UAssetDeserializeRequest,
  UAssetInspectRequest,
} from '../../types/contracts';

type UAssetOperationKind = 'serialize' | 'deserialize' | 'inspect';

export type UAssetFormSubmit =
  | { kind: 'serialize'; request: UAssetSerializeRequest }
  | { kind: 'deserialize'; request: UAssetDeserializeRequest }
  | { kind: 'inspect'; request: UAssetInspectRequest };

interface UAssetFormProps {
  isSubmitting: boolean;
  onSubmit: (payload: UAssetFormSubmit) => void;
}

export function UAssetForm({ isSubmitting, onSubmit }: UAssetFormProps) {
  const [activeTab, setActiveTab] = useState<UAssetOperationKind>('deserialize');

  const [inputJsonPath, setInputJsonPath] = useState('');
  const [outputAssetPath, setOutputAssetPath] = useState('');

  const [inputAssetPath, setInputAssetPath] = useState('');
  const [outputJsonPath, setOutputJsonPath] = useState('');
  const [includeBulkData, setIncludeBulkData] = useState(false);

  const [inspectAssetPath, setInspectAssetPath] = useState('');
  const [inspectExports, setInspectExports] = useState(true);
  const [inspectImports, setInspectImports] = useState(true);
  const [inspectNames, setInspectNames] = useState(true);

  const [ueVersion, setUeVersion] = useState('');
  const [schemaVersion, setSchemaVersion] = useState('');

  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (activeTab === 'serialize') {
      if (!inputJsonPath.trim()) {
        newErrors.inputJsonPath = 'Input JSON path is required';
      }
      if (!outputAssetPath.trim()) {
        newErrors.outputAssetPath = 'Output asset path is required';
      }
    } else if (activeTab === 'deserialize') {
      if (!inputAssetPath.trim()) {
        newErrors.inputAssetPath = 'Input asset path is required';
      }
      if (!outputJsonPath.trim()) {
        newErrors.outputJsonPath = 'Output JSON path is required';
      }
    } else if (activeTab === 'inspect') {
      if (!inspectAssetPath.trim()) {
        newErrors.inspectAssetPath = 'Input asset path is required';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    if (activeTab === 'serialize') {
      const request: UAssetSerializeRequest = {
        inputJsonPath: inputJsonPath.trim(),
        outputAssetPath: outputAssetPath.trim(),
        ueVersion: ueVersion.trim() || undefined,
        schemaVersion: schemaVersion.trim() || undefined,
      };
      onSubmit({ kind: 'serialize', request });
    } else if (activeTab === 'deserialize') {
      const request: UAssetDeserializeRequest = {
        inputAssetPath: inputAssetPath.trim(),
        outputJsonPath: outputJsonPath.trim(),
        ueVersion: ueVersion.trim() || undefined,
        schemaVersion: schemaVersion.trim() || undefined,
        includeBulkData,
      };
      onSubmit({ kind: 'deserialize', request });
    } else if (activeTab === 'inspect') {
      const fields: string[] = [];
      if (inspectExports) fields.push('exports');
      if (inspectImports) fields.push('imports');
      if (inspectNames) fields.push('names');

      const request: UAssetInspectRequest = {
        inputAssetPath: inspectAssetPath.trim(),
        fields: fields.length > 0 ? fields : undefined,
      };
      onSubmit({ kind: 'inspect', request });
    }
  };

  const tabs: { id: UAssetOperationKind; label: string }[] = [
    { id: 'deserialize', label: 'Deserialize (Asset → JSON)' },
    { id: 'serialize', label: 'Serialize (JSON → Asset)' },
    { id: 'inspect', label: 'Inspect' },
  ];

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="border-b border-gray-700">
        <div className="flex space-x-1">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              disabled={isSubmitting}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-blue-500 text-blue-400'
                  : 'border-transparent text-gray-400 hover:text-gray-300'
              } ${isSubmitting ? 'cursor-not-allowed opacity-50' : ''}`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      <div className="space-y-4">
        {activeTab === 'serialize' && (
          <>
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Input JSON Path <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={inputJsonPath}
                onChange={(e) => setInputJsonPath(e.target.value)}
                disabled={isSubmitting}
                placeholder="C:/path/to/input.json"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.inputJsonPath ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.inputJsonPath && (
                <p className="text-red-400 text-xs mt-1">{errors.inputJsonPath}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Output Asset Path <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={outputAssetPath}
                onChange={(e) => setOutputAssetPath(e.target.value)}
                disabled={isSubmitting}
                placeholder="C:/path/to/output.uasset"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.outputAssetPath ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.outputAssetPath && (
                <p className="text-red-400 text-xs mt-1">{errors.outputAssetPath}</p>
              )}
            </div>
          </>
        )}

        {activeTab === 'deserialize' && (
          <>
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Input Asset Path <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={inputAssetPath}
                onChange={(e) => setInputAssetPath(e.target.value)}
                disabled={isSubmitting}
                placeholder="C:/path/to/input.uasset"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.inputAssetPath ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.inputAssetPath && (
                <p className="text-red-400 text-xs mt-1">{errors.inputAssetPath}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Output JSON Path <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={outputJsonPath}
                onChange={(e) => setOutputJsonPath(e.target.value)}
                disabled={isSubmitting}
                placeholder="C:/path/to/output.json"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.outputJsonPath ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.outputJsonPath && (
                <p className="text-red-400 text-xs mt-1">{errors.outputJsonPath}</p>
              )}
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="includeBulkData"
                checked={includeBulkData}
                onChange={(e) => setIncludeBulkData(e.target.checked)}
                disabled={isSubmitting}
                className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
              />
              <label htmlFor="includeBulkData" className="text-sm text-gray-300">
                Include bulk data
              </label>
            </div>
          </>
        )}

        {activeTab === 'inspect' && (
          <>
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Input Asset Path <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={inspectAssetPath}
                onChange={(e) => setInspectAssetPath(e.target.value)}
                disabled={isSubmitting}
                placeholder="C:/path/to/asset.uasset"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.inspectAssetPath ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.inspectAssetPath && (
                <p className="text-red-400 text-xs mt-1">{errors.inspectAssetPath}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Fields to Inspect
              </label>
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="inspectExports"
                    checked={inspectExports}
                    onChange={(e) => setInspectExports(e.target.checked)}
                    disabled={isSubmitting}
                    className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
                  />
                  <label htmlFor="inspectExports" className="text-sm text-gray-300">
                    Exports
                  </label>
                </div>

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="inspectImports"
                    checked={inspectImports}
                    onChange={(e) => setInspectImports(e.target.checked)}
                    disabled={isSubmitting}
                    className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
                  />
                  <label htmlFor="inspectImports" className="text-sm text-gray-300">
                    Imports
                  </label>
                </div>

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="inspectNames"
                    checked={inspectNames}
                    onChange={(e) => setInspectNames(e.target.checked)}
                    disabled={isSubmitting}
                    className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
                  />
                  <label htmlFor="inspectNames" className="text-sm text-gray-300">
                    Names
                  </label>
                </div>
              </div>
            </div>
          </>
        )}

        {(activeTab === 'serialize' || activeTab === 'deserialize') && (
          <>
            <div className="pt-4 border-t border-gray-700">
              <h3 className="text-sm font-medium text-gray-300 mb-3">Optional Settings</h3>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    UE Version
                  </label>
                  <input
                    type="text"
                    value={ueVersion}
                    onChange={(e) => setUeVersion(e.target.value)}
                    disabled={isSubmitting}
                    placeholder="5.3"
                    className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
                      isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
                    }`}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Schema Version
                  </label>
                  <input
                    type="text"
                    value={schemaVersion}
                    onChange={(e) => setSchemaVersion(e.target.value)}
                    disabled={isSubmitting}
                    placeholder="GAME_UE5_3"
                    className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
                      isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
                    }`}
                  />
                </div>
              </div>
            </div>
          </>
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
            <span>Running Operation...</span>
          </span>
        ) : (
          `Run ${activeTab === 'serialize' ? 'Serialization' : activeTab === 'deserialize' ? 'Deserialization' : 'Inspection'}`
        )}
      </button>
    </form>
  );
}
