import {
    LocalChannelClient,
    type LocalChannelTransport,
} from "./client";
import { LocalChannelProtocolError } from "./protocol";
import type {
    DataAccessFailure,
    DataAccessPort,
    DataAccessResult,
    FoundationStatus,
    ExecutionProfile,
    ExecutionProfileReadResult,
    ExecutionProfileUpdate,
} from "../port";

export type LocalDataAccessPort = DataAccessPort & { dispose(): void };

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

function failure<T>(
    code: DataAccessFailure["code"],
    message: string,
    retryable: boolean,
    nextAction: DataAccessFailure["nextAction"],
): DataAccessResult<T> {
    return {
        kind: "failure",
        error: { code, message, retryable, nextAction },
    };
}

function isExecutionProfile(value: unknown): value is ExecutionProfile {
    if (typeof value !== "object" || value === null) return false;
    const candidate = value as Record<string, unknown>;
    return (
        candidate.schemaVersion === 1 &&
        (candidate.mode === "local" || candidate.mode === "server") &&
        (typeof candidate.serverUri === "string" ||
            candidate.serverUri === null)
    );
}

function isExecutionProfileReadResult(
    value: unknown,
): value is ExecutionProfileReadResult {
    if (typeof value !== "object" || value === null) return false;
    const candidate = value as Record<string, unknown>;
    return (
        isExecutionProfile(candidate.profile) &&
        typeof candidate.usedLocalFallback === "boolean" &&
        (typeof candidate.warningCode === "string" ||
            candidate.warningCode === null) &&
        typeof candidate.hasPersistedProfile === "boolean"
    );
}

export function createLocalDataAccess(
    transport: LocalChannelTransport,
): LocalDataAccessPort {
    const client = new LocalChannelClient(transport);

    const failureFrom = <T>(
        error: unknown,
        fallbackMessage: string,
    ): DataAccessResult<T> => {
        if (error instanceof LocalChannelProtocolError) {
            if (error.code === "timeout") {
                return failure("timeout", fallbackMessage, true, "retry");
            }
            if (error.code === "cancelled") {
                return failure("cancelled", fallbackMessage, true, "retry");
            }
            if (error.code === "channel_unavailable") {
                return failure(
                    "channel_unavailable",
                    fallbackMessage,
                    false,
                    "close",
                );
            }
        }
        return failure("operation_failed", fallbackMessage, true, "retry");
    };

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
            } catch (error) {
                return failureFrom(
                    error,
                    "ローカル処理を完了できませんでした。",
                );
            }
        },
        async getExecutionProfile() {
            try {
                const result = await client.request("executionProfile.get", {});
                return isExecutionProfileReadResult(result)
                    ? { kind: "success", value: result }
                    : failure<ExecutionProfileReadResult>(
                          "invalid_result",
                          "実行プロファイルを読み込めませんでした。",
                          false,
                          "close",
                      );
            } catch (error) {
                return failureFrom<ExecutionProfileReadResult>(
                    error,
                    "実行プロファイルを読み込めませんでした。",
                );
            }
        },
        async updateExecutionProfile(update: ExecutionProfileUpdate) {
            try {
                const result = await client.request(
                    "executionProfile.update",
                    update,
                );
                return isExecutionProfile(result)
                    ? { kind: "success", value: result }
                    : failure<ExecutionProfile>(
                          "invalid_result",
                          "実行プロファイルを保存できませんでした。",
                          false,
                          "close",
                      );
            } catch (error) {
                return failureFrom<ExecutionProfile>(
                    error,
                    "実行プロファイルを保存できませんでした。",
                );
            }
        },
        dispose: () => client.dispose(),
    };
}
