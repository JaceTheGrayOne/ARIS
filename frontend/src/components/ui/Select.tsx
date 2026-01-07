import { forwardRef } from 'react';
import type { SelectHTMLAttributes } from 'react';

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  error?: boolean;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ error = false, className = '', children, ...props }, ref) => {
    const baseStyles = 'w-full px-3 py-2 bg-[var(--bg-inset)] border rounded-md text-[var(--text-primary)] text-sm transition-all duration-200 appearance-none cursor-pointer';
    const focusStyles = 'focus:outline-none focus:ring-2 focus:ring-[var(--module-accent,#3b82f6)] focus:border-transparent';
    const errorStyles = error
      ? 'border-[var(--status-error)] focus:ring-[var(--status-error)]'
      : 'border-[var(--border-default)]';
    const disabledStyles = 'disabled:opacity-50 disabled:cursor-not-allowed';

    return (
      <div className="relative">
        <select
          ref={ref}
          className={`${baseStyles} ${focusStyles} ${errorStyles} ${disabledStyles} ${className}`}
          {...props}
        >
          {children}
        </select>
        <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-2 text-[var(--text-muted)]">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>
    );
  }
);

Select.displayName = 'Select';
