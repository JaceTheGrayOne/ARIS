import type {
  UwpDumpCommandDto,
  UwpDumpResponse,
  UwpDumpResultDto,
  ProducedFileDto,
} from '../types/contracts';
import { OperationStatus } from '../types/contracts';

const generateOperationId = (): string => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
};

const simulateLatency = (min: number = 500, max: number = 800): Promise<void> => {
  const delay = Math.random() * (max - min) + min;
  return new Promise((resolve) => setTimeout(resolve, delay));
};

const shouldIncludeWarnings = (): boolean => {
  return Math.random() < 0.3;
};

const generateWarnings = (): string[] => {
  const warnings = [
    'Some executable files are still running',
    'Package manifest missing optional fields',
    'Debug symbols not found for some binaries',
    'Partial file permissions encountered',
  ];

  const count = Math.floor(Math.random() * 2) + 1;
  return warnings.slice(0, count);
};

export async function runDump(
  command: UwpDumpCommandDto
): Promise<UwpDumpResponse> {
  await simulateLatency();

  const filesExtracted = Math.floor(Math.random() * 100) + 20;
  const producedFiles: ProducedFileDto[] = [];

  // Generate some sample produced files based on mode
  if (command.mode === 'FullDump') {
    producedFiles.push({
      path: `${command.outputPath}/AppxManifest.xml`,
      sizeBytes: Math.floor(Math.random() * 10000) + 2000,
      sha256: null,
      fileType: '.xml',
    });
    producedFiles.push({
      path: `${command.outputPath}/Assets/`,
      sizeBytes: Math.floor(Math.random() * 50000000) + 10000000,
      sha256: null,
      fileType: 'directory',
    });
    producedFiles.push({
      path: `${command.outputPath}/${command.packageFamilyName}.exe`,
      sizeBytes: Math.floor(Math.random() * 20000000) + 5000000,
      sha256: null,
      fileType: '.exe',
    });

    if (command.includeSymbols) {
      producedFiles.push({
        path: `${command.outputPath}/${command.packageFamilyName}.pdb`,
        sizeBytes: Math.floor(Math.random() * 30000000) + 10000000,
        sha256: null,
        fileType: '.pdb',
      });
    }
  } else if (command.mode === 'MetadataOnly') {
    producedFiles.push({
      path: `${command.outputPath}/metadata.json`,
      sizeBytes: Math.floor(Math.random() * 50000) + 5000,
      sha256: null,
      fileType: '.json',
    });
    producedFiles.push({
      path: `${command.outputPath}/AppxManifest.xml`,
      sizeBytes: Math.floor(Math.random() * 10000) + 2000,
      sha256: null,
      fileType: '.xml',
    });
  }

  const totalSizeBytes = producedFiles.reduce((sum, file) => sum + file.sizeBytes, 0);

  const result: UwpDumpResultDto = {
    packageFamilyName: command.packageFamilyName,
    applicationId: command.applicationId || null,
    mode: command.mode,
    outputPath: command.outputPath,
    duration: '00:00:0' + (Math.floor(Math.random() * 8) + 2) + '.000',
    filesExtracted: command.mode === 'ValidateOnly' ? 0 : filesExtracted,
    totalSizeBytes: command.mode === 'ValidateOnly' ? 0 : totalSizeBytes,
    warnings: shouldIncludeWarnings() ? generateWarnings() : [],
    producedFiles: command.mode === 'ValidateOnly' ? [] : producedFiles,
    metadata: {
      packageFullName: `${command.packageFamilyName}_1.0.0.0_x64__8wekyb3d8bbwe`,
      version: '1.0.0.0',
      architecture: 'x64',
      publisher: 'CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US',
    },
  };

  return {
    operationId: generateOperationId(),
    status: OperationStatus.Succeeded,
    result,
    error: null,
    startedAt: new Date().toISOString(),
    completedAt: new Date(Date.now() + 2000).toISOString(),
  };
}
