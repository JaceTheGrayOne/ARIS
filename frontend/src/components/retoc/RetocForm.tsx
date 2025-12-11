import { useState } from 'react';
import type { RetocConvertRequest } from '../../types/contracts';

interface RetocFormProps {
  onSubmit: (request: RetocConvertRequest) => void;
  isSubmitting: boolean;
}

const RETOC_MODES = [
  { value: 'PakToIoStore', label: 'Pak → IoStore' },
  { value: 'IoStoreToPak', label: 'IoStore → Pak' },
  { value: 'Repack', label: 'Repack' },
  { value: 'Validate', label: 'Validate' },
];

export function RetocForm({ onSubmit, isSubmitting }: RetocFormProps) {
  const [inputPath, setInputPath] = useState('');
  const [outputPath, setOutputPath] = useState('');
  const [mode, setMode] = useState('PakToIoStore');
  const [ueVersion, setUeVersion] = useState('');
  const [compressionFormat, setCompressionFormat] = useState('');

  const [errors, setErrors] = useState<Record<string, string>>({});

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!inputPath.trim()) {
      newErrors.inputPath = 'Input path is required';
    }

    if (!outputPath.trim()) {
      newErrors.outputPath = 'Output path is required';
    }

    if (!mode) {
      newErrors.mode = 'Mode is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    const request: RetocConvertRequest = {
      inputPath: inputPath.trim(),
      outputPath: outputPath.trim(),
      mode,
      ueVersion: ueVersion.trim() || undefined,
      compressionFormat: compressionFormat.trim() || undefined,
    };

    onSubmit(request);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Input Path <span className="text-red-400">*</span>
        </label>
        <input
          type="text"
          value={inputPath}
          onChange={(e) => setInputPath(e.target.value)}
          disabled={isSubmitting}
          placeholder="C:/path/to/input.pak"
          className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
            errors.inputPath ? 'border-red-500' : 'border-gray-600'
          } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
        />
        {errors.inputPath && (
          <p className="text-red-400 text-xs mt-1">{errors.inputPath}</p>
        )}
        <p className="text-gray-500 text-xs mt-1">
          Use absolute paths until workspace-relative paths are implemented
        </p>
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
          Mode <span className="text-red-400">*</span>
        </label>
        <select
          value={mode}
          onChange={(e) => setMode(e.target.value)}
          disabled={isSubmitting}
          className={`w-full px-3 py-2 bg-gray-900 border rounded text-white ${
            errors.mode ? 'border-red-500' : 'border-gray-600'
          } ${isSubmitting ? 'opacity-50 cursor-not-allowed' : ''}`}
        >
          {RETOC_MODES.map((m) => (
            <option key={m.value} value={m.value}>
              {m.label}
            </option>
          ))}
        </select>
        {errors.mode && (
          <p className="text-red-400 text-xs mt-1">{errors.mode}</p>
        )}
      </div>

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
        <p className="text-gray-500 text-xs mt-1">Optional</p>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Compression Format
        </label>
        <input
          type="text"
          value={compressionFormat}
          onChange={(e) => setCompressionFormat(e.target.value)}
          disabled={isSubmitting}
          placeholder="Zlib, Gzip, etc."
          className={`w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white ${
            isSubmitting ? 'opacity-50 cursor-not-allowed' : ''
          }`}
        />
        <p className="text-gray-500 text-xs mt-1">Optional</p>
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
            <span>Running Conversion...</span>
          </span>
        ) : (
          'Run Conversion'
        )}
      </button>
    </form>
  );
}
