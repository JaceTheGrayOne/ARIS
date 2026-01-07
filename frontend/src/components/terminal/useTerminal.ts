import { useRef, useCallback, useEffect, useState } from 'react';
import { Terminal } from '@xterm/xterm';
import { FitAddon } from '@xterm/addon-fit';
import '@xterm/xterm/css/xterm.css';

export interface UseTerminalOptions {
  fontSize?: number;
  fontFamily?: string;
  theme?: {
    background?: string;
    foreground?: string;
    cursor?: string;
    cursorAccent?: string;
    selectionBackground?: string;
  };
}

export interface UseTerminalReturn {
  terminalRef: React.RefObject<HTMLDivElement | null>;
  terminal: Terminal | null;
  isReady: boolean;
  write: (data: string) => void;
  writeln: (data: string) => void;
  clear: () => void;
  fit: () => void;
  focus: () => void;
  scrollToBottom: () => void;
}

const DEFAULT_THEME = {
  background: '#0a0a0a',
  foreground: '#e4e4e4',
  cursor: '#f59e0b',
  cursorAccent: '#0a0a0a',
  selectionBackground: '#3b82f6',
};

export function useTerminal(options: UseTerminalOptions = {}): UseTerminalReturn {
  const terminalRef = useRef<HTMLDivElement | null>(null);
  const terminalInstanceRef = useRef<Terminal | null>(null);
  const fitAddonRef = useRef<FitAddon | null>(null);
  const [isReady, setIsReady] = useState(false);

  const {
    fontSize = 14,
    fontFamily = 'Consolas, "Courier New", monospace',
    theme = DEFAULT_THEME,
  } = options;

  // Initialize terminal
  useEffect(() => {
    if (!terminalRef.current || terminalInstanceRef.current) {
      return;
    }

    const terminal = new Terminal({
      fontSize,
      fontFamily,
      theme: { ...DEFAULT_THEME, ...theme },
      cursorBlink: true,
      cursorStyle: 'block',
      scrollback: 10000,
      convertEol: true,
      allowProposedApi: true,
    });

    const fitAddon = new FitAddon();
    terminal.loadAddon(fitAddon);

    terminal.open(terminalRef.current);
    fitAddon.fit();

    terminalInstanceRef.current = terminal;
    fitAddonRef.current = fitAddon;
    setIsReady(true);

    // Handle window resize
    const handleResize = () => {
      fitAddon.fit();
    };
    window.addEventListener('resize', handleResize);

    // Use ResizeObserver for container size changes
    const resizeObserver = new ResizeObserver(() => {
      fitAddon.fit();
    });
    resizeObserver.observe(terminalRef.current);

    return () => {
      window.removeEventListener('resize', handleResize);
      resizeObserver.disconnect();
      terminal.dispose();
      terminalInstanceRef.current = null;
      fitAddonRef.current = null;
      setIsReady(false);
    };
  }, [fontSize, fontFamily, theme]);

  const write = useCallback((data: string) => {
    terminalInstanceRef.current?.write(data);
  }, []);

  const writeln = useCallback((data: string) => {
    terminalInstanceRef.current?.writeln(data);
  }, []);

  const clear = useCallback(() => {
    terminalInstanceRef.current?.clear();
  }, []);

  const fit = useCallback(() => {
    fitAddonRef.current?.fit();
  }, []);

  const focus = useCallback(() => {
    terminalInstanceRef.current?.focus();
  }, []);

  const scrollToBottom = useCallback(() => {
    terminalInstanceRef.current?.scrollToBottom();
  }, []);

  return {
    terminalRef,
    terminal: terminalInstanceRef.current,
    isReady,
    write,
    writeln,
    clear,
    fit,
    focus,
    scrollToBottom,
  };
}
