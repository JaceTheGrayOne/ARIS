import { forwardRef } from 'react';
import type { InputHTMLAttributes } from 'react';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ error = false, className = '', ...props }, ref) => {
    const baseStyles = 'w-full px-3 py-2 bg-[var(--bg-inset)] border rounded-md text-[var(--text-primary)] text-sm font-mono placeholder:text-[var(--text-muted)] placeholder:font-body transition-all duration-200';
    const focusStyles = 'focus:outline-none focus:ring-2 focus:ring-[var(--module-accent,#3b82f6)] focus:border-transparent';
    const errorStyles = error
      ? 'border-[var(--status-error)] focus:ring-[var(--status-error)]'
      : 'border-[var(--border-default)]';
    const disabledStyles = 'disabled:opacity-50 disabled:cursor-not-allowed';

    return (
      <input
        ref={ref}
        className={`${baseStyles} ${focusStyles} ${errorStyles} ${disabledStyles} ${className}`}
        {...props}
      />
    );
  }
);

Input.displayName = 'Input';
