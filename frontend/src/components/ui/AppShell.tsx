import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import type { LucideIcon } from 'lucide-react';

export interface NavItem {
  to: string;
  label: string;
  icon?: LucideIcon;
  accent?: string;
}

export interface NavSection {
  label: string;
  items: NavItem[];
}

export interface SidebarProps {
  title: string;
  subtitle: string;
  sections: NavSection[];
}

export function Sidebar({ title, subtitle, sections }: SidebarProps) {
  return (
    <aside className="w-60 bg-[var(--bg-panel)] border-r border-[var(--border-default)] flex flex-col shadow-lg">
      {/* Sidebar Header */}
      <div className="p-6 border-b border-[var(--border-default)]">
        <h1 className="font-display text-2xl font-bold text-[var(--text-primary)] tracking-wider">
          {title}
        </h1>
        <p className="text-xs text-[var(--text-muted)] mt-1 tracking-wide uppercase">
          {subtitle}
        </p>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-6 overflow-y-auto">
        {sections.map((section, idx) => (
          <div key={idx}>
            <div className="text-xs font-semibold text-[var(--text-muted)] uppercase tracking-wider mb-2 px-3">
              {section.label}
            </div>
            <div className="space-y-1">
              {section.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) => {
                    const baseStyles = 'flex items-center gap-3 px-3 py-2 rounded-sm text-sm font-medium transition-all duration-200';
                    const activeStyles = isActive
                      ? 'bg-[var(--bg-panel-2)] text-[var(--text-primary)] border-l-2 pl-[10px]'
                      : 'text-[var(--text-secondary)] hover:bg-[var(--bg-panel-2)] hover:text-[var(--text-primary)]';

                    return `${baseStyles} ${activeStyles}`;
                  }}
                  style={({ isActive }) =>
                    isActive && item.accent
                      ? { borderLeftColor: item.accent }
                      : undefined
                  }
                >
                  {item.icon && (
                    <item.icon
                      size={18}
                      strokeWidth={2}
                      style={item.accent ? { color: item.accent } : undefined}
                    />
                  )}
                  {item.label}
                </NavLink>
              ))}
            </div>
          </div>
        ))}
      </nav>
    </aside>
  );
}

export interface AppShellProps {
  sidebar: SidebarProps;
  children: ReactNode;
}

export function AppShell({ sidebar, children }: AppShellProps) {
  return (
    <div className="min-h-screen app-background app-vignette flex">
      <Sidebar {...sidebar} />

      <div className="flex-1 flex flex-col min-w-0">
        <main className="flex-1 overflow-y-auto">
          <div className="max-w-7xl mx-auto p-6">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}
