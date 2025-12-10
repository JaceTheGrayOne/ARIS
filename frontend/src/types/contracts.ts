export const OperationStatus = {
  Pending: 0,
  Succeeded: 1,
  Failed: 2,
} as const;

export type OperationStatus = typeof OperationStatus[keyof typeof OperationStatus];

export interface HealthResponse {
  status: string;
  dependenciesReady: boolean;
  currentWorkspace?: string | null;
  message?: string | null;
}

export interface InfoResponse {
  version: string;
  backendBaseUrl: string;
  ipcToken?: string | null;
  toolVersions: Record<string, string>;
}

export interface ErrorInfo {
  code: string;
  message: string;
  remediationHint?: string | null;
}

export interface ProducedFileDto {
  path: string;
  sizeBytes: number;
  sha256?: string | null;
  fileType?: string | null;
}

export interface RetocResultDto {
  exitCode: number;
  outputPath: string;
  outputFormat?: string | null;
  duration: string;
  warnings: string[];
  producedFiles: ProducedFileDto[];
  schemaVersion?: string | null;
  ueVersion?: string | null;
  logExcerpt?: string | null;
}

export interface RetocConvertRequest {
  inputPath: string;
  outputPath: string;
  mode: string;
  game?: string | null;
  ueVersion?: string | null;
  compressionFormat?: string | null;
  compressionLevel?: number | null;
  timeoutSeconds?: number | null;
  mountKeys?: Record<string, string> | null;
  includeFilters?: string[] | null;
  excludeFilters?: string[] | null;
}

export interface RetocConvertResponse {
  operationId: string;
  status: OperationStatus;
  result?: RetocResultDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}
