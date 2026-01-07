import type { HTMLAttributes, ReactNode } from 'react';

export type BadgeVariant = 'default' | 'success' | 'error' | 'warning' | 'info';

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
}

export function Badge({ variant = 'default', className = '', children, ...props }: BadgeProps) {
  const variantStyles = {
    default: 'bg-[var(--bg-panel-2)] text-[var(--text-secondary)] border-[var(--border-default)]',
    success: 'bg-[var(--status-success)]/10 text-[var(--status-success)] border-[var(--status-success)]/30',
    error: 'bg-[var(--status-error)]/10 text-[var(--status-error)] border-[var(--status-error)]/30',
    warning: 'bg-[var(--status-warning)]/10 text-[var(--status-warning)] border-[var(--status-warning)]/30',
    info: 'bg-[var(--status-info)]/10 text-[var(--status-info)] border-[var(--status-info)]/30',
  };

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-sm text-xs font-medium border ${variantStyles[variant]} ${className}`}
      {...props}
    >
      {children}
    </span>
  );
}

export interface StatusPillProps {
  status: 'success' | 'error' | 'warning' | 'pending';
  children: ReactNode;
}

export function StatusPill({ status, children }: StatusPillProps) {
  const statusStyles = {
    success: 'bg-[var(--status-success)]/10 text-[var(--status-success)] border-[var(--status-success)]/30',
    error: 'bg-[var(--status-error)]/10 text-[var(--status-error)] border-[var(--status-error)]/30',
    warning: 'bg-[var(--status-warning)]/10 text-[var(--status-warning)] border-[var(--status-warning)]/30',
    pending: 'bg-[var(--bg-panel-2)] text-[var(--text-muted)] border-[var(--border-default)]',
  };

  return (
    <span className={`inline-flex items-center px-2.5 py-1 rounded-md text-xs font-semibold border ${statusStyles[status]}`}>
      {children}
    </span>
  );
}
