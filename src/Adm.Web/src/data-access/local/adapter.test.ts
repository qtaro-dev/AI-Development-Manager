import { describe, expect, it } from "vitest";
import { createLocalDataAccess } from "./adapter";

describe("Local Application DataAccess Adapter", () => {
    it("maps a foundation response from the local channel", async () => {
        let receive: ((message: string) => void) | undefined;
        const result = createLocalDataAccess({
            postMessage: (message) => {
                const request = JSON.parse(message) as { requestId: string };
                receive?.(
                    JSON.stringify({
                        version: 1,
                        kind: "response",
                        requestId: request.requestId,
                        result: {
                            state: "ready",
                            apiVersion: "local",
                            contractVersion: "1.0",
                            serverTimeUtc: "2026-08-04T00:00:00Z",
                        },
                    }),
                );
            },
            subscribe: (listener) => {
                receive = listener;
                return () => {
                    receive = undefined;
                };
            },
        }).getFoundationStatus();

        await expect(result).resolves.toEqual({
            kind: "success",
            value: {
                state: "ready",
                apiVersion: "local",
                contractVersion: "1.0",
                serverTimeUtc: "2026-08-04T00:00:00Z",
            },
        });
    });

    it("maps an unavailable local channel to a safe failure", async () => {
        const result = createLocalDataAccess({
            postMessage: () => {
                throw new Error("private path");
            },
            subscribe: () => () => undefined,
        }).getFoundationStatus();

        await expect(result).resolves.toMatchObject({
            kind: "failure",
            error: { code: "operation_failed", nextAction: "retry" },
        });
    });
});
