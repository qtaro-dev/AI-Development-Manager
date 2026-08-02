export interface ApiVersionResponse {
    readonly apiVersion: string;
    readonly contractVersion: string;
    readonly serverTimeUtc: string;
    readonly status: "ready";
}

export interface ApiClient {
    getVersion(): Promise<ApiVersionResponse>;
}

export function createApiClient(
    baseUrl: string,
    request: typeof fetch = fetch,
): ApiClient {
    return {
        async getVersion() {
            const response = await request(`${baseUrl}/version`, {
                headers: { Accept: "application/json" },
            });
            if (!response.ok) {
                throw new Error(`API request failed: ${response.status}`);
            }
            return (await response.json()) as ApiVersionResponse;
        },
    };
}
