import type {
  OperationStatus,
  RetocConvertResponse,
  UAssetSerializeResponse,
  UAssetDeserializeResponse,
  UAssetInspectResponse,
  UwpDumpResponse,
  DllInjectResponse,
  DllEjectResponse,
} from '../types/contracts';

export type ToolKind = 'Retoc' | 'UAsset' | 'UwpDumper' | 'DllInjector';

export type OperationKind =
  | 'RetocConvert'
  | 'UAssetSerialize'
  | 'UAssetDeserialize'
  | 'UAssetInspect'
  | 'UwpDump'
  | 'DllInject'
  | 'DllEject';

export interface OperationHistoryEntry {
  id: string;
  tool: ToolKind;
  kind: OperationKind;
  status: OperationStatus;
  startedAt: string;
  completedAt: string;
  label: string;
  summary: string;
  payload:
    | RetocConvertResponse
    | UAssetSerializeResponse
    | UAssetDeserializeResponse
    | UAssetInspectResponse
    | UwpDumpResponse
    | DllInjectResponse
    | DllEjectResponse;
}

const MAX_OPERATIONS = 100;

let operations: OperationHistoryEntry[] = [];
let listeners: Array<() => void> = [];

export function recordOperation(entry: OperationHistoryEntry): void {
  operations = [entry, ...operations].slice(0, MAX_OPERATIONS);
  listeners.forEach((listener) => listener());
}

export function getOperations(): OperationHistoryEntry[] {
  return operations;
}

export function subscribe(listener: () => void): () => void {
  listeners.push(listener);
  return () => {
    listeners = listeners.filter((l) => l !== listener);
  };
}
