import { getBackendBaseUrl, getBackendWsUrl } from '../config/backend';
import type {
  RetocBuildCommandRequest,
  RetocBuildCommandResponse,
  RetocCommandSchemaResponse,
  RetocHelpResponse,
  RetocStreamRequest,
  RetocStreamEvent,
} from '../types/contracts';

/**
 * Build a Retoc command for preview.
 * Returns the executable path, arguments, and formatted command line.
 */
export async function buildRetocCommand(
  request: RetocBuildCommandRequest
): Promise<RetocBuildCommandResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/retoc/build`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  return await response.json();
}

/**
 * Get the schema definition for all supported Retoc commands.
 * Used to render the Advanced Mode UI dynamically.
 *
 * Note: Currently uses /api/retoc/schema (RetocCommandSchemaProvider) for UI compatibility.
 * The canonical /api/tools/retoc/schema endpoint serves the generated ToolSchema format.
 * Migration to the generated schema is tracked as a follow-up.
 */
export async function getRetocSchema(): Promise<RetocCommandSchemaResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/retoc/schema`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  return await response.json();
}

/**
 * Get Retoc help text as Markdown.
 * Uses the canonical /api/tools/retoc/help endpoint.
 */
export async function getRetocHelp(): Promise<RetocHelpResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/tools/retoc/help`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  // The canonical endpoint returns plain text; wrap in code fence for UI
  const text = await response.text();
  return { markdown: '```\n' + text + '\n```' };
}

/**
 * Execute a Retoc command with streaming output via WebSocket.
 * The backend uses ConPTY to provide proper terminal output for indicatif progress bars.
 *
 * @param request - The Retoc command to execute
 * @param onEvent - Callback invoked for each stream event
 * @returns Object with cancel function and promise that resolves when stream ends
 */
export function streamRetocExecution(
  request: RetocStreamRequest,
  onEvent: (event: RetocStreamEvent) => void
): { cancel: () => void; promise: Promise<void> } {
  const wsUrl = getBackendWsUrl();
  const ws = new WebSocket(`${wsUrl}/api/retoc/stream`);
  let isCancelled = false;

  const promise = new Promise<void>((resolve, reject) => {
    ws.onopen = () => {
      // Send the request when connection opens
      ws.send(JSON.stringify(request));
    };

    ws.onmessage = (event) => {
      if (isCancelled) return;

      try {
        // Parse NDJSON (one JSON object per line)
        const lines = (event.data as string).split('\n').filter((line: string) => line.trim());
        for (const line of lines) {
          const streamEvent = JSON.parse(line) as RetocStreamEvent;
          onEvent(streamEvent);

          // Resolve when we get the exited or error event
          if (streamEvent.type === 'exited' || streamEvent.type === 'error') {
            ws.close();
            resolve();
          }
        }
      } catch (err) {
        console.error('Failed to parse stream event:', err);
      }
    };

    ws.onerror = (event) => {
      console.error('WebSocket error:', event);
      reject(new Error('WebSocket connection error'));
    };

    ws.onclose = (event) => {
      if (!isCancelled && event.code !== 1000) {
        // Abnormal close
        reject(new Error(`WebSocket closed unexpectedly: ${event.code} ${event.reason}`));
      } else {
        resolve();
      }
    };
  });

  const cancel = () => {
    isCancelled = true;
    if (ws.readyState === WebSocket.OPEN) {
      // Send cancel command
      ws.send(JSON.stringify({ action: 'cancel' }));
    }
    ws.close();
  };

  return { cancel, promise };
}
