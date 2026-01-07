import { useEffect, useState } from 'react';
import { getRetocHelp } from '../../api/retocClient';
import { Button, Alert } from '../ui';
import { X } from 'lucide-react';

interface RetocHelpModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function RetocHelpModal({ isOpen, onClose }: RetocHelpModalProps) {
  const [helpMarkdown, setHelpMarkdown] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isOpen && !helpMarkdown && !error) {
      loadHelp();
    }
  }, [isOpen, helpMarkdown, error]);

  const loadHelp = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await getRetocHelp();
      setHelpMarkdown(response.markdown);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(`Failed to load help: ${message}`);
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-[var(--bg-panel)] border-2 border-[var(--module-accent)] rounded-lg shadow-xl max-w-4xl max-h-[80vh] w-full mx-4 flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-[var(--border-default)]">
          <h2 className="font-display text-xl font-bold text-[var(--text-primary)]">
            Retoc Help
          </h2>
          <button
            onClick={onClose}
            className="text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition-colors"
          >
            <X size={24} />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4">
          {isLoading && (
            <div className="text-[var(--text-muted)]">Loading help...</div>
          )}

          {error && (
            <Alert variant="error" title="Error">
              {error}
            </Alert>
          )}

          {helpMarkdown && (
            <div className="bg-[var(--bg-inset)] border border-[var(--border-default)] rounded-md p-4">
              <pre className="font-mono text-xs text-[var(--text-primary)] whitespace-pre-wrap overflow-x-auto">
                {helpMarkdown.replace(/```/g, '')}
              </pre>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-[var(--border-default)] flex justify-end">
          <Button variant="primary" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </div>
  );
}
