import { useEffect, useRef, useImperativeHandle, forwardRef } from 'react';
import { Terminal } from '@xterm/xterm';
import { FitAddon } from '@xterm/addon-fit';
import '@xterm/xterm/css/xterm.css';

export interface TerminalPanelProps {
  title?: string;
  className?: string;
  height?: string;
  onReady?: (terminal: Terminal) => void;
}

export interface TerminalPanelRef {
  terminal: Terminal | null;
  write: (data: string) => void;
  writeln: (data: string) => void;
  clear: () => void;
  fit: () => void;
  focus: () => void;
  scrollToBottom: () => void;
}

const TERMINAL_THEME = {
  background: '#0a0a0a',
  foreground: '#e4e4e4',
  cursor: '#f59e0b',
  cursorAccent: '#0a0a0a',
  selectionBackground: 'rgba(59, 130, 246, 0.4)',
  black: '#0a0a0a',
  red: '#ef4444',
  green: '#22c55e',
  yellow: '#f59e0b',
  blue: '#3b82f6',
  magenta: '#a855f7',
  cyan: '#06b6d4',
  white: '#e4e4e4',
  brightBlack: '#525252',
  brightRed: '#f87171',
  brightGreen: '#4ade80',
  brightYellow: '#fbbf24',
  brightBlue: '#60a5fa',
  brightMagenta: '#c084fc',
  brightCyan: '#22d3ee',
  brightWhite: '#fafafa',
};

export const TerminalPanel = forwardRef<TerminalPanelRef, TerminalPanelProps>(
  ({ title = 'Terminal', className = '', height = '300px', onReady }, ref) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const terminalRef = useRef<Terminal | null>(null);
    const fitAddonRef = useRef<FitAddon | null>(null);

    // Expose methods via ref
    useImperativeHandle(ref, () => ({
      terminal: terminalRef.current,
      write: (data: string) => terminalRef.current?.write(data),
      writeln: (data: string) => terminalRef.current?.writeln(data),
      clear: () => terminalRef.current?.clear(),
      fit: () => fitAddonRef.current?.fit(),
      focus: () => terminalRef.current?.focus(),
      scrollToBottom: () => terminalRef.current?.scrollToBottom(),
    }));

    // Initialize terminal
    useEffect(() => {
      if (!containerRef.current || terminalRef.current) {
        return;
      }

      const terminal = new Terminal({
        fontSize: 13,
        fontFamily: 'Consolas, "Courier New", monospace',
        theme: TERMINAL_THEME,
        cursorBlink: true,
        cursorStyle: 'block',
        scrollback: 10000,
        convertEol: true,
        allowProposedApi: true,
      });

      const fitAddon = new FitAddon();
      terminal.loadAddon(fitAddon);

      terminal.open(containerRef.current);

      // Delay fit to ensure container is rendered
      requestAnimationFrame(() => {
        fitAddon.fit();
      });

      terminalRef.current = terminal;
      fitAddonRef.current = fitAddon;

      if (onReady) {
        onReady(terminal);
      }

      // Handle resize with debounce to prevent feedback loops
      let resizeTimeout: ReturnType<typeof setTimeout> | null = null;
      const resizeObserver = new ResizeObserver(() => {
        if (resizeTimeout) {
          clearTimeout(resizeTimeout);
        }
        resizeTimeout = setTimeout(() => {
          requestAnimationFrame(() => {
            fitAddon.fit();
          });
        }, 100);
      });
      resizeObserver.observe(containerRef.current);

      return () => {
        if (resizeTimeout) {
          clearTimeout(resizeTimeout);
        }
        resizeObserver.disconnect();
        terminal.dispose();
        terminalRef.current = null;
        fitAddonRef.current = null;
      };
    }, [onReady]);

    return (
      <div
        className={`terminal-panel ${className}`}
        style={{
          display: 'flex',
          flexDirection: 'column',
          backgroundColor: 'var(--bg-panel-2, #111)',
          borderRadius: '8px',
          overflow: 'hidden',
          border: '1px solid var(--border-subtle, #333)',
        }}
      >
        {/* Header */}
        <div
          style={{
            padding: '8px 12px',
            borderBottom: '1px solid var(--border-subtle, #333)',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            backgroundColor: 'var(--bg-inset, #0a0a0a)',
          }}
        >
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            style={{ color: 'var(--module-accent, #f59e0b)' }}
          >
            <polyline points="4 17 10 11 4 5" />
            <line x1="12" y1="19" x2="20" y2="19" />
          </svg>
          <span
            style={{
              fontSize: '13px',
              fontWeight: 500,
              color: 'var(--text-primary, #e4e4e4)',
            }}
          >
            {title}
          </span>
        </div>

        {/* Terminal container */}
        <div
          ref={containerRef}
          style={{
            height: height,
            maxHeight: height,
            padding: '8px',
            backgroundColor: '#0a0a0a',
            overflow: 'hidden',
          }}
        />
      </div>
    );
  }
);

TerminalPanel.displayName = 'TerminalPanel';

export default TerminalPanel;
