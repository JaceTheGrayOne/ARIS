import type { ReactNode } from 'react';

export interface FieldProps {
  label: string;
  htmlFor?: string;
  required?: boolean;
  error?: string;
  help?: string;
  children: ReactNode;
}

export function Field({ label, htmlFor, required = false, error, help, children }: FieldProps) {
  return (
    <div className="space-y-2">
      <label htmlFor={htmlFor} className="block text-sm font-medium text-[var(--text-secondary)]">
        {label}
        {required && <span className="text-[var(--status-error)] ml-1">*</span>}
      </label>

      {children}

      {help && !error && (
        <p className="text-xs text-[var(--text-muted)] font-mono">
          {help}
        </p>
      )}

      {error && (
        <p className="text-xs text-[var(--status-error)] flex items-center gap-1">
          <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
          </svg>
          {error}
        </p>
      )}
    </div>
  );
}
