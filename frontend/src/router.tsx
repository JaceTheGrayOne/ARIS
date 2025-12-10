import { createBrowserRouter } from 'react-router-dom';
import { MainLayout } from './layouts/MainLayout';
import { DashboardPage } from './pages/DashboardPage';
import { SystemHealthPage } from './pages/SystemHealthPage';
import { SettingsPage } from './pages/SettingsPage';
import { LogsPage } from './pages/LogsPage';
import { RetocPage } from './pages/tools/RetocPage';
import { UAssetPage } from './pages/tools/UAssetPage';
import { UwpDumperPage } from './pages/tools/UwpDumperPage';
import { DllInjectorPage } from './pages/tools/DllInjectorPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        index: true,
        element: <DashboardPage />,
      },
      {
        path: 'tools/retoc',
        element: <RetocPage />,
      },
      {
        path: 'tools/uasset',
        element: <UAssetPage />,
      },
      {
        path: 'tools/uwpdumper',
        element: <UwpDumperPage />,
      },
      {
        path: 'tools/dllinjector',
        element: <DllInjectorPage />,
      },
      {
        path: 'system/health',
        element: <SystemHealthPage />,
      },
      {
        path: 'settings',
        element: <SettingsPage />,
      },
      {
        path: 'logs',
        element: <LogsPage />,
      },
    ],
  },
]);
