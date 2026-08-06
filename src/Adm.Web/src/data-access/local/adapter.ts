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
    DataAccessRequestOptions,
    Project,
    ProjectList,
    RegisterProjectInput,
    RegisterProjectResult,
    UnregisterProjectResult,
    SelectProjectResult,
} from "../port";

export type LocalDataAccessPort = DataAccessPort & { dispose(): void };

const FOUNDATION_STATUS_OPERATION = "getFoundationStatus";
const PROJECT_LIST_OPERATION = "project.list";
const PROJECT_REGISTER_OPERATION = "project.register";
const PROJECT_UNREGISTER_OPERATION = "project.unregister";
const PROJECT_SELECT_OPERATION = "project.select";

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

function isProject(value: unknown): value is Project {
    if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
    const candidate = value as Record<string, unknown>;
    return Object.keys(candidate).length === 5 &&
        typeof candidate.id === "string" &&
        typeof candidate.displayName === "string" &&
        typeof candidate.root === "string" &&
        typeof candidate.registeredAtUtc === "string" &&
        typeof candidate.isSelected === "boolean";
}

function isProjectList(value: unknown): value is ProjectList {
    if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
    const candidate = value as Record<string, unknown>;
    return Object.keys(candidate).length === 3 &&
        Array.isArray(candidate.projects) && candidate.projects.every(isProject) &&
        (typeof candidate.selectedProjectId === "string" || candidate.selectedProjectId === null) &&
        Array.isArray(candidate.warnings) && candidate.warnings.every((warning) => {
            if (typeof warning !== "object" || warning === null || Array.isArray(warning)) return false;
            const item = warning as Record<string, unknown>;
            return Object.keys(item).length === 2 &&
                typeof item.projectId === "string" && typeof item.code === "string";
        });
}

function isRegisterProjectResult(value: unknown): value is RegisterProjectResult {
    if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
    const candidate = value as Record<string, unknown>;
    return Object.keys(candidate).length === 1 && isProject(candidate.project);
}

function isUnregisterProjectResult(value: unknown): value is UnregisterProjectResult {
    if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
    const candidate = value as Record<string, unknown>;
    return Object.keys(candidate).length === 1 && typeof candidate.projectId === "string";
}

function isSelectProjectResult(value: unknown): value is SelectProjectResult {
    if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
    const candidate = value as Record<string, unknown>;
    return Object.keys(candidate).length === 1 &&
        (typeof candidate.selectedProjectId === "string" || candidate.selectedProjectId === null);
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
        async listProjects(options: DataAccessRequestOptions = {}) {
            try {
                const result = await client.request(PROJECT_LIST_OPERATION, {}, options);
                return isProjectList(result)
                    ? { kind: "success", value: result }
                    : failure<ProjectList>("invalid_result", "Local project list result is invalid.", false, "close");
            } catch (error) {
                return failureFrom<ProjectList>(error, "Local project list is unavailable.");
            }
        },
        async registerProject(input: RegisterProjectInput, options: DataAccessRequestOptions = {}) {
            try {
                const result = await client.request(PROJECT_REGISTER_OPERATION, input, options);
                return isRegisterProjectResult(result)
                    ? { kind: "success", value: result }
                    : failure<RegisterProjectResult>("invalid_result", "Local project registration result is invalid.", false, "close");
            } catch (error) {
                return failureFrom<RegisterProjectResult>(error, "Local project registration failed.");
            }
        },
        async unregisterProject(projectId: string, options: DataAccessRequestOptions = {}) {
            try {
                const result = await client.request(PROJECT_UNREGISTER_OPERATION, { projectId }, options);
                return isUnregisterProjectResult(result)
                    ? { kind: "success", value: result }
                    : failure<UnregisterProjectResult>("invalid_result", "Local project unregister result is invalid.", false, "close");
            } catch (error) {
                return failureFrom<UnregisterProjectResult>(error, "Local project unregister failed.");
            }
        },
        async selectProject(projectId: string | null, options: DataAccessRequestOptions = {}) {
            try {
                const result = await client.request(PROJECT_SELECT_OPERATION, { projectId }, options);
                return isSelectProjectResult(result)
                    ? { kind: "success", value: result }
                    : failure<SelectProjectResult>("invalid_result", "Local project selection result is invalid.", false, "close");
            } catch (error) {
                return failureFrom<SelectProjectResult>(error, "Local project selection failed.");
            }
        },
        dispose: () => client.dispose(),
    };
}
