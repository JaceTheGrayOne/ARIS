export function UwpDumperPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">UWP Dumper</h1>
        <p className="text-gray-400 mt-2">Dump UWP application packages</p>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Configuration</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-2">Package Family Name</label>
            <input
              type="text"
              placeholder="Enter PFN or AppId"
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            />
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-2">Mode</label>
            <select className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white">
              <option>Full Dump</option>
              <option>Metadata Only</option>
            </select>
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-2">Output Folder</label>
            <input
              type="text"
              placeholder="Select output folder"
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            />
          </div>

          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="uwp-symbols"
              className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
            />
            <label htmlFor="uwp-symbols" className="text-sm text-gray-300">
              Include symbols
            </label>
          </div>

          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="uwp-elevation"
              className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
              defaultChecked
            />
            <label htmlFor="uwp-elevation" className="text-sm text-gray-300">
              Require elevation (recommended)
            </label>
          </div>

          <button className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded">
            Run Dump
          </button>
        </div>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">About UWP Dumper</h2>
        <p className="text-gray-400 text-sm">
          UWP Dumper extracts application packages from Universal Windows Platform apps.
          Elevation is typically required for this operation.
        </p>
      </div>
    </div>
  );
}
