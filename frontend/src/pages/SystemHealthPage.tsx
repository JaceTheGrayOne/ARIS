import { HealthPanel } from '../components/HealthPanel';

export function SystemHealthPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">System Health</h1>
        <p className="text-gray-400 mt-2">Backend status and diagnostics</p>
      </div>

      <HealthPanel />

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">System Information</h2>
        <p className="text-gray-400 text-sm">
          Additional diagnostics and system information will be available here.
        </p>
      </div>
    </div>
  );
}
