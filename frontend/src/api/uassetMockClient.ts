import type {
  UAssetSerializeRequest,
  UAssetDeserializeRequest,
  UAssetInspectRequest,
  UAssetSerializeResponse,
  UAssetDeserializeResponse,
  UAssetInspectResponse,
  UAssetResultDto,
  UAssetInspectionDto,
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
    'Asset contains deprecated property types',
    'Schema version mismatch detected',
    'Missing bulk data reference',
    'Unknown custom version encountered',
  ];

  const count = Math.floor(Math.random() * 2) + 1;
  return warnings.slice(0, count);
};

export async function runSerialize(
  request: UAssetSerializeRequest
): Promise<UAssetSerializeResponse> {
  await simulateLatency();

  const producedFiles: ProducedFileDto[] = [
    {
      path: request.outputAssetPath,
      sizeBytes: Math.floor(Math.random() * 5000000) + 100000,
      sha256: null,
      fileType: '.uasset',
    },
    {
      path: request.outputAssetPath.replace(/\.uasset$/, '.uexp'),
      sizeBytes: Math.floor(Math.random() * 50000000) + 1000000,
      sha256: null,
      fileType: '.uexp',
    },
  ];

  if (Math.random() < 0.5) {
    producedFiles.push({
      path: request.outputAssetPath.replace(/\.uasset$/, '.ubulk'),
      sizeBytes: Math.floor(Math.random() * 100000000) + 5000000,
      sha256: null,
      fileType: '.ubulk',
    });
  }

  const result: UAssetResultDto = {
    operation: 'Serialize',
    inputPath: request.inputJsonPath,
    outputPath: request.outputAssetPath,
    duration: '00:00:0' + (Math.floor(Math.random() * 5) + 2) + '.000',
    warnings: shouldIncludeWarnings() ? generateWarnings() : [],
    producedFiles,
    schemaVersion: request.schemaVersion || 'GAME_UE5_3',
    ueVersion: request.ueVersion || '5.3',
    logExcerpt: null,
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

export async function runDeserialize(
  request: UAssetDeserializeRequest
): Promise<UAssetDeserializeResponse> {
  await simulateLatency();

  const producedFiles: ProducedFileDto[] = [
    {
      path: request.outputJsonPath,
      sizeBytes: Math.floor(Math.random() * 10000000) + 50000,
      sha256: null,
      fileType: '.json',
    },
  ];

  const result: UAssetResultDto = {
    operation: 'Deserialize',
    inputPath: request.inputAssetPath,
    outputPath: request.outputJsonPath,
    duration: '00:00:0' + (Math.floor(Math.random() * 4) + 1) + '.000',
    warnings: shouldIncludeWarnings() ? generateWarnings() : [],
    producedFiles,
    schemaVersion: request.schemaVersion || 'GAME_UE5_3',
    ueVersion: request.ueVersion || '5.3',
    logExcerpt: null,
  };

  return {
    operationId: generateOperationId(),
    status: OperationStatus.Succeeded,
    result,
    error: null,
    startedAt: new Date().toISOString(),
    completedAt: new Date(Date.now() + 1500).toISOString(),
  };
}

export async function runInspect(
  request: UAssetInspectRequest
): Promise<UAssetInspectResponse> {
  await simulateLatency();

  const nameCount = Math.floor(Math.random() * 500) + 50;
  const exportCount = Math.floor(Math.random() * 50) + 5;
  const importCount = Math.floor(Math.random() * 100) + 10;

  const sampleNames = [
    'None',
    'Default__BlueprintGeneratedClass',
    'ObjectProperty',
    'StructProperty',
    'ArrayProperty',
    'ByteProperty',
    'IntProperty',
    'FloatProperty',
    'BoolProperty',
    'StrProperty',
  ];

  const sampleExports = [
    '/Game/Characters/Player/BP_Player.BP_Player',
    '/Game/Weapons/Rifle/BP_Rifle.BP_Rifle',
    '/Game/Items/Health/BP_HealthPack.BP_HealthPack',
    '/Game/UI/HUD/W_MainHUD.W_MainHUD',
    '/Script/Engine.BlueprintGeneratedClass',
  ];

  const sampleImports = [
    '/Script/CoreUObject.Class',
    '/Script/Engine.BlueprintGeneratedClass',
    '/Script/UMG.WidgetBlueprint',
    '/Game/Core/GameInstance.GameInstance',
  ];

  const result: UAssetInspectionDto = {
    inputPath: request.inputAssetPath,
    summary: {
      ueVersion: '5.3.2',
      licenseeVersion: 0,
      customVersionCount: Math.floor(Math.random() * 10),
      nameCount,
      exportCount,
      importCount,
    },
    exports:
      request.fields?.includes('exports') || !request.fields
        ? sampleExports.slice(0, Math.min(exportCount, sampleExports.length))
        : null,
    imports:
      request.fields?.includes('imports') || !request.fields
        ? sampleImports.slice(0, Math.min(importCount, sampleImports.length))
        : null,
    names:
      request.fields?.includes('names') || !request.fields
        ? sampleNames.slice(0, 10)
        : null,
  };

  return {
    operationId: generateOperationId(),
    status: OperationStatus.Succeeded,
    result,
    error: null,
    startedAt: new Date().toISOString(),
    completedAt: new Date(Date.now() + 500).toISOString(),
  };
}
