import { getBackendBaseUrl } from '../config/backend';
import type { UwpDumpCommandDto, UwpDumpResponse } from '../types/contracts';

export async function runDump(
  command: UwpDumpCommandDto
): Promise<UwpDumpResponse> {
  const baseUrl = getBackendBaseUrl();
  const url = `${baseUrl}/api/uwpdumper/dump`;

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(command),
    });

    if (!response.ok) {
      let message = `Request failed with status ${response.status}`;
      try {
        const body = await response.json();
        if (body?.error?.code || body?.error?.message) {
          const errorCode = body.error.code ?? 'ERROR';
          const errorMessage = body.error.message ?? message;
          const hint = body.error.remediationHint;
          message = hint ? `${errorCode}: ${errorMessage} (${hint})` : `${errorCode}: ${errorMessage}`;
        }
      } catch {
        // Ignore JSON parse errors
      }
      throw new Error(message);
    }

    const data = (await response.json()) as UwpDumpResponse;
    return data;
  } catch (err) {
    if (err instanceof Error) {
      throw err;
    }
    throw new Error(`Failed to submit UWP dump: ${String(err)}`);
  }
}
