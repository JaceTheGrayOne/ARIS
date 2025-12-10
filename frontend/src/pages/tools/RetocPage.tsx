import { RetocTestPanel } from '../../components/RetocTestPanel';

export function RetocPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">IoStore / Retoc</h1>
        <p className="text-gray-400 mt-2">Convert between Pak and IoStore formats</p>
      </div>

      <RetocTestPanel />

      <div className="border border-gray-700 rounded p-6 bg-gray-800">
        <h2 className="text-xl font-semibold mb-4">About Retoc</h2>
        <p className="text-gray-400 text-sm">
          Retoc enables conversion between Unreal Engine Pak and IoStore container formats.
          Use this tool to extract, convert, or repack game assets.
        </p>
      </div>
    </div>
  );
}
