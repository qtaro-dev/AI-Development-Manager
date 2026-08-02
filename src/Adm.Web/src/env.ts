export interface WebRuntimeConfig {
    readonly apiBaseUrl: string;
}

export function readRuntimeConfig(
    env: ImportMetaEnv = import.meta.env,
): WebRuntimeConfig {
    return {
        apiBaseUrl: env.VITE_API_BASE_URL?.trim() || "/api/v1",
    };
}
