import { forwardRef } from 'react';
import type { ButtonHTMLAttributes } from 'react';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'accent' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  fullWidth?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant = 'primary', size = 'md', fullWidth = false, className = '', children, disabled, ...props }, ref) => {
    const baseStyles = 'inline-flex items-center justify-center font-medium transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-[var(--bg-base)]';

    const variantStyles = {
      primary: 'bg-[var(--module-accent,#3b82f6)] text-white hover:opacity-90 focus:ring-[var(--module-accent,#3b82f6)] shadow-sm',
      secondary: 'bg-[var(--bg-panel-2)] text-[var(--text-primary)] border border-[var(--border-default)] hover:bg-[var(--bg-panel)] focus:ring-[var(--border-strong)]',
      ghost: 'bg-transparent text-[var(--text-secondary)] hover:bg-[var(--bg-panel-2)] hover:text-[var(--text-primary)] focus:ring-[var(--border-strong)]',
      accent: 'bg-transparent text-[var(--module-accent,#3b82f6)] border border-[var(--module-accent,#3b82f6)] hover:bg-[var(--module-accent,#3b82f6)] hover:text-white focus:ring-[var(--module-accent,#3b82f6)]',
      danger: 'bg-[var(--status-error)] text-white hover:opacity-90 focus:ring-[var(--status-error)] shadow-sm',
    };

    const sizeStyles = {
      sm: 'px-3 py-1.5 text-sm rounded-sm',
      md: 'px-4 py-2.5 text-base rounded-md',
      lg: 'px-6 py-3 text-lg rounded-md',
    };

    const widthStyles = fullWidth ? 'w-full' : '';

    const disabledStyles = disabled
      ? 'opacity-50 cursor-not-allowed pointer-events-none'
      : '';

    const combinedClassName = `${baseStyles} ${variantStyles[variant]} ${sizeStyles[size]} ${widthStyles} ${disabledStyles} ${className}`.trim();

    return (
      <button ref={ref} className={combinedClassName} disabled={disabled} {...props}>
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';
