import { LocalChannelClient, type LocalChannelTransport } from "./client";
import type {
    DataAccessFailure,
    DataAccessPort,
    DataAccessResult,
    FoundationStatus,
} from "../port";

const FOUNDATION_STATUS_OPERATION = "getFoundationStatus";

function isFoundationStatus(value: unknown): value is FoundationStatus {
    if (typeof value !== "object" || value === null) return false;

    const candidate = value as Record<string, unknown>;
    return (
        candidate.state === "ready" &&
        typeof candidate.apiVersion === "string" &&
        typeof candidate.contractVersion === "string" &&
        typeof candidate.serverTimeUtc === "string"
    );
}

function failure(
    code: DataAccessFailure["code"],
    message: string,
    retryable: boolean,
    nextAction: DataAccessFailure["nextAction"],
): DataAccessResult<FoundationStatus> {
    return {
        kind: "failure",
        error: { code, message, retryable, nextAction },
    };
}

export function createLocalDataAccess(
    transport: LocalChannelTransport,
): DataAccessPort {
    const client = new LocalChannelClient(transport);

    return {
        async getFoundationStatus(): Promise<
            DataAccessResult<FoundationStatus>
        > {
            try {
                const result = await client.request(
                    FOUNDATION_STATUS_OPERATION,
                    {},
                );
                return isFoundationStatus(result)
                    ? { kind: "success", value: result }
                    : failure(
                          "invalid_result",
                          "アプリケーションから正しい状態を取得できませんでした。",
                          false,
                          "close",
                      );
            } catch {
                return failure(
                    "operation_failed",
                    "ローカル処理を完了できませんでした。",
                    true,
                    "retry",
                );
            }
        },
    };
}
