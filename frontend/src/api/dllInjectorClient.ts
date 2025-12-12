import { getBackendBaseUrl } from '../config/backend';
import type {
  DllInjectCommandDto,
  DllInjectResponse,
  DllEjectCommandDto,
  DllEjectResponse,
} from '../types/contracts';

async function postJson<TResponse>(path: string, body: unknown): Promise<TResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
  });

  let json: any = null;
  try {
    json = await response.json();
  } catch {
    // If backend returns no/invalid JSON on error, we'll handle below
  }

  if (!response.ok) {
    // Try to surface backend ErrorInfo if present
    if (json && typeof json === 'object' && 'error' in json && json.error) {
      const err = json.error;
      const code = typeof err.code === 'string' ? err.code : 'UNKNOWN_ERROR';
      const message =
        typeof err.message === 'string'
          ? err.message
          : 'An error occurred while calling DLL Injector endpoint.';
      const hint = typeof err.remediationHint === 'string' ? ` Hint: ${err.remediationHint}` : '';
      throw new Error(`${code}: ${message}${hint}`);
    }

    throw new Error(`Request failed with status ${response.status}`);
  }

  return json as TResponse;
}

export async function runInject(command: DllInjectCommandDto): Promise<DllInjectResponse> {
  return postJson<DllInjectResponse>('/api/dllinjector/inject', command);
}

export async function runEject(command: DllEjectCommandDto): Promise<DllEjectResponse> {
  return postJson<DllEjectResponse>('/api/dllinjector/eject', command);
}
