import { useState, useEffect, useRef, useCallback } from 'react';
import { buildRetocCommand, streamRetocExecution, getRetocHelp } from '../../api/retocClient';
import { RetocCommandPreview } from '../../components/retoc/RetocCommandPreview';
import { RetocAdvancedCommandBuilder } from '../../components/retoc/RetocAdvancedCommandBuilder';
import { TerminalPanel, type TerminalPanelRef } from '../../components/terminal/TerminalPanel';
import type { RetocBuildCommandRequest, RetocStreamRequest, RetocStreamEvent } from '../../types/contracts';
import { Panel, PanelHeader, PanelBody, Field, Input, Select, Button, Alert, MarkdownFlyout } from '../../components/ui';
import { Package, HelpCircle, Terminal, XCircle, FileText } from 'lucide-react';

const UE_VERSIONS = [
  { value: 'UE5_6', label: 'UE 5.6' },
  { value: 'UE5_5', label: 'UE 5.5' },
  { value: 'UE5_4', label: 'UE 5.4' },
  { value: 'UE5_3', label: 'UE 5.3' },
  { value: 'UE5_2', label: 'UE 5.2' },
  { value: 'UE5_1', label: 'UE 5.1' },
  { value: 'UE5_0', label: 'UE 5.0' },
];

type Mode = 'simple' | 'advanced';

