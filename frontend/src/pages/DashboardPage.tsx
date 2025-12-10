export function DashboardPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <p className="text-gray-400 mt-2">Welcome to ARIS</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="border border-gray-700 rounded p-6 bg-gray-800">
          <h2 className="text-xl font-semibold mb-4">Quick Start</h2>
          <p className="text-gray-400 text-sm mb-4">
            Configure a workspace and select a tool from the sidebar to begin.
          </p>
          <button className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded text-sm">
            Open Workspace
          </button>
        </div>

        <div className="border border-gray-700 rounded p-6 bg-gray-800">
          <h2 className="text-xl font-semibold mb-4">Recent Operations</h2>
          <p className="text-gray-400 text-sm">No recent operations</p>
        </div>
      </div>

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">Available Tools</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="p-4 bg-gray-900 rounded">
            <h3 className="font-semibold mb-2">IoStore / Retoc</h3>
            <p className="text-sm text-gray-400">Convert between Pak and IoStore formats</p>
          </div>
          <div className="p-4 bg-gray-900 rounded">
            <h3 className="font-semibold mb-2">UAsset</h3>
            <p className="text-sm text-gray-400">Serialize and deserialize UAsset files</p>
          </div>
          <div className="p-4 bg-gray-900 rounded">
            <h3 className="font-semibold mb-2">UWP Dumper</h3>
            <p className="text-sm text-gray-400">Dump UWP application packages</p>
          </div>
          <div className="p-4 bg-gray-900 rounded">
            <h3 className="font-semibold mb-2">DLL Injector</h3>
            <p className="text-sm text-gray-400">Inject and eject DLLs into processes</p>
          </div>
        </div>
      </div>
    </div>
  );
}
