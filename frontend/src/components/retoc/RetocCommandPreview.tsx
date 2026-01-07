import { useState } from 'react';
import { Panel, PanelHeader, PanelBody, Button } from '../ui';
import { Copy, Check } from 'lucide-react';

interface RetocCommandPreviewProps {
  commandLine: string | null;
  isLoading?: boolean;
}

export function RetocCommandPreview({ commandLine, isLoading }: RetocCommandPreviewProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    if (!commandLine) return;

    try {
      await navigator.clipboard.writeText(commandLine);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy command:', err);
    }
  };

  if (!commandLine && !isLoading) {
    return null;
  }

  return (
    <Panel>
      <PanelHeader title="Command Preview" />
      <PanelBody>
        {isLoading ? (
          <div className="text-[var(--text-muted)] text-sm">
            Building command...
          </div>
        ) : (
          <div className="space-y-3">
            <div className="bg-[var(--bg-inset)] border border-[var(--border-default)] rounded-md p-3 font-mono text-xs overflow-x-auto">
              <code className="text-[var(--text-primary)] whitespace-pre-wrap break-all">
                {commandLine}
              </code>
            </div>
            <Button
              variant="secondary"
              size="sm"
              onClick={handleCopy}
              disabled={!commandLine}
            >
              {copied ? (
                <>
                  <Check size={14} className="mr-2" />
                  Copied!
                </>
              ) : (
                <>
                  <Copy size={14} className="mr-2" />
                  Copy Command
                </>
              )}
            </Button>
          </div>
        )}
      </PanelBody>
    </Panel>
  );
}