export function RetocPage() {
  // Mode state
  const [mode, setMode] = useState<Mode>('simple');

  // Simple mode state
  const [packInputDir, setPackInputDir] = useState('');
  const [packOutputDir, setPackOutputDir] = useState('');
  const [packModName, setPackModName] = useState('');
  const [packEngineVersion, setPackEngineVersion] = useState('UE5_6');
  const [packErrors, setPackErrors] = useState<Record<string, string>>({});

  const [unpackInputDir, setUnpackInputDir] = useState('');
  const [unpackOutputDir, setUnpackOutputDir] = useState('');
  const [unpackErrors, setUnpackErrors] = useState<Record<string, string>>({});

  // Advanced mode state
  const [advancedValues, setAdvancedValues] = useState<Record<string, any>>({});
  const [advancedErrors, setAdvancedErrors] = useState<Record<string, string>>({});

  // Shared state
  const [commandPreview, setCommandPreview] = useState<string | null>(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);

  const [isExecuting, setIsExecuting] = useState(false);
  const [executionError, setExecutionError] = useState<string | null>(null);

  const [isHelpModalOpen, setIsHelpModalOpen] = useState(false);
  const [isNamingModalOpen, setIsNamingModalOpen] = useState(false);

  // Terminal state
  const [showTerminal, setShowTerminal] = useState(true);
  const [lastExitCode, setLastExitCode] = useState<number | null>(null);
  const terminalRef = useRef<TerminalPanelRef>(null);
  const cancelRef = useRef<(() => void) | null>(null);

  // Auto-update preview when fields change in Simple mode
  useEffect(() => {
    if (mode === 'simple') {
      // No auto-preview in simple mode to keep it simple
    }
  }, [mode, packInputDir, packOutputDir, packModName, packEngineVersion, unpackInputDir, unpackOutputDir]);

  // Auto-update preview when fields change in Advanced mode
  useEffect(() => {
    const inputPath = advancedValues.InputPath ?? advancedValues.inputPath;
    const outputPath = advancedValues.OutputPath ?? advancedValues.outputPath;

    if (mode === 'advanced' && advancedValues.commandType && inputPath && outputPath) {
      updatePreview(advancedValues);
    } else if (mode === 'advanced') {
      setCommandPreview(null);
    }
  }, [mode, advancedValues]);

  const updatePreview = async (values: Record<string, any>) => {
    setIsPreviewLoading(true);
    setCommandPreview(null);

    try {
      const request: RetocBuildCommandRequest = {
        commandType: values.commandType || '',
        inputPath: values.InputPath || values.inputPath || '',
        outputPath: values.OutputPath || values.outputPath || '',
        engineVersion: values.EngineVersion || values.engineVersion || null,
        aesKey: values.AesKey || values.aesKey || null,
        containerHeaderVersion: values.ContainerHeaderVersion || values.containerHeaderVersion || null,
        tocVersion: values.TocVersion || values.tocVersion || null,
        chunkId: values.ChunkId || values.chunkId || null,
        verbose: values.Verbose || values.verbose || false,
        timeoutSeconds: values.TimeoutSeconds || values.timeoutSeconds || null,
      };

      const response = await buildRetocCommand(request);
      setCommandPreview(response.commandLine);
    } catch (err) {
      console.error('Failed to build command preview:', err);
    } finally {
      setIsPreviewLoading(false);
    }
  };

  const handlePackSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: Record<string, string> = {};

    if (!packInputDir.trim()) {
      newErrors.packInputDir = 'Input directory is required';
    }

    if (!packOutputDir.trim()) {
      newErrors.packOutputDir = 'Output directory is required';
    }

    if (!packModName.trim()) {
      newErrors.packModName = 'Mod name is required';
    }

    if (!packEngineVersion) {
      newErrors.packEngineVersion = 'Engine version is required';
    }

    setPackErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      return;
    }

    const outputUtocPath = `${packOutputDir.trim()}\\${packModName.trim()}.utoc`;

    updatePreview({
      commandType: 'ToZen',
      inputPath: packInputDir.trim(),
      outputPath: outputUtocPath,
      engineVersion: packEngineVersion,
    });
  };

  const handleUnpackSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const newErrors: Record<string, string> = {};

    if (!unpackInputDir.trim()) {
      newErrors.unpackInputDir = 'Input directory is required';
    }

    if (!unpackOutputDir.trim()) {
      newErrors.unpackOutputDir = 'Output directory is required';
    }

    setUnpackErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      return;
    }

    updatePreview({
      commandType: 'ToLegacy',
      inputPath: unpackInputDir.trim(),
      outputPath: unpackOutputDir.trim(),
    });
  };

  const handleAdvancedExecute = async () => {
    const newErrors: Record<string, string> = {};

    if (!advancedValues.commandType) {
      newErrors.commandType = 'Command type is required';
    }

    if (!advancedValues.InputPath && !advancedValues.inputPath) {
      newErrors.InputPath = 'Input path is required';
    }

    if (!advancedValues.OutputPath && !advancedValues.outputPath) {
      newErrors.OutputPath = 'Output path is required';
    }

    setAdvancedErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      return;
    }

    updatePreview(advancedValues);
  };

  const handleAdvancedFieldChange = (field: string, value: string | number | boolean) => {
    setAdvancedValues((prev) => ({ ...prev, [field]: value }));
    setAdvancedErrors((prev) => {
      const newErrors = { ...prev };
      delete newErrors[field];
      return newErrors;
    });
  };

  // Handle stream events from ConPTY
  const handleStreamEvent = useCallback((event: RetocStreamEvent) => {
    switch (event.type) {
      case 'started':
        terminalRef.current?.writeln(`\x1b[36m▶ Started: ${event.operationId}\x1b[0m`);
        terminalRef.current?.writeln(`\x1b[90m${event.commandLine}\x1b[0m`);
        terminalRef.current?.writeln('');
        break;

      case 'output':
        // Write raw terminal output (VT sequences)
        terminalRef.current?.write(event.data);
        break;

      case 'exited':
        terminalRef.current?.writeln('');
        if (event.exitCode === 0) {
          terminalRef.current?.writeln(`\x1b[32m✓ Completed successfully (${event.duration})\x1b[0m`);
        } else {
          terminalRef.current?.writeln(`\x1b[31m✗ Failed with exit code ${event.exitCode} (${event.duration})\x1b[0m`);
        }
        setLastExitCode(event.exitCode);
        setIsExecuting(false);
        break;

      case 'error':
        terminalRef.current?.writeln('');
        terminalRef.current?.writeln(`\x1b[31m✗ Error: ${event.code}\x1b[0m`);
        terminalRef.current?.writeln(`\x1b[31m${event.message}\x1b[0m`);
        if (event.remediationHint) {
          terminalRef.current?.writeln(`\x1b[33mHint: ${event.remediationHint}\x1b[0m`);
        }
        setExecutionError(event.message);
        setIsExecuting(false);
        break;
    }
  }, []);

  // Execute command with streaming
  const executeStream = useCallback(async (request: RetocStreamRequest) => {
    setIsExecuting(true);
    setExecutionError(null);
    setLastExitCode(null);
    setShowTerminal(true);

    // Clear terminal and write header
    terminalRef.current?.clear();
    terminalRef.current?.writeln('\x1b[1m═══════════════════════════════════════════\x1b[0m');
    terminalRef.current?.writeln('\x1b[1m  ARIS Retoc Terminal (ConPTY)\x1b[0m');
    terminalRef.current?.writeln('\x1b[1m═══════════════════════════════════════════\x1b[0m');
    terminalRef.current?.writeln('');

    try {
      const { cancel, promise } = streamRetocExecution(request, handleStreamEvent);
      cancelRef.current = cancel;
      await promise;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      setExecutionError(errorMessage);
      terminalRef.current?.writeln(`\x1b[31m✗ Connection error: ${errorMessage}\x1b[0m`);
    } finally {
      setIsExecuting(false);
      cancelRef.current = null;
    }
  }, [handleStreamEvent]);

  // Cancel current execution
  const handleCancel = useCallback(() => {
    if (cancelRef.current) {
      cancelRef.current();
      cancelRef.current = null;
      terminalRef.current?.writeln('\x1b[33m⚠ Execution cancelled by user\x1b[0m');
      setIsExecuting(false);
    }
  }, []);

  // Execute pack operation
  const handlePackExecute = async () => {
    const newErrors: Record<string, string> = {};

    if (!packInputDir.trim()) {
      newErrors.packInputDir = 'Input directory is required';
    }
    if (!packOutputDir.trim()) {
      newErrors.packOutputDir = 'Output directory is required';
    }
    if (!packModName.trim()) {
      newErrors.packModName = 'Mod name is required';
    }

    setPackErrors(newErrors);
    if (Object.keys(newErrors).length > 0) return;

    const outputUtocPath = `${packOutputDir.trim()}\\${packModName.trim()}.utoc`;

    const request: RetocStreamRequest = {
      commandType: 'ToZen',
      inputPath: packInputDir.trim(),
      outputPath: outputUtocPath,
      engineVersion: packEngineVersion,
    };

    await executeStream(request);
  };

  // Execute unpack operation
  const handleUnpackExecute = async () => {
    const newErrors: Record<string, string> = {};

    if (!unpackInputDir.trim()) {
      newErrors.unpackInputDir = 'Input directory is required';
    }
    if (!unpackOutputDir.trim()) {
      newErrors.unpackOutputDir = 'Output directory is required';
    }

    setUnpackErrors(newErrors);
    if (Object.keys(newErrors).length > 0) return;

    const request: RetocStreamRequest = {
      commandType: 'ToLegacy',
      inputPath: unpackInputDir.trim(),
      outputPath: unpackOutputDir.trim(),
    };

    await executeStream(request);
  };

  // Execute advanced mode command
  const handleAdvancedStreamExecute = async () => {
    const newErrors: Record<string, string> = {};

    if (!advancedValues.commandType) {
      newErrors.commandType = 'Command type is required';
    }

    const inputPath = advancedValues.InputPath ?? advancedValues.inputPath;
    const outputPath = advancedValues.OutputPath ?? advancedValues.outputPath;

    if (!inputPath) {
      newErrors.InputPath = 'Input path is required';
    }
    if (!outputPath) {
      newErrors.OutputPath = 'Output path is required';
    }

    setAdvancedErrors(newErrors);
    if (Object.keys(newErrors).length > 0) return;

    // Helper to convert empty strings to null
    const emptyToNull = (val: string | null | undefined): string | null =>
      val && val.trim() !== '' ? val.trim() : null;

    // Helper to parse optional number
    const parseOptionalNumber = (val: string | number | null | undefined): number | null => {
      if (val === null || val === undefined || val === '') return null;
      const num = typeof val === 'number' ? val : parseInt(val, 10);
      return isNaN(num) ? null : num;
    };

    // Helper to parse boolean (handles string "true"/"false" and actual booleans)
    const parseBoolean = (val: string | boolean | null | undefined): boolean => {
      if (typeof val === 'boolean') return val;
      if (val === 'true') return true;
      return false;
    };

    const request: RetocStreamRequest = {
      commandType: advancedValues.commandType,
      inputPath: inputPath,
      outputPath: outputPath,
      engineVersion: emptyToNull(advancedValues.EngineVersion ?? advancedValues.engineVersion),
      aesKey: emptyToNull(advancedValues.AesKey ?? advancedValues.aesKey),
      containerHeaderVersion: emptyToNull(advancedValues.ContainerHeaderVersion ?? advancedValues.containerHeaderVersion),
      tocVersion: emptyToNull(advancedValues.TocVersion ?? advancedValues.tocVersion),
      chunkId: emptyToNull(advancedValues.ChunkId ?? advancedValues.chunkId),
      verbose: parseBoolean(advancedValues.Verbose ?? advancedValues.verbose),
      timeoutSeconds: parseOptionalNumber(advancedValues.TimeoutSeconds ?? advancedValues.timeoutSeconds),
    };

    await executeStream(request);
  };

  return (
    <div className="space-y-6 module-retoc">
      {/* Page Header */}
      <div className="border-b-2 border-[var(--module-accent)] pb-4">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-3">
            <Package size={32} className="text-[var(--module-accent)]" strokeWidth={2} />
            <h1 className="font-display text-3xl font-bold text-[var(--text-primary)]">
              IoStore / Retoc
            </h1>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setIsNamingModalOpen(true)}
            >
              <FileText size={16} className="mr-2" />
              UE Naming
            </Button>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setIsHelpModalOpen(true)}
            >
              <HelpCircle size={16} className="mr-2" />
              Help
            </Button>
          </div>
        </div>
        <p className="text-[var(--text-secondary)] ml-11">
          Pack and unpack Unreal Engine IoStore containers
        </p>
      </div>

      {/* Mode Toggle */}
      <div className="flex gap-2">
        <button
          onClick={() => setMode('simple')}
          className={`px-4 py-2 rounded-md font-semibold transition-all ${
            mode === 'simple'
              ? 'bg-[var(--module-accent)] text-white'
              : 'bg-[var(--bg-panel-2)] text-[var(--text-secondary)] hover:bg-[var(--bg-inset)]'
          }`}
        >
          Simple Mode
        </button>
        <button
          onClick={() => setMode('advanced')}
          className={`px-4 py-2 rounded-md font-semibold transition-all ${
            mode === 'advanced'
              ? 'bg-[var(--module-accent)] text-white'
              : 'bg-[var(--bg-panel-2)] text-[var(--text-secondary)] hover:bg-[var(--bg-inset)]'
          }`}
        >
          Advanced Mode
        </button>
      </div>

      {executionError && (
        <Alert variant="error" title="Execution Error">
          {executionError}
        </Alert>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column: Input Forms */}
        <div className="space-y-6">
          {mode === 'simple' ? (
            <>
              {/* Pack Section */}
              <Panel>
                <PanelHeader
                  title="Pack (Legacy → Zen)"
                  subtitle="Build a mod from modified UAsset files"
                  accent
                />
                <PanelBody>
                  <form onSubmit={handlePackSubmit} className="space-y-4">
                    <Field
                      label="Modified UAsset Directory"
                      required
                      error={packErrors.packInputDir}
                    >
                      <Input
                        type="text"
                        value={packInputDir}
                        onChange={(e) => setPackInputDir(e.target.value)}
                        disabled={isExecuting}
                        placeholder="G:\\Grounded\\Modding\\ModFolder"
                        error={!!packErrors.packInputDir}
                      />
                    </Field>

                    <Field
                      label="Mod Output Directory"
                      required
                      error={packErrors.packOutputDir}
                    >
                      <Input
                        type="text"
                        value={packOutputDir}
                        onChange={(e) => setPackOutputDir(e.target.value)}
                        disabled={isExecuting}
                        placeholder="G:\\Grounded\\Modding\\AwesomeMod"
                        error={!!packErrors.packOutputDir}
                      />
                    </Field>

                    <Field
                      label="Mod Name"
                      required
                      error={packErrors.packModName}
                      help={`Output: ${packOutputDir || '<output-dir>'}\\${packModName || '<mod-name>'}.utoc`}
                    >
                      <Input
                        type="text"
                        value={packModName}
                        onChange={(e) => setPackModName(e.target.value)}
                        disabled={isExecuting}
                        placeholder="AwesomeMod"
                        error={!!packErrors.packModName}
                      />
                    </Field>

                    <Field
                      label="UE Version"
                      required
                      error={packErrors.packEngineVersion}
                    >
                      <Select
                        value={packEngineVersion}
                        onChange={(e) => setPackEngineVersion(e.target.value)}
                        disabled={isExecuting}
                        error={!!packErrors.packEngineVersion}
                      >
                        {UE_VERSIONS.map((v) => (
                          <option key={v.value} value={v.value}>
                            {v.label}
                          </option>
                        ))}
                      </Select>
                    </Field>

                    <div className="flex gap-2">
                      <Button
                        type="submit"
                        variant="accent"
                        size="lg"
                        className="flex-1"
                        disabled={isExecuting}
                      >
                        Preview Command
                      </Button>
                      <Button
                        type="button"
                        variant="accent"
                        size="lg"
                        className="flex-1"
                        onClick={handlePackExecute}
                        disabled={isExecuting}
                      >
                        <Terminal size={16} className="mr-2" />
                        {isExecuting ? 'Running...' : 'Build Mod'}
                      </Button>
                    </div>
                  </form>
                </PanelBody>
              </Panel>

              {/* Unpack Section */}
              <Panel>
                <PanelHeader
                  title="Unpack (Zen → Legacy)"
                  subtitle="Extract IoStore containers to editable UAsset files"
                  accent
                />
                <PanelBody>
                  <form onSubmit={handleUnpackSubmit} className="space-y-4">
                    <Field
                      label="Base Game Paks Directory"
                      required
                      error={unpackErrors.unpackInputDir}
                    >
                      <Input
                        type="text"
                        value={unpackInputDir}
                        onChange={(e) => setUnpackInputDir(e.target.value)}
                        disabled={isExecuting}
                        placeholder="E:\\SteamLibrary\\steamapps\\common\\Grounded2\\Augusta\\Content\\Paks"
                        error={!!unpackErrors.unpackInputDir}
                      />
                    </Field>

                    <Field
                      label="Extracted Output Directory"
                      required
                      error={unpackErrors.unpackOutputDir}
                    >
                      <Input
                        type="text"
                        value={unpackOutputDir}
                        onChange={(e) => setUnpackOutputDir(e.target.value)}
                        disabled={isExecuting}
                        placeholder="G:\\Grounded\\Modding\\Grounded 2_Extracted"
                        error={!!unpackErrors.unpackOutputDir}
                      />
                    </Field>

                    <div className="flex gap-2">
                      <Button
                        type="submit"
                        variant="accent"
                        size="lg"
                        className="flex-1"
                        disabled={isExecuting}
                      >
                        Preview Command
                      </Button>
                      <Button
                        type="button"
                        variant="accent"
                        size="lg"
                        className="flex-1"
                        onClick={handleUnpackExecute}
                        disabled={isExecuting}
                      >
                        <Terminal size={16} className="mr-2" />
                        {isExecuting ? 'Running...' : 'Extract Files'}
                      </Button>
                    </div>
                  </form>
                </PanelBody>
              </Panel>
            </>
          ) : (
            <>
              {/* Advanced Mode Builder */}
              <RetocAdvancedCommandBuilder
                onFieldChange={handleAdvancedFieldChange}
                values={advancedValues}
                errors={advancedErrors}
              />

              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  size="lg"
                  className="flex-1"
                  onClick={handleAdvancedExecute}
                  disabled={isExecuting}
                >
                  Preview Command
                </Button>
                <Button
                  variant="accent"
                  size="lg"
                  className="flex-1"
                  onClick={handleAdvancedStreamExecute}
                  disabled={isExecuting}
                >
                  <Terminal size={16} className="mr-2" />
                  {isExecuting ? 'Running...' : 'Run in Terminal'}
                </Button>
              </div>
            </>
          )}
        </div>

        {/* Right Column: Preview and Terminal */}
        <div className="space-y-4">
          <RetocCommandPreview commandLine={commandPreview} isLoading={isPreviewLoading} />

          {/* Terminal Panel */}
          {showTerminal && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Terminal size={16} className="text-[var(--module-accent)]" />
                  <span className="text-sm font-medium text-[var(--text-primary)]">
                    Terminal Output
                  </span>
                  {lastExitCode !== null && (
                    <span
                      className={`text-xs px-2 py-0.5 rounded ${
                        lastExitCode === 0
                          ? 'bg-green-500/20 text-green-400'
                          : 'bg-red-500/20 text-red-400'
                      }`}
                    >
                      Exit: {lastExitCode}
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {isExecuting && (
                    <Button
                      variant="danger"
                      size="sm"
                      onClick={handleCancel}
                    >
                      <XCircle size={14} className="mr-1" />
                      Cancel
                    </Button>
                  )}
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => terminalRef.current?.clear()}
                    disabled={isExecuting}
                  >
                    Clear
                  </Button>
                </div>
              </div>
              <TerminalPanel
                ref={terminalRef}
                title="Retoc Execution"
                height="350px"
              />
            </div>
          )}
        </div>
      </div>

      {/* Help Flyout */}
      <MarkdownFlyout
        isOpen={isHelpModalOpen}
        onClose={() => setIsHelpModalOpen(false)}
        title="Retoc Help"
        loadContent={async () => {
          const response = await getRetocHelp();
          // Strip code fence markers if present
          return response.markdown.replace(/^```\n?/, '').replace(/\n?```$/, '');
        }}
      />

      {/* UE Naming Flyout */}
      <MarkdownFlyout
        isOpen={isNamingModalOpen}
        onClose={() => setIsNamingModalOpen(false)}
        title="Unreal Mod Naming Convention"
        loadContent={async () => {
          const response = await fetch('/UE_Mod_Naming.md');
          if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
          }
          return response.text();
        }}
      />
    </div>
  );
}
