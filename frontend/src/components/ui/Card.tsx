import type { HTMLAttributes, ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  hover?: boolean;
  accent?: boolean;
}

export function Card({ children, hover = false, accent = false, className = '', ...props }: CardProps) {
  const hoverStyles = hover ? 'hover:border-[var(--module-accent,var(--border-strong))] hover:shadow-lg transition-all duration-200 cursor-pointer' : '';
  const accentStyles = accent ? 'border-[var(--module-accent,var(--border-default))]' : 'border-[var(--border-default)]';

  return (
    <div
      className={`bg-[var(--bg-panel)] border ${accentStyles} rounded-md shadow-sm ${hoverStyles} ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}

export interface ModuleCardProps {
  icon: LucideIcon;
  title: string;
  description: string;
  accentColor: string;
  actions?: ReactNode;
  onClick?: () => void;
}

export function ModuleCard({ icon: Icon, title, description, accentColor, actions, onClick }: ModuleCardProps) {
  return (
    <Card
      hover={!!onClick}
      className="p-6 group"
      style={{ '--module-accent': accentColor } as React.CSSProperties}
      onClick={onClick}
    >
      <div className="flex items-start gap-4">
        <div
          className="flex-shrink-0 w-12 h-12 rounded-md flex items-center justify-center border-2 transition-colors duration-200"
          style={{
            borderColor: accentColor,
            color: accentColor,
            backgroundColor: `${accentColor}15`,
          }}
        >
          <Icon size={24} strokeWidth={2} />
        </div>

        <div className="flex-1 min-w-0">
          <h3
            className="font-display text-lg font-semibold mb-1 transition-colors duration-200"
            style={{ color: onClick ? accentColor : 'var(--text-primary)' }}
          >
            {title}
          </h3>
          <p className="text-sm text-[var(--text-secondary)] leading-relaxed">
            {description}
          </p>

          {actions && (
            <div className="mt-4 flex items-center gap-2">
              {actions}
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}
