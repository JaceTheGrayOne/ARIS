import { forwardRef } from 'react';
import type { TextareaHTMLAttributes } from 'react';

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ error = false, className = '', ...props }, ref) => {
    const baseStyles = 'w-full px-3 py-2 bg-[var(--bg-inset)] border rounded-md text-[var(--text-primary)] text-sm font-mono placeholder:text-[var(--text-muted)] placeholder:font-body transition-all duration-200 resize-y min-h-[80px]';
    const focusStyles = 'focus:outline-none focus:ring-2 focus:ring-[var(--module-accent,#3b82f6)] focus:border-transparent';
    const errorStyles = error
      ? 'border-[var(--status-error)] focus:ring-[var(--status-error)]'
      : 'border-[var(--border-default)]';
    const disabledStyles = 'disabled:opacity-50 disabled:cursor-not-allowed';

    return (
      <textarea
        ref={ref}
        className={`${baseStyles} ${focusStyles} ${errorStyles} ${disabledStyles} ${className}`}
        {...props}
      />
    );
  }
);

Textarea.displayName = 'Textarea';
