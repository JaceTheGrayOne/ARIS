import { HealthPanel } from './components/HealthPanel';
import { RetocTestPanel } from './components/RetocTestPanel';

function App() {
  return (
    <div className="min-h-screen bg-gray-900 text-white p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <header className="mb-8">
          <h1 className="text-3xl font-bold">ARIS - Phase 5 Minimal UI</h1>
          <p className="text-gray-400 mt-2">Backend wiring test harness</p>
        </header>

        <HealthPanel />

        <RetocTestPanel />
      </div>
    </div>
  );
}

export default App;
