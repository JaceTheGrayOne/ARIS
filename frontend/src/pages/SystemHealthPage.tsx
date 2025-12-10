import { HealthPanel } from '../components/HealthPanel';
import { SystemDiagnosticsPanel } from '../components/SystemDiagnosticsPanel';

export function SystemHealthPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">System Health</h1>
        <p className="text-gray-400 mt-2">Backend status and diagnostics</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div>
          <SystemDiagnosticsPanel />
        </div>

        <div>
          <HealthPanel />
        </div>
      </div>
    </div>
  );
}
