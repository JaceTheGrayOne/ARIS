import type {
  DllInjectCommandDto,
  DllEjectCommandDto,
  DllInjectResponse,
  DllEjectResponse,
  DllInjectResultDto,
  DllEjectResultDto,
} from '../types/contracts';
import { OperationStatus } from '../types/contracts';

const generateOperationId = (): string => {
  return Array.from({ length: 32 }, () =>
    Math.floor(Math.random() * 16).toString(16)
  ).join('');
};

const simulateLatency = (min: number = 400, max: number = 900): Promise<void> => {
  const delay = Math.random() * (max - min) + min;
  return new Promise((resolve) => setTimeout(resolve, delay));
};

const shouldIncludeWarnings = (): boolean => {
  return Math.random() < 0.4;
};

const generateInjectWarnings = (): string[] => {
  const warnings = [
    'Target process had existing hooks; injection may be unstable.',
    'Module already loaded; skipping duplicate injection.',
    'Symbols not resolved for all modules.',
    'Process protection detected; elevated permissions required.',
  ];

  const count = Math.floor(Math.random() * 2) + 1;
  return warnings.slice(0, count);
};

const generateEjectWarnings = (): string[] => {
  const warnings = [
    'Module was not found in the main module list.',
    'Module unloaded, but handle validation is incomplete.',
    'Some threads may still reference the module memory.',
  ];

  const count = Math.random() < 0.5 ? 1 : 0;
  return warnings.slice(0, count);
};

const generateProcessName = (processName?: string): string => {
  if (processName) return processName;

  const defaultNames = [
    'Game-Win64-Shipping.exe',
    'UnrealEditor.exe',
    'UE4Editor.exe',
    'FortniteClient-Win64-Shipping.exe',
  ];

  return defaultNames[Math.floor(Math.random() * defaultNames.length)];
};

const generateProcessId = (processId?: number): number => {
  if (processId) return processId;
  return Math.floor(Math.random() * 16000) + 4000;
};

const formatDuration = (ms: number): string => {
  const seconds = Math.floor(ms / 1000);
  const milliseconds = ms % 1000;
  return `00:00:${seconds.toString().padStart(2, '0')}.${milliseconds.toString().padStart(3, '0')}`;
};

export async function runInject(
  command: DllInjectCommandDto
): Promise<DllInjectResponse> {
  const startTime = Date.now();
  await simulateLatency();
  const endTime = Date.now();
  const durationMs = endTime - startTime;

  const processId = generateProcessId(command.processId);
  const processName = generateProcessName(command.processName);
  const elevationUsed = command.requireElevation ?? (Math.random() < 0.3);

  const logExcerpt = `[INFO] Targeting process: ${processName} (PID: ${processId})
[INFO] DLL path: ${command.dllPath}
[INFO] Injection method: ${command.method}
[INFO] Elevation: ${elevationUsed ? 'Yes' : 'No'}
[INFO] Allocating memory in remote process...
[INFO] Writing DLL path to allocated memory...
[INFO] ${command.method === 'CreateRemoteThread' ? 'Creating remote thread...' : command.method === 'ManualMap' ? 'Manually mapping DLL sections...' : 'Queueing APC to target thread...'}
[SUCCESS] DLL injection completed successfully.`;

  const result: DllInjectResultDto = {
    processId,
    processName,
    dllPath: command.dllPath,
    method: command.method,
    elevationUsed,
    duration: formatDuration(durationMs),
    warnings: shouldIncludeWarnings() ? generateInjectWarnings() : [],
    logExcerpt,
  };

  return {
    operationId: generateOperationId(),
    status: OperationStatus.Succeeded,
    result,
    error: null,
    startedAt: new Date(startTime).toISOString(),
    completedAt: new Date(endTime).toISOString(),
  };
}

export async function runEject(
  command: DllEjectCommandDto
): Promise<DllEjectResponse> {
  const startTime = Date.now();
  await simulateLatency();
  const endTime = Date.now();
  const durationMs = endTime - startTime;

  const processId = generateProcessId(command.processId);
  const processName = generateProcessName(command.processName);
  const wasLoadedBefore = Math.random() < 0.85;
  const isUnloaded = wasLoadedBefore ? true : false;

  const logExcerpt = `[INFO] Targeting process: ${processName} (PID: ${processId})
[INFO] Module name: ${command.moduleName}
[INFO] Scanning loaded modules...
[INFO] ${wasLoadedBefore ? 'Module found in process memory.' : 'Module not found (may have been already unloaded).'}
${wasLoadedBefore ? '[INFO] Retrieving module handle...\n[INFO] Calling FreeLibrary...\n[SUCCESS] Module unloaded successfully.' : '[WARN] Module was not loaded; no action taken.'}`;

  const result: DllEjectResultDto = {
    processId,
    processName,
    moduleName: command.moduleName,
    wasLoadedBefore,
    isUnloaded,
    duration: formatDuration(durationMs),
    warnings: shouldIncludeWarnings() ? generateEjectWarnings() : [],
    logExcerpt,
  };

  return {
    operationId: generateOperationId(),
    status: OperationStatus.Succeeded,
    result,
    error: null,
    startedAt: new Date(startTime).toISOString(),
    completedAt: new Date(endTime).toISOString(),
  };
}
