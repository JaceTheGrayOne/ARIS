const devUrl = "http://localhost:5000";

/**
 * Gets the backend base URL for API calls.
 * In production (served by backend), uses same-origin relative URLs.
 * In development, uses explicit localhost URL (or override via VITE_ARIS_BACKEND_URL).
 */
export function getBackendBaseUrl(): string {
  // In production (served by backend), use same-origin relative URLs
  if (import.meta.env.PROD) {
    return "";
  }
  // In development, use explicit URL (or override)
  const fromEnv = import.meta.env.VITE_ARIS_BACKEND_URL;
  return (fromEnv && typeof fromEnv === "string" && fromEnv.length > 0)
    ? fromEnv
    : devUrl;
}

/**
 * Gets the backend WebSocket URL.
 * In production, uses same-origin WebSocket.
 * In development, converts http:// to ws://.
 */
export function getBackendWsUrl(): string {
  if (import.meta.env.PROD) {
    // Same-origin WebSocket
    const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    return `${proto}//${window.location.host}`;
  }
  const http = getBackendBaseUrl();
  return http.replace(/^http/, 'ws');
}
