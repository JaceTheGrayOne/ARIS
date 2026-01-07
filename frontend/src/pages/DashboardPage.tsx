import { useNavigate } from 'react-router-dom';
import { ModuleCard, Panel, PanelHeader, PanelBody, Button } from '../components/ui';
import { Package, FileCode, Syringe } from 'lucide-react';

export function DashboardPage() {
  const navigate = useNavigate();

  return (
    <div className="space-y-8">
      {/* Page Header */}
      <div>
        <h1 className="font-display text-3xl font-bold text-[var(--text-primary)] mb-2">
          Dashboard
        </h1>
        <p className="text-[var(--text-secondary)]">
          Welcome to ARIS Unreal Engine Modding Toolkit
        </p>
      </div>

      {/* Quick Actions */}
      <Panel>
        <PanelHeader title="Quick Start" />
        <PanelBody>
          <p className="text-[var(--text-secondary)] text-sm leading-relaxed">
            Select a tool from the sidebar to begin modding Unreal Engine games.
          </p>
        </PanelBody>
      </Panel>

      {/* Available Tools */}
      <div>
        <h2 className="font-display text-2xl font-semibold text-[var(--text-primary)] mb-4">
          Available Tools
        </h2>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <ModuleCard
            icon={Package}
            title="IoStore / Retoc"
            description="Convert between legacy Pak and modern IoStore container formats. Pack mods or extract game assets."
            accentColor="#f59e0b"
            onClick={() => navigate('/tools/retoc')}
            actions={
              <Button variant="accent" size="sm" onClick={(e) => { e.stopPropagation(); navigate('/tools/retoc'); }}>
                Open Tool
              </Button>
            }
          />

          <ModuleCard
            icon={FileCode}
            title="UAsset Inspector"
            description="Serialize and deserialize Unreal Engine asset files. Inspect asset metadata and structure in-process."
            accentColor="#06b6d4"
            onClick={() => navigate('/tools/uasset')}
            actions={
              <Button variant="accent" size="sm" onClick={(e) => { e.stopPropagation(); navigate('/tools/uasset'); }}>
                Open Tool
              </Button>
            }
          />

          <ModuleCard
            icon={Syringe}
            title="DLL Injector"
            description="Inject or eject DLL libraries into running game processes for runtime modifications."
            accentColor="#ec4899"
            onClick={() => navigate('/tools/dllinjector')}
            actions={
              <Button variant="accent" size="sm" onClick={(e) => { e.stopPropagation(); navigate('/tools/dllinjector'); }}>
                Open Tool
              </Button>
            }
          />
        </div>
      </div>
    </div>
  );
}
