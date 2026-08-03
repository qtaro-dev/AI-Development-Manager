import { describe, expect, it, vi } from "vitest";
import {
    composeDataAccess,
    createHttpDataAccess,
    DataAccessCompositionError,
    type DataAccessPort,
} from "./index";

describe("DataAccess Port", () => {
    it("maps a typed HTTP result without exposing transport details", async () => {
        const request = vi.fn<typeof fetch>(
            async () =>
                new Response(
                    JSON.stringify({
                        apiVersion: "v1",
                        contractVersion: "1.0",
                        serverTimeUtc: "2026-08-04T00:00:00Z",
                        status: "ready",
                    }),
                    { status: 200 },
                ),
        );

        const result = await createHttpDataAccess(
            "/api/v1",
            request,
        ).getFoundationStatus();

        expect(result).toEqual({
            kind: "success",
            value: {
                state: "ready",
                apiVersion: "v1",
                contractVersion: "1.0",
                serverTimeUtc: "2026-08-04T00:00:00Z",
            },
        });
    });

    it("returns a safe typed failure instead of an exception", async () => {
        const request = vi.fn<typeof fetch>(
            async () => new Response("unavailable", { status: 503 }),
        );

        const result = await createHttpDataAccess(
            "/api/v1",
            request,
        ).getFoundationStatus();

        expect(result).toEqual({
            kind: "failure",
            error: {
                code: "operation_failed",
                message: "サービスに接続できませんでした。",
                retryable: true,
                nextAction: "retry",
            },
        });
    });

    it("injects a Fake Adapter for local mode", async () => {
        const fake: DataAccessPort = {
            getFoundationStatus: vi.fn(async () => ({
                kind: "success" as const,
                value: {
                    state: "ready" as const,
                    apiVersion: "local",
                    contractVersion: "1.0",
                    serverTimeUtc: "2026-08-04T00:00:00Z",
                },
            })),
            getExecutionProfile: vi.fn(async () => ({
                kind: "failure" as const,
                error: {
                    code: "adapter_unavailable" as const,
                    message: "unavailable",
                    retryable: false,
                    nextAction: "checkSettings" as const,
                },
            })),
            updateExecutionProfile: vi.fn(async () => ({
                kind: "failure" as const,
                error: {
                    code: "adapter_unavailable" as const,
                    message: "unavailable",
                    retryable: false,
                    nextAction: "checkSettings" as const,
                },
            })),
        };

        const port = composeDataAccess({ mode: "local", adapter: fake });

        await expect(port.getFoundationStatus()).resolves.toMatchObject({
            kind: "success",
            value: { apiVersion: "local" },
        });
        expect(fake.getFoundationStatus).toHaveBeenCalledOnce();
    });

    it("fails safely when a mode has no adapter", () => {
        expect(() => composeDataAccess({ mode: "local" })).toThrow(
            DataAccessCompositionError,
        );
        expect(() => composeDataAccess({ mode: "invalid" as never })).toThrow(
            DataAccessCompositionError,
        );
    });
});
