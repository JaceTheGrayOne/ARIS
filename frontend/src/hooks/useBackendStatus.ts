import { useState, useEffect } from 'react';
import { getBackendBaseUrl } from '../config/backend';
import type { HealthResponse, InfoResponse } from '../types/contracts';

const HEALTH_POLL_INTERVAL_MS = 15000;

export type BackendStatusState = {
  loading: boolean;
  error: string | null;

  health: HealthResponse | null;
  info: InfoResponse | null;

  effectiveStatus: 'unknown' | 'starting' | 'ready' | 'error';
  lastUpdated: Date | null;
};

export function useBackendStatus(): BackendStatusState {
  const [state, setState] = useState<BackendStatusState>({
    loading: true,
    error: null,
    health: null,
    info: null,
    effectiveStatus: 'unknown',
    lastUpdated: null,
  });

  useEffect(() => {
    const baseUrl = getBackendBaseUrl();

    const fetchHealth = async () => {
      try {
        const response = await fetch(`${baseUrl}/health`);

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const healthData: HealthResponse = await response.json();

        setState((prev) => ({
          ...prev,
          loading: false,
          error: null,
          health: healthData,
          effectiveStatus: mapHealthStatus(healthData.status),
          lastUpdated: new Date(),
        }));
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unknown error';
        setState((prev) => ({
          ...prev,
          loading: false,
          error: `Failed to fetch health: ${message}`,
          effectiveStatus: 'error',
          lastUpdated: new Date(),
        }));
      }
    };

    const fetchInfo = async () => {
      try {
        const response = await fetch(`${baseUrl}/info`);

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const infoData: InfoResponse = await response.json();

        setState((prev) => ({
          ...prev,
          info: infoData,
        }));
      } catch (err) {
        console.error('Failed to fetch backend info:', err);
      }
    };

    fetchHealth();
    fetchInfo();

    const intervalId = setInterval(fetchHealth, HEALTH_POLL_INTERVAL_MS);

    return () => {
      clearInterval(intervalId);
    };
  }, []);

  return state;
}

function mapHealthStatus(status: string): 'unknown' | 'starting' | 'ready' | 'error' {
  const normalized = status.toLowerCase();

  if (normalized === 'ready') {
    return 'ready';
  }

  if (normalized === 'starting') {
    return 'starting';
  }

  if (normalized === 'error') {
    return 'error';
  }

  return 'unknown';
}
