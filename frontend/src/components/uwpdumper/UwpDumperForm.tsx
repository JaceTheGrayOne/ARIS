import { useState } from 'react';
import type { UwpDumpCommandDto, UwpDumpMode } from '../../types/contracts';

interface UwpDumperFormProps {
  isSubmitting: boolean;
  onSubmit: (command: UwpDumpCommandDto) => void;
}

export function UwpDumperForm({ isSubmitting, onSubmit }: UwpDumperFormProps) {
  const [packageFamilyName, setPackageFamilyName] = useState('');
  const [applicationId, setApplicationId] = useState('');
  const [outputPath, setOutputPath] = useState('');
  const [mode, setMode] = useState<UwpDumpMode>('FullDump');
  const [includeSymbols, setIncludeSymbols] = useState(false);

  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!packageFamilyName.trim()) {
      newErrors.packageFamilyName = 'Package family name is required';
    }

    if (!outputPath.trim()) {
      newErrors.outputPath = 'Output path is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    const command: UwpDumpCommandDto = {
      packageFamilyName: packageFamilyName.trim(),
      applicationId: applicationId.trim() || null,
      outputPath: outputPath.trim(),
      mode,
      includeSymbols,
    };

    onSubmit(command);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Package Family Name <span className="text-red-400">*</span>
        </label>
        <input
          type="text"
          value={packageFamilyName}
          onChange={(e) => setPackageFamilyName(e.target.value)}
          disabled={isSubmitting}
          placeholder="e.g., Microsoft.WindowsCalculator_8wekyb3d8bbwe"
          className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
            errors.packageFamilyName ? 'border-red-500' : 'border-gray-600'
          } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
        />
        {errors.packageFamilyName && (
          <p className="text-red-400 text-xs mt-1">{errors.packageFamilyName}</p>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Application ID
        </label>
        <input
          type="text"
          value={applicationId}
          onChange={(e) => setApplicationId(e.target.value)}
          disabled={isSubmitting}
          placeholder="Optional - leave empty for main app"
          className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
            isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
          }`}
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Output Path <span className="text-red-400">*</span>
        </label>
        <input
          type="text"
          value={outputPath}
          onChange={(e) => setOutputPath(e.target.value)}
          disabled={isSubmitting}
          placeholder="C:/path/to/output"
          className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
            errors.outputPath ? 'border-red-500' : 'border-gray-600'
          } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
        />
        {errors.outputPath && (
          <p className="text-red-400 text-xs mt-1">{errors.outputPath}</p>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Dump Mode <span className="text-red-400">*</span>
        </label>
        <select
          value={mode}
          onChange={(e) => setMode(e.target.value as UwpDumpMode)}
          disabled={isSubmitting}
          className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
            isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
          }`}
        >
          <option value="FullDump">Full Dump (extract all files)</option>
          <option value="MetadataOnly">Metadata Only (manifest and info)</option>
          <option value="ValidateOnly">Validate Only (check package integrity)</option>
        </select>
      </div>

      <div className="flex items-center space-x-2">
        <input
          type="checkbox"
          id="includeSymbols"
          checked={includeSymbols}
          onChange={(e) => setIncludeSymbols(e.target.checked)}
          disabled={isSubmitting}
          className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
        />
        <label htmlFor="includeSymbols" className="text-sm text-gray-300">
          Include debug symbols (.pdb files)
        </label>
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
            <span>Running Dump...</span>
          </span>
        ) : (
          'Run UWP Dump'
        )}
      </button>
    </form>
  );
}
