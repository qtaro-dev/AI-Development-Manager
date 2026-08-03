export type DataAccessMode = "local" | "server";

export type FoundationStatus = {
    readonly state: "ready";
    readonly apiVersion: string;
    readonly contractVersion: string;
    readonly serverTimeUtc: string;
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

export interface DataAccessPort {
    getFoundationStatus(): Promise<DataAccessResult<FoundationStatus>>;
}
