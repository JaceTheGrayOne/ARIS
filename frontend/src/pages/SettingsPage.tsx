export function SettingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Settings</h1>
        <p className="text-gray-400 mt-2">Application preferences and configuration</p>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Appearance</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-2">Theme</label>
            <select className="px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white">
              <option>Dark (default)</option>
              <option>Dark High Contrast</option>
            </select>
          </div>
        </div>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Tool Defaults</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-2">Default UE Version</label>
            <input
              type="text"
              placeholder="5.3"
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            />
          </div>
          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="keep-temp"
              className="w-4 h-4 bg-gray-900 border-gray-600 rounded"
            />
            <label htmlFor="keep-temp" className="text-sm text-gray-300">
              Keep temporary files on failure
            </label>
          </div>
        </div>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Logging</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-2">Verbosity Level</label>
            <select className="px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white">
              <option>Error</option>
              <option>Warning</option>
              <option>Information</option>
              <option>Debug</option>
              <option>Trace</option>
            </select>
          </div>
        </div>
      </div>
    </div>
  );
}
