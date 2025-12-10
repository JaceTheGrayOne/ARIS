import { Outlet, NavLink } from 'react-router-dom';

interface NavItem {
  to: string;
  label: string;
  section?: boolean;
}

const navItems: NavItem[] = [
  { to: '/', label: 'Dashboard' },
  { to: '', label: 'Tools', section: true },
  { to: '/tools/retoc', label: 'IoStore / Retoc' },
  { to: '/tools/uasset', label: 'UAsset' },
  { to: '/tools/uwpdumper', label: 'UWP Dumper' },
  { to: '/tools/dllinjector', label: 'DLL Injector' },
  { to: '', label: 'System', section: true },
  { to: '/system/health', label: 'Health' },
  { to: '/settings', label: 'Settings' },
  { to: '/logs', label: 'Logs' },
];

export function MainLayout() {
  return (
    <div className="min-h-screen bg-gray-900 text-white flex">
      <aside className="w-64 bg-gray-800 border-r border-gray-700 flex flex-col">
        <div className="p-4 border-b border-gray-700">
          <h1 className="text-xl font-bold">ARIS</h1>
          <p className="text-xs text-gray-400 mt-1">Unreal Modding Toolkit</p>
        </div>

        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
          {navItems.map((item, index) => {
            if (item.section) {
              return (
                <div
                  key={index}
                  className="text-xs font-semibold text-gray-400 uppercase tracking-wide mt-4 mb-2"
                >
                  {item.label}
                </div>
              );
            }

            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  `block px-3 py-2 rounded transition-colors ${
                    isActive
                      ? 'bg-blue-600 text-white'
                      : 'text-gray-300 hover:bg-gray-700 hover:text-white'
                  }`
                }
              >
                {item.label}
              </NavLink>
            );
          })}
        </nav>
      </aside>

      <div className="flex-1 flex flex-col">
        <header className="bg-gray-800 border-b border-gray-700 px-6 py-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <div>
                <span className="text-sm text-gray-400">Workspace:</span>
                <span className="ml-2 text-sm text-white">Not configured</span>
              </div>
            </div>

            <div className="flex items-center space-x-3">
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 rounded-full bg-green-500"></div>
                <span className="text-sm text-gray-300">Backend Ready</span>
              </div>
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto">
          <div className="max-w-6xl mx-auto p-6">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
