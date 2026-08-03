import { createApiClient, type ApiVersionResponse } from "../api/client";
import type {
    DataAccessPort,
    DataAccessResult,
    FoundationStatus,
} from "./port";

function isApiVersionResponse(value: unknown): value is ApiVersionResponse {
    if (typeof value !== "object" || value === null) return false;

    const candidate = value as Record<string, unknown>;
    return (
        candidate.status === "ready" &&
        typeof candidate.apiVersion === "string" &&
        typeof candidate.contractVersion === "string" &&
        typeof candidate.serverTimeUtc === "string"
    );
}

export function createHttpDataAccess(
    baseUrl: string,
    request: typeof fetch = fetch,
): DataAccessPort {
    const client = createApiClient(baseUrl, request);

    return {
        async getFoundationStatus(): Promise<
            DataAccessResult<FoundationStatus>
        > {
            try {
                const response = await client.getVersion();
                if (!isApiVersionResponse(response)) {
                    return {
                        kind: "failure",
                        error: {
                            code: "invalid_result",
                            message:
                                "サービスから正しい状態を取得できませんでした。",
                            retryable: false,
                            nextAction: "checkSettings",
                        },
                    };
                }

                return {
                    kind: "success",
                    value: {
                        state: response.status,
                        apiVersion: response.apiVersion,
                        contractVersion: response.contractVersion,
                        serverTimeUtc: response.serverTimeUtc,
                    },
                };
            } catch {
                return {
                    kind: "failure",
                    error: {
                        code: "operation_failed",
                        message: "サービスに接続できませんでした。",
                        retryable: true,
                        nextAction: "retry",
                    },
                };
            }
        },
    };
}
