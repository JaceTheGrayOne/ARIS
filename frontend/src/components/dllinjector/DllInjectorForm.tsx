import { useState } from 'react';
import type {
  DllInjectCommandDto,
  DllEjectCommandDto,
  DllInjectionMethod,
} from '../../types/contracts';

type OperationTab = 'inject' | 'eject';
type TargetMode = 'pid' | 'name';

interface DllInjectorFormProps {
  isSubmitting: boolean;
  onInject: (command: DllInjectCommandDto) => void;
  onEject: (command: DllEjectCommandDto) => void;
}

export function DllInjectorForm({ isSubmitting, onInject, onEject }: DllInjectorFormProps) {
  const [activeTab, setActiveTab] = useState<OperationTab>('inject');

  // Inject fields
  const [injectTargetMode, setInjectTargetMode] = useState<TargetMode>('name');
  const [injectProcessId, setInjectProcessId] = useState('');
  const [injectProcessName, setInjectProcessName] = useState('');
  const [dllPath, setDllPath] = useState('');
  const [method, setMethod] = useState<DllInjectionMethod>('CreateRemoteThread');
  const [injectRequireElevation, setInjectRequireElevation] = useState(false);
  const [injectArguments, setInjectArguments] = useState('');

  // Eject fields
  const [ejectTargetMode, setEjectTargetMode] = useState<TargetMode>('name');
  const [ejectProcessId, setEjectProcessId] = useState('');
  const [ejectProcessName, setEjectProcessName] = useState('');
  const [moduleName, setModuleName] = useState('');
  const [ejectRequireElevation, setEjectRequireElevation] = useState(false);

  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateInject = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (injectTargetMode === 'pid') {
      if (!injectProcessId.trim() || isNaN(Number(injectProcessId))) {
        newErrors.injectProcessId = 'Valid process ID is required';
      }
    } else {
      if (!injectProcessName.trim()) {
        newErrors.injectProcessName = 'Process name is required';
      }
    }

    if (!dllPath.trim()) {
      newErrors.dllPath = 'DLL path is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const validateEject = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (ejectTargetMode === 'pid') {
      if (!ejectProcessId.trim() || isNaN(Number(ejectProcessId))) {
        newErrors.ejectProcessId = 'Valid process ID is required';
      }
    } else {
      if (!ejectProcessName.trim()) {
        newErrors.ejectProcessName = 'Process name is required';
      }
    }

    if (!moduleName.trim()) {
      newErrors.moduleName = 'Module name is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (activeTab === 'inject') {
      if (!validateInject()) return;

      const command: DllInjectCommandDto = {
        processId: injectTargetMode === 'pid' ? Number(injectProcessId) : undefined,
        processName: injectTargetMode === 'name' ? injectProcessName.trim() : undefined,
        dllPath: dllPath.trim(),
        method,
        requireElevation: injectRequireElevation,
        arguments: injectArguments.trim()
          ? injectArguments.split(',').map((arg) => arg.trim())
          : undefined,
      };

      onInject(command);
    } else {
      if (!validateEject()) return;

      const command: DllEjectCommandDto = {
        processId: ejectTargetMode === 'pid' ? Number(ejectProcessId) : undefined,
        processName: ejectTargetMode === 'name' ? ejectProcessName.trim() : undefined,
        moduleName: moduleName.trim(),
        requireElevation: ejectRequireElevation,
      };

      onEject(command);
    }
  };

  const tabs: { id: OperationTab; label: string }[] = [
    { id: 'inject', label: 'Inject DLL' },
    { id: 'eject', label: 'Eject DLL' },
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
        {activeTab === 'inject' && (
          <>
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Target Process <span className="text-red-400">*</span>
              </label>
              <div className="flex space-x-2 mb-2">
                <button
                  type="button"
                  onClick={() => setInjectTargetMode('name')}
                  disabled={isSubmitting}
                  className={`px-3 py-1 rounded text-sm ${
                    injectTargetMode === 'name'
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                  } ${isSubmitting ? 'cursor-not-allowed opacity-50' : ''}`}
                >
                  By Process Name
                </button>
                <button
                  type="button"
                  onClick={() => setInjectTargetMode('pid')}
                  disabled={isSubmitting}
                  className={`px-3 py-1 rounded text-sm ${
                    injectTargetMode === 'pid'
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                  } ${isSubmitting ? 'cursor-not-allowed opacity-50' : ''}`}
                >
                  By Process ID
                </button>
              </div>

              {injectTargetMode === 'name' ? (
                <div>
                  <input
                    type="text"
                    value={injectProcessName}
                    onChange={(e) => setInjectProcessName(e.target.value)}
                    disabled={isSubmitting}
                    placeholder="e.g., Game-Win64-Shipping.exe"
                    className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                      errors.injectProcessName ? 'border-red-500' : 'border-gray-600'
                    } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                  />
                  {errors.injectProcessName && (
                    <p className="text-red-400 text-xs mt-1">{errors.injectProcessName}</p>
                  )}
                </div>
              ) : (
                <div>
                  <input
                    type="number"
                    value={injectProcessId}
                    onChange={(e) => setInjectProcessId(e.target.value)}
                    disabled={isSubmitting}
                    placeholder="e.g., 12345"
                    className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                      errors.injectProcessId ? 'border-red-500' : 'border-gray-600'
                    } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                  />
                  {errors.injectProcessId && (
                    <p className="text-red-400 text-xs mt-1">{errors.injectProcessId}</p>
                  )}
                </div>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                DLL Path <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={dllPath}
                onChange={(e) => setDllPath(e.target.value)}
                disabled={isSubmitting}
                placeholder="C:/Tools/aris.dll"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.dllPath ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.dllPath && (
                <p className="text-red-400 text-xs mt-1">{errors.dllPath}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Injection Method <span className="text-red-400">*</span>
              </label>
              <select
                value={method}
                onChange={(e) => setMethod(e.target.value as DllInjectionMethod)}
                disabled={isSubmitting}
                className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
                  isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
                }`}
              >
                <option value="CreateRemoteThread">CreateRemoteThread</option>
                <option value="ApcQueue">APC Queue</option>
                <option value="ManualMap">Manual Map</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Arguments (comma-separated)
              </label>
              <input
                type="text"
                value={injectArguments}
                onChange={(e) => setInjectArguments(e.target.value)}
                disabled={isSubmitting}
                placeholder="Optional: arg1, arg2, arg3"
                className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
                  isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
                }`}
              />
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="injectRequireElevation"
                checked={injectRequireElevation}
                onChange={(e) => setInjectRequireElevation(e.target.checked)}
                disabled={isSubmitting}
                className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
              />
              <label htmlFor="injectRequireElevation" className="text-sm text-gray-300">
                Require elevation (UAC)
              </label>
            </div>
          </>
        )}

        {activeTab === 'eject' && (
          <>
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Target Process <span className="text-red-400">*</span>
              </label>
              <div className="flex space-x-2 mb-2">
                <button
                  type="button"
                  onClick={() => setEjectTargetMode('name')}
                  disabled={isSubmitting}
                  className={`px-3 py-1 rounded text-sm ${
                    ejectTargetMode === 'name'
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                  } ${isSubmitting ? 'cursor-not-allowed opacity-50' : ''}`}
                >
                  By Process Name
                </button>
                <button
                  type="button"
                  onClick={() => setEjectTargetMode('pid')}
                  disabled={isSubmitting}
                  className={`px-3 py-1 rounded text-sm ${
                    ejectTargetMode === 'pid'
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                  } ${isSubmitting ? 'cursor-not-allowed opacity-50' : ''}`}
                >
                  By Process ID
                </button>
              </div>

              {ejectTargetMode === 'name' ? (
                <div>
                  <input
                    type="text"
                    value={ejectProcessName}
                    onChange={(e) => setEjectProcessName(e.target.value)}
                    disabled={isSubmitting}
                    placeholder="e.g., Game-Win64-Shipping.exe"
                    className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                      errors.ejectProcessName ? 'border-red-500' : 'border-gray-600'
                    } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                  />
                  {errors.ejectProcessName && (
                    <p className="text-red-400 text-xs mt-1">{errors.ejectProcessName}</p>
                  )}
                </div>
              ) : (
                <div>
                  <input
                    type="number"
                    value={ejectProcessId}
                    onChange={(e) => setEjectProcessId(e.target.value)}
                    disabled={isSubmitting}
                    placeholder="e.g., 12345"
                    className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                      errors.ejectProcessId ? 'border-red-500' : 'border-gray-600'
                    } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                  />
                  {errors.ejectProcessId && (
                    <p className="text-red-400 text-xs mt-1">{errors.ejectProcessId}</p>
                  )}
                </div>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Module Name <span className="text-red-400">*</span>
              </label>
              <input
                type="text"
                value={moduleName}
                onChange={(e) => setModuleName(e.target.value)}
                disabled={isSubmitting}
                placeholder="e.g., aris.dll"
                className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
                  errors.moduleName ? 'border-red-500' : 'border-gray-600'
                } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              {errors.moduleName && (
                <p className="text-red-400 text-xs mt-1">{errors.moduleName}</p>
              )}
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="ejectRequireElevation"
                checked={ejectRequireElevation}
                onChange={(e) => setEjectRequireElevation(e.target.checked)}
                disabled={isSubmitting}
                className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
              />
              <label htmlFor="ejectRequireElevation" className="text-sm text-gray-300">
                Require elevation (UAC)
              </label>
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
          `Run ${activeTab === 'inject' ? 'Injection' : 'Ejection'}`
        )}
      </button>
    </form>
  );
}
