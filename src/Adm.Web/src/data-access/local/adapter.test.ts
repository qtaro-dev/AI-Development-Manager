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
            error: { code: "channel_unavailable", nextAction: "close" },
        });
    });

    it("maps the four project operations and preserves request options", async () => {
        const requests: Array<{ operation: string; payload: unknown }> = [];
        let receive: ((message: string) => void) | undefined;
        const adapter = createLocalDataAccess({
            postMessage: (message) => {
                const request = JSON.parse(message) as {
                    requestId: string;
                    operation: string;
                    payload: unknown;
                };
                requests.push({ operation: request.operation, payload: request.payload });
                const result = request.operation === "project.list"
                    ? { projects: [], selectedProjectId: null, warnings: [] }
                    : request.operation === "project.register"
                      ? { project: {
                          id: "project-001",
                          displayName: "Sample",
                          root: "C:\\Projects\\Sample",
                          registeredAtUtc: "2026-08-06T00:00:00Z",
                          isSelected: false,
                      } }
                      : request.operation === "project.unregister"
                        ? { projectId: "project-001" }
                        : { selectedProjectId: "project-001" };
                receive?.(JSON.stringify({ version: 1, kind: "response", requestId: request.requestId, result }));
            },
            subscribe: (listener) => {
                receive = listener;
                return () => { receive = undefined; };
            },
        });
        const options = { timeoutMs: 1000 };

        await expect(adapter.listProjects(options)).resolves.toMatchObject({ kind: "success" });
        await expect(adapter.registerProject({ root: "C:\\Projects\\Sample", displayName: "Sample" }, options)).resolves.toMatchObject({ kind: "success" });
        await expect(adapter.unregisterProject("project-001", options)).resolves.toMatchObject({ kind: "success" });
        await expect(adapter.selectProject("project-001", options)).resolves.toMatchObject({ kind: "success" });
        adapter.dispose();

        expect(requests.map((request) => request.operation)).toEqual([
            "project.list",
            "project.register",
            "project.unregister",
            "project.select",
        ]);
        expect(requests[1].payload).toEqual({ root: "C:\\Projects\\Sample", displayName: "Sample" });
    });

    it("rejects a malformed project result as a safe invalid-result failure", async () => {
        let receive: ((message: string) => void) | undefined;
        const adapter = createLocalDataAccess({
            postMessage: (message) => {
                const request = JSON.parse(message) as { requestId: string };
                receive?.(JSON.stringify({
                    version: 1,
                    kind: "response",
                    requestId: request.requestId,
                    result: { projects: [], selectedProjectId: null },
                }));
            },
            subscribe: (listener) => {
                receive = listener;
                return () => { receive = undefined; };
            },
        });

        await expect(adapter.listProjects()).resolves.toMatchObject({
            kind: "failure",
            error: { code: "invalid_result", nextAction: "close" },
        });
        adapter.dispose();
    });
});
