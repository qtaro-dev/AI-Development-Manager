export type DataAccessMode = "local" | "server";

export type FoundationStatus = {
    readonly state: "ready";
    readonly apiVersion: string;
    readonly contractVersion: string;
    readonly serverTimeUtc: string;
};

export type ExecutionProfileMode = "local" | "server";
export type ExecutionProfile = {
    readonly schemaVersion: 1;
    readonly mode: ExecutionProfileMode;
    readonly serverUri: string | null;
};
export type ExecutionProfileUpdate = {
    readonly mode: ExecutionProfileMode;
    readonly serverUri: string | null;
};
export type ExecutionProfileReadResult = {
    readonly profile: ExecutionProfile;
    readonly usedLocalFallback: boolean;
    readonly warningCode: string | null;
    readonly hasPersistedProfile: boolean;
};

export type DataAccessFailureCode =
    "adapter_unavailable" | "operation_failed" | "invalid_result";

export type DataAccessFailure = {
    readonly code: DataAccessFailureCode;
    readonly message: string;
    readonly retryable: boolean;
    readonly nextAction: "retry" | "checkSettings" | "close";
};

export type DataAccessResult<T> =
    | { readonly kind: "success"; readonly value: T }
    | { readonly kind: "failure"; readonly error: DataAccessFailure };

/**
 * Business data access only. Host settings are intentionally kept in the
 * separate HostSettingsPort so future project operations do not acquire a
 * dependency on WPF execution-profile storage.
 */
export interface BusinessDataAccessPort {
    getFoundationStatus(): Promise<DataAccessResult<FoundationStatus>>;
}

/** WPF execution-profile and host-settings boundary. */
export interface HostSettingsPort {
    getExecutionProfile(): Promise<
        DataAccessResult<ExecutionProfileReadResult>
    >;
    updateExecutionProfile(
        update: ExecutionProfileUpdate,
    ): Promise<DataAccessResult<ExecutionProfile>>;
}

/** Current UI composition combines the two explicit boundaries. */
export type DataAccessPort = BusinessDataAccessPort & HostSettingsPort;
