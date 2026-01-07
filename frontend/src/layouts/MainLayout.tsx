import { Outlet } from 'react-router-dom';
import { useBackendStatus } from '../hooks/useBackendStatus';
import { AppShell, type NavSection } from '../components/ui';
import {
  LayoutDashboard,
  Package,
  FileCode,
  Syringe,
  Activity,
  Settings,
} from 'lucide-react';

const navSections: NavSection[] = [
  {
    label: 'Overview',
    items: [
      {
        to: '/',
        label: 'Dashboard',
        icon: LayoutDashboard,
      },
    ],
  },
  {
    label: 'Tools',
    items: [
      {
        to: '/tools/retoc',
        label: 'IoStore / Retoc',
        icon: Package,
        accent: '#f59e0b', // amber
      },
      {
        to: '/tools/uasset',
        label: 'UAsset',
        icon: FileCode,
        accent: '#06b6d4', // cyan
      },
      {
        to: '/tools/dllinjector',
        label: 'DLL Injector',
        icon: Syringe,
        accent: '#ec4899', // magenta
      },
    ],
  },
  {
    label: 'System',
    items: [
      {
        to: '/system/health',
        label: 'Health',
        icon: Activity,
      },
      {
        to: '/settings',
        label: 'Settings',
        icon: Settings,
      },
    ],
  },
];

export function MainLayout() {
  // Keep hook call for SystemDiagnosticsPanel and future consumers
  useBackendStatus();

  return (
    <AppShell
      sidebar={{
        title: 'ARIS',
        subtitle: 'Unreal Modding Toolkit',
        sections: navSections,
      }}
    >
      <Outlet />
    </AppShell>
  );
}
