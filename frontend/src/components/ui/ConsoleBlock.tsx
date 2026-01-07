import type { HTMLAttributes } from 'react';

export interface ConsoleBlockProps extends HTMLAttributes<HTMLDivElement> {
  content: string;
  maxHeight?: string;
  showLineNumbers?: boolean;
}

export function ConsoleBlock({ content, maxHeight = '400px', showLineNumbers = false, className = '', ...props }: ConsoleBlockProps) {
  const lines = content.split('\n');

  return (
    <div
      className={`inset-surface rounded-md overflow-hidden ${className}`}
      {...props}
    >
      <div
        className="overflow-auto font-mono text-xs p-4"
        style={{ maxHeight }}
      >
        {showLineNumbers ? (
          <div className="flex">
            <div className="flex-shrink-0 pr-4 text-[var(--text-muted)] select-none border-r border-[var(--border-subtle)] mr-4">
              {lines.map((_, i) => (
                <div key={i} className="leading-relaxed">
                  {i + 1}
                </div>
              ))}
            </div>
            <div className="flex-1 text-[var(--text-secondary)] leading-relaxed whitespace-pre-wrap break-all">
              {content}
            </div>
          </div>
        ) : (
          <div className="text-[var(--text-secondary)] leading-relaxed whitespace-pre-wrap break-all">
            {content}
          </div>
        )}
      </div>
    </div>
  );
}

export interface CodeBlockProps extends HTMLAttributes<HTMLPreElement> {
  children: string;
  language?: string;
}

export function CodeBlock({ children, language, className = '', ...props }: CodeBlockProps) {
  return (
    <div className="inset-surface rounded-md overflow-hidden">
      {language && (
        <div className="px-4 py-2 border-b border-[var(--border-subtle)] bg-[var(--bg-void)]">
          <span className="text-xs font-mono text-[var(--text-muted)] uppercase tracking-wide">
            {language}
          </span>
        </div>
      )}
      <pre
        className={`font-mono text-xs p-4 overflow-x-auto text-[var(--text-secondary)] leading-relaxed ${className}`}
        {...props}
      >
        <code>{children}</code>
      </pre>
    </div>
  );
}
