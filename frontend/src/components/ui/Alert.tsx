import type { HTMLAttributes, ReactNode } from 'react';
import { AlertCircle, CheckCircle, Info, AlertTriangle } from 'lucide-react';

export type AlertVariant = 'info' | 'success' | 'warning' | 'error';

export interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  variant?: AlertVariant;
  title?: string;
  children: ReactNode;
}

export function Alert({ variant = 'info', title, children, className = '', ...props }: AlertProps) {
  const config = {
    info: {
      icon: Info,
      styles: 'bg-[var(--status-info)]/10 border-[var(--status-info)]/30 text-[var(--status-info)]',
    },
    success: {
      icon: CheckCircle,
      styles: 'bg-[var(--status-success)]/10 border-[var(--status-success)]/30 text-[var(--status-success)]',
    },
    warning: {
      icon: AlertTriangle,
      styles: 'bg-[var(--status-warning)]/10 border-[var(--status-warning)]/30 text-[var(--status-warning)]',
    },
    error: {
      icon: AlertCircle,
      styles: 'bg-[var(--status-error)]/10 border-[var(--status-error)]/30 text-[var(--status-error)]',
    },
  };

  const { icon: Icon, styles } = config[variant];

  return (
    <div
      className={`flex gap-3 p-4 rounded-md border ${styles} ${className}`}
      {...props}
    >
      <Icon size={20} className="flex-shrink-0 mt-0.5" />
      <div className="flex-1 min-w-0">
        {title && (
          <p className="font-semibold text-sm mb-1">
            {title}
          </p>
        )}
        <div className="text-sm text-[var(--text-secondary)]">
          {children}
        </div>
      </div>
    </div>
  );
}
