import { getBackendBaseUrl } from '../config/backend';
import type {
  UAssetSerializeRequest,
  UAssetDeserializeRequest,
  UAssetInspectRequest,
  UAssetSerializeResponse,
  UAssetDeserializeResponse,
  UAssetInspectResponse,
} from '../types/contracts';

export async function runSerialize(
  request: UAssetSerializeRequest
): Promise<UAssetSerializeResponse> {
  const baseUrl = getBackendBaseUrl();
  const url = `${baseUrl}/api/uasset/serialize`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const body = await response.json();
      if (body?.error?.code || body?.error?.message) {
        message = `${body.error.code ?? 'ERROR'}: ${body.error.message ?? message}`;
      }
    } catch {
      // Ignore JSON parse errors
    }
    throw new Error(message);
  }

  const data = (await response.json()) as UAssetSerializeResponse;
  return data;
}

export async function runDeserialize(
  request: UAssetDeserializeRequest
): Promise<UAssetDeserializeResponse> {
  const baseUrl = getBackendBaseUrl();
  const url = `${baseUrl}/api/uasset/deserialize`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const body = await response.json();
      if (body?.error?.code || body?.error?.message) {
        message = `${body.error.code ?? 'ERROR'}: ${body.error.message ?? message}`;
      }
    } catch {
      // Ignore JSON parse errors
    }
    throw new Error(message);
  }

  const data = (await response.json()) as UAssetDeserializeResponse;
  return data;
}

export async function runInspect(
  request: UAssetInspectRequest
): Promise<UAssetInspectResponse> {
  const baseUrl = getBackendBaseUrl();
  const url = `${baseUrl}/api/uasset/inspect`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const body = await response.json();
      if (body?.error?.code || body?.error?.message) {
        message = `${body.error.code ?? 'ERROR'}: ${body.error.message ?? message}`;
      }
    } catch {
      // Ignore JSON parse errors
    }
    throw new Error(message);
  }

  const data = (await response.json()) as UAssetInspectResponse;
  return data;
}
