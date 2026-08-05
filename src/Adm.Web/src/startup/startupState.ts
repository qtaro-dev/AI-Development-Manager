import type { DataAccessFailure, ExecutionProfileReadResult, FoundationStatus } from "../data-access";

export type StartupStatus =
    | "loading"
    | "ready"
    | "degraded"
    | "recovered"
    | "error"
    | "retrying";

export type StartupState = {
    readonly status: StartupStatus;
    readonly foundation: FoundationStatus | null;
    readonly profile: ExecutionProfileReadResult | null;
    readonly failure: DataAccessFailure | null;
};

export type StartupAction =
    | { readonly type: "request"; readonly retry: boolean }
    | { readonly type: "resolved"; readonly foundation: FoundationStatus; readonly profile: ExecutionProfileReadResult }
    | { readonly type: "failed"; readonly failure: DataAccessFailure };

export const initialStartupState: StartupState = {
    status: "loading",
    foundation: null,
    profile: null,
    failure: null,
};

export function startupReducer(
    state: StartupState,
    action: StartupAction,
): StartupState {
    switch (action.type) {
        case "request":
            return { ...state, status: action.retry ? "retrying" : "loading", failure: null };
        case "resolved": {
            const fallback = action.profile.usedLocalFallback;
            const recovered = state.status === "retrying" || state.status === "error";
            return {
                status: recovered ? "recovered" : fallback ? "degraded" : "ready",
                foundation: action.foundation,
                profile: action.profile,
                failure: null,
            };
        }
        case "failed":
            return { ...state, status: "error", failure: action.failure };
    }
}
