import { useEffect, useState, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { Button } from './Button';

export interface MarkdownFlyoutProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  /** Async function that returns markdown content */
  loadContent: () => Promise<string>;
}

/**
 * A modal flyout that displays markdown/text content.
 * Renders OUTSIDE the terminal stream as a dismissible overlay.
 */
export function MarkdownFlyout({ isOpen, onClose, title, loadContent }: MarkdownFlyoutProps) {
  const [content, setContent] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const fetchContent = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const text = await loadContent();
      setContent(text);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(`Failed to load content: ${message}`);
    } finally {
      setIsLoading(false);
    }
  }, [loadContent]);

  useEffect(() => {
    if (isOpen && !content && !error) {
      fetchContent();
    }
  }, [isOpen, content, error, fetchContent]);

  // Handle Escape key to close
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  // Use portal to escape parent stacking contexts
  // Using inline styles to bypass Tailwind v4 inset-0 issue
  return createPortal(
    <div
      id="markdown-flyout-overlay"
      data-title={title}
      style={{
        position: 'fixed',
        top: 0,
        right: 0,
        bottom: 0,
        left: 0,
        zIndex: 9999,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgba(0, 0, 0, 0.6)',
      }}
      onClick={(e) => {
        // Close on backdrop click
        if (e.target === e.currentTarget) {
          onClose();
        }
      }}
    >
      <div className="bg-[var(--bg-panel)] border-2 border-[var(--module-accent,var(--accent-retoc))] rounded-lg shadow-2xl max-w-4xl max-h-[85vh] w-full mx-4 flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-[var(--border-default)]">
          <h2 className="font-display text-xl font-bold text-[var(--text-primary)]">
            {title}
          </h2>
          <button
            onClick={onClose}
            className="p-1 text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-[var(--bg-panel-2)] rounded transition-colors"
            aria-label="Close"
          >
            <X size={22} />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-5">
          {isLoading && (
            <div className="flex items-center gap-2 text-[var(--text-muted)]">
              <span className="animate-pulse">Loading...</span>
            </div>
          )}

          {error && (
            <div className="bg-red-500/10 border border-red-500/30 text-red-400 rounded-md p-4">
              {error}
            </div>
          )}

          {content && (
            <div className="bg-[var(--bg-inset)] border border-[var(--border-default)] rounded-md p-4">
              <pre className="font-mono text-sm text-[var(--text-primary)] whitespace-pre-wrap leading-relaxed">
                {content}
              </pre>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-5 py-4 border-t border-[var(--border-default)] flex justify-end">
          <Button variant="primary" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </div>,
    document.body
  );
}
