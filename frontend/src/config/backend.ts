const defaultUrl = "http://localhost:5000";

/**
 * Gets the backend base URL for API calls.
 * Can be overridden via VITE_ARIS_BACKEND_URL environment variable.
 */
export function getBackendBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_ARIS_BACKEND_URL;
  return (fromEnv && typeof fromEnv === "string" && fromEnv.length > 0)
    ? fromEnv
    : defaultUrl;
}
