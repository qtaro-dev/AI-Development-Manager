import { describe, expect, it } from "vitest";
import {
    initialStartupState,
    startupReducer,
} from "./startupState";

const foundation = {
    state: "ready" as const,
    apiVersion: "local",
    contractVersion: "1.0",
    serverTimeUtc: "2026-08-04T00:00:00Z",
};

const profile = {
    profile: { schemaVersion: 1 as const, mode: "local" as const, serverUri: null },
    usedLocalFallback: false,
    warningCode: null,
    hasPersistedProfile: false,
};

describe("startup state", () => {
    it("transitions loading to ready only after foundation and profile resolve", () => {
        const state = startupReducer(initialStartupState, {
            type: "resolved",
            foundation,
            profile,
        });

        expect(state.status).toBe("ready");
    });

    it("marks profile fallback as degraded and retry success as recovered", () => {
        const degraded = startupReducer(initialStartupState, {
            type: "resolved",
            foundation,
            profile: { ...profile, usedLocalFallback: true, warningCode: "profile_recovered_local" },
        });
        expect(degraded.status).toBe("degraded");

        const retrying = startupReducer(degraded, { type: "request", retry: true });
        const recovered = startupReducer(retrying, {
            type: "resolved",
            foundation,
            profile,
        });
        expect(recovered.status).toBe("recovered");
    });
});
