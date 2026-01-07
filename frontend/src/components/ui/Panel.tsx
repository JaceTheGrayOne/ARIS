import type { HTMLAttributes, ReactNode } from 'react';

export interface PanelProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

export function Panel({ children, className = '', ...props }: PanelProps) {
  return (
    <div
      className={`glass-panel rounded-lg ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}

export interface PanelHeaderProps extends HTMLAttributes<HTMLDivElement> {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  accent?: boolean;
}

export function PanelHeader({ title, subtitle, actions, accent = false, className = '', ...props }: PanelHeaderProps) {
  return (
    <div
      className={`px-6 py-4 border-b ${accent ? 'border-b-2 border-[var(--module-accent,var(--border-default))]' : 'border-[var(--border-default)]'} ${className}`}
      {...props}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <h2 className="font-display text-xl font-semibold text-[var(--text-primary)] tracking-wide">
            {title}
          </h2>
          {subtitle && (
            <p className="mt-1 text-sm text-[var(--text-secondary)]">
              {subtitle}
            </p>
          )}
        </div>
        {actions && (
          <div className="flex items-center gap-2 flex-shrink-0">
            {actions}
          </div>
        )}
      </div>
    </div>
  );
}

export interface PanelBodyProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  padding?: 'none' | 'sm' | 'md' | 'lg';
}

export function PanelBody({ children, padding = 'md', className = '', ...props }: PanelBodyProps) {
  const paddingStyles = {
    none: '',
    sm: 'p-4',
    md: 'p-6',
    lg: 'p-8',
  };

  return (
    <div
      className={`${paddingStyles[padding]} ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}
