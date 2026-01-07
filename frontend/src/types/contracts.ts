export const OperationStatus = {
  Pending: 0,
  Succeeded: 1,
  Failed: 2,
} as const;

export type OperationStatus = typeof OperationStatus[keyof typeof OperationStatus];

export interface HealthResponse {
  status: string;
  dependenciesReady: boolean;
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

// Retoc Streaming and Advanced Mode Contracts

export interface RetocBuildCommandRequest {
  commandType: string;
  inputPath: string;
  outputPath: string;
  engineVersion?: string | null;
  aesKey?: string | null;
  containerHeaderVersion?: string | null;
  tocVersion?: string | null;
  chunkId?: string | null;
  verbose?: boolean;
  timeoutSeconds?: number | null;
}

export interface RetocBuildCommandResponse {
  executablePath: string;
  arguments: string[];
  commandLine: string;
}

export interface RetocCommandFieldDefinition {
  fieldName: string;
  label: string;
  fieldType: string;
  helpText?: string | null;
  enumValues?: string[] | null;
  minValue?: number | null;
  maxValue?: number | null;
}

export interface RetocFieldUiHint {
  pathKind?: 'file' | 'folder' | null;
  extensions?: string[] | null;
}

export interface RetocCommandDefinition {
  commandType: string;
  displayName: string;
  description: string;
  requiredFields: string[];
  optionalFields: string[];
  fieldUiHints?: Record<string, RetocFieldUiHint> | null;
}

export interface RetocCommandSchemaResponse {
  commands: RetocCommandDefinition[];
  globalOptions: RetocCommandFieldDefinition[];
  allowlistedFlags: string[];
}

export interface RetocHelpResponse {
  markdown: string;
}

// Tool Schema Types (canonical format from /api/tools/{tool}/schema)

export interface ToolSchemaResponse {
  tool: string;
  version?: string | null;
  generatedAtUtc: string;
  commands: ToolCommandSchema[];
  globalOptions?: ToolOptionSchema[] | null;
}

export interface ToolCommandSchema {
  name: string;
  summary?: string | null;
  usages: string[];
  positionals: ToolPositionalSchema[];
  options?: ToolOptionSchema[] | null;
}

export interface ToolPositionalSchema {
  name: string;
  index: number;
  required: boolean;
  typeHint?: string | null;
  description?: string | null;
}

export interface ToolOptionSchema {
  name: string;
  shortName?: string | null;
  description?: string | null;
  takesValue?: boolean;
  valueHint?: string | null;
}

// Retoc Streaming Contracts (WebSocket / ConPTY)

export interface RetocStreamRequest {
  commandType: string;
  inputPath: string;
  outputPath: string;
  engineVersion?: string | null;
  aesKey?: string | null;
  containerHeaderVersion?: string | null;
  tocVersion?: string | null;
  chunkId?: string | null;
  verbose?: boolean;
  timeoutSeconds?: number | null;
  ttyProbe?: boolean;
}

export type RetocStreamEventType = 'started' | 'output' | 'exited' | 'error';

export interface RetocStreamEventBase {
  type: RetocStreamEventType;
  timestamp: string;
}

export interface RetocStreamStarted extends RetocStreamEventBase {
  type: 'started';
  operationId: string;
  commandLine: string;
}

export interface RetocStreamOutput extends RetocStreamEventBase {
  type: 'output';
  data: string;
}

export interface RetocStreamExited extends RetocStreamEventBase {
  type: 'exited';
  exitCode: number;
  duration: string;
}

export interface RetocStreamError extends RetocStreamEventBase {
  type: 'error';
  code: string;
  message: string;
  remediationHint?: string | null;
}

export type RetocStreamEvent =
  | RetocStreamStarted
  | RetocStreamOutput
  | RetocStreamExited
  | RetocStreamError;
