import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
    LocalChannelClient,
    LocalChannelProtocolError,
    parseMessage,
    serializeRequest,
    type LocalChannelTransport,
} from "./index";

const fixtureRoot = resolve(
    process.cwd(),
    "../../tests/fixtures/local-channel",
);
const fixture = (name: string) =>
    readFileSync(resolve(fixtureRoot, name), "utf8");

describe("Local Application Channel v1", () => {
    afterEach(() => {
        vi.useRealTimers();
    });
    it("parses the shared response and error fixtures", () => {
        expect(parseMessage(fixture("valid-response.json")).kind).toBe(
            "response",
        );
        expect(parseMessage(fixture("valid-error.json")).kind).toBe("error");
    });

    it("serializes a strict request envelope", () => {
        const request = JSON.parse(
            serializeRequest("request-001", "test.echo", { value: "fixture" }),
        );

        expect(request).toEqual({
            version: 1,
            kind: "request",
            requestId: "request-001",
            operation: "test.echo",
            payload: { value: "fixture" },
        });
    });

    it("rejects unknown fields, unsupported kinds, and oversized messages", () => {
        expect(() =>
            parseMessage(fixture("invalid-unknown-field.json")),
        ).toThrow(LocalChannelProtocolError);
        expect(() =>
            parseMessage(
                '{"version":1,"kind":"request","requestId":"r","operation":"test.echo","payload":{}}',
            ),
        ).toThrow("kind");
        expect(() =>
            parseMessage(
                `{"version":1,"kind":"response","requestId":"request-001","result":{"value":"${"x".repeat(1024 * 1024)}"}}`,
            ),
        ).toThrow("size");
    });

    it("correlates only pending responses and ignores unknown or duplicate IDs", async () => {
        let receive: ((message: string) => void) | undefined;
        const transport: LocalChannelTransport = {
            postMessage: vi.fn((message) => {
                const request = JSON.parse(message) as { requestId: string };
                receive?.(
                    JSON.stringify({
                        version: 1,
                        kind: "response",
                        requestId: request.requestId,
                        result: { ok: true },
                    }),
                );
            }),
            subscribe: (listener) => {
                receive = listener;
                return () => {
                    receive = undefined;
                };
            },
        };
        const client = new LocalChannelClient(transport);

        await expect(client.request("test.echo", {})).resolves.toEqual({
            ok: true,
        });
        receive?.(
            '{"version":1,"kind":"response","requestId":"unknown","result":{}}',
        );
        client.dispose();
    });

    it("rejects safely when WebView2 transport is unavailable", async () => {
        await expect(
            new LocalChannelClient({
                postMessage: () => {
                    throw new Error("transport unavailable");
                },
                subscribe: () => () => undefined,
            }).request("test.echo"),
        ).rejects.toMatchObject({ code: "channel_unavailable" });
    });

    it("fails a request on timeout and ignores its late response", async () => {
        vi.useFakeTimers();
        let receive: ((message: string) => void) | undefined;
        let requestId: string | undefined;
        const transport: LocalChannelTransport = {
            postMessage: vi.fn((message) => {
                requestId = (JSON.parse(message) as { requestId: string })
                    .requestId;
            }),
            subscribe: (listener) => {
                receive = listener;
                return () => {
                    receive = undefined;
                };
            },
        };
        const client = new LocalChannelClient(transport);

        const request = client.request("test.echo", {}, { timeoutMs: 10 });
        await vi.advanceTimersByTimeAsync(10);
        await expect(request).rejects.toMatchObject({ code: "timeout" });
        receive?.(
            JSON.stringify({
                version: 1,
                kind: "response",
                requestId: requestId!,
                result: {},
            }),
        );
        client.dispose();
    });

    it("fails a request on caller cancellation and does not send after cancellation", async () => {
        const controller = new AbortController();
        const postMessage = vi.fn();
        const client = new LocalChannelClient({
            postMessage,
            subscribe: () => () => undefined,
        });

        const request = client.request("test.echo", {}, {
            timeoutMs: 1000,
            signal: controller.signal,
        });
        controller.abort();
        await expect(request).rejects.toMatchObject({ code: "cancelled" });
        expect(postMessage).toHaveBeenCalledTimes(1);
        client.dispose();
    });

    it("fails all pending requests on idempotent dispose and rejects new requests", async () => {
        const postMessage = vi.fn();
        const transport: LocalChannelTransport = {
            postMessage,
            subscribe: () => () => undefined,
            dispose: vi.fn(),
        };
        const client = new LocalChannelClient(transport);
        const first = client.request("test.echo", {}, { timeoutMs: 1000 });
        const second = client.request("test.echo", {}, { timeoutMs: 1000 });

        client.dispose();
        client.dispose();
        await expect(first).rejects.toMatchObject({
            code: "channel_unavailable",
        });
        await expect(second).rejects.toMatchObject({
            code: "channel_unavailable",
        });
        await expect(
            client.request("test.echo", {}, { timeoutMs: 1000 }),
        ).rejects.toMatchObject({ code: "channel_unavailable" });
        expect(postMessage).toHaveBeenCalledTimes(2);
        expect(transport.dispose).toHaveBeenCalledTimes(1);
    });
});
