export function UAssetPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">UAsset</h1>
        <p className="text-gray-400 mt-2">Serialize and deserialize UAsset files</p>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Configuration</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-2">Mode</label>
            <select className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white">
              <option>Deserialize (Asset to JSON)</option>
              <option>Serialize (JSON to Asset)</option>
              <option>Inspect</option>
            </select>
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-2">Input Path</label>
            <input
              type="text"
              placeholder="Select asset or JSON file"
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            />
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-2">Output Path</label>
            <input
              type="text"
              placeholder="Select output location"
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            />
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-2">UE Version</label>
            <input
              type="text"
              placeholder="5.3"
              className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded text-white"
            />
          </div>

          <button className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded">
            Run Operation
          </button>
        </div>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">About UAsset</h2>
        <p className="text-gray-400 text-sm">
          UAsset tools allow you to serialize Unreal Engine assets to JSON for inspection or modification,
          and deserialize them back to binary format.
        </p>
      </div>
    </div>
  );
}
