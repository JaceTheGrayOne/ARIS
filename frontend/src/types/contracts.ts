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
  engineVersion?: string | null;
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

export type UAssetOperation = 'Serialize' | 'Deserialize' | 'Inspect';

export interface UAssetSerializeRequest {
  inputJsonPath: string;
  outputAssetPath: string;
  game?: string | null;
  ueVersion?: string | null;
  schemaVersion?: string | null;
}

export interface UAssetDeserializeRequest {
  inputAssetPath: string;
  outputJsonPath: string;
  game?: string | null;
  ueVersion?: string | null;
  schemaVersion?: string | null;
  includeBulkData?: boolean;
}

export interface UAssetInspectRequest {
  inputAssetPath: string;
  fields?: string[] | null;
}

export interface UAssetResultDto {
  operation: UAssetOperation;
  inputPath: string;
  outputPath: string;
  duration: string;
  warnings: string[];
  producedFiles: ProducedFileDto[];
  schemaVersion?: string | null;
  ueVersion?: string | null;
  logExcerpt?: string | null;
}

export interface UAssetInspectionDto {
  inputPath: string;
  summary: {
    ueVersion?: string | null;
    licenseeVersion: number;
    customVersionCount: number;
    nameCount: number;
    exportCount: number;
    importCount: number;
  };
  exports?: string[] | null;
  imports?: string[] | null;
  names?: string[] | null;
}

export interface UAssetSerializeResponse {
  operationId: string;
  status: OperationStatus;
  result?: UAssetResultDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}

export interface UAssetDeserializeResponse {
  operationId: string;
  status: OperationStatus;
  result?: UAssetResultDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}

export interface UAssetInspectResponse {
  operationId: string;
  status: OperationStatus;
  result?: UAssetInspectionDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}

export type UwpDumpMode = 'FullDump' | 'MetadataOnly' | 'ValidateOnly';

export interface UwpDumpCommandDto {
  packageFamilyName: string;
  applicationId?: string | null;
  outputPath: string;
  mode: UwpDumpMode;
  includeSymbols?: boolean;
}

export interface UwpDumpResultDto {
  packageFamilyName: string;
  applicationId?: string | null;
  mode: UwpDumpMode;
  outputPath: string;
  duration: string;
  filesExtracted: number;
  totalSizeBytes: number;
  warnings: string[];
  producedFiles: ProducedFileDto[];
  metadata?: {
    packageFullName?: string | null;
    version?: string | null;
    architecture?: string | null;
    publisher?: string | null;
  } | null;
}

export interface UwpDumpResponse {
  operationId: string;
  status: OperationStatus;
  result?: UwpDumpResultDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}

export type DllInjectionMethod = 'CreateRemoteThread' | 'ApcQueue' | 'ManualMap';

export interface DllInjectCommandDto {
  processId?: number;
  processName?: string;
  dllPath: string;
  method: DllInjectionMethod;
  requireElevation?: boolean;
  arguments?: string[];
}

export interface DllEjectCommandDto {
  processId?: number;
  processName?: string;
  moduleName: string;
  requireElevation?: boolean;
}

export interface DllInjectResultDto {
  processId: number;
  processName: string;
  dllPath: string;
  method: DllInjectionMethod;
  elevationUsed: boolean;
  duration: string;
  warnings: string[];
  logExcerpt?: string | null;
}

export interface DllEjectResultDto {
  processId: number;
  processName: string;
  moduleName: string;
  wasLoadedBefore: boolean;
  isUnloaded: boolean;
  duration: string;
  warnings: string[];
  logExcerpt?: string | null;
}

export interface DllInjectResponse {
  operationId: string;
  status: OperationStatus;
  result?: DllInjectResultDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}

export interface DllEjectResponse {
  operationId: string;
  status: OperationStatus;
  result?: DllEjectResultDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}
